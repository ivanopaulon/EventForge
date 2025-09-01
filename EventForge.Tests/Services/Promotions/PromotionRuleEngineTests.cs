using EventForge.DTOs.Promotions;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Promotions;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Promotions
{
    /// <summary>
    /// Comprehensive unit tests for PromotionService rule types and stacking behavior.
    /// Tests all scenarios specified in the problem statement.
    /// </summary>
    [Trait("Category", "Unit")]
public class PromotionRuleEngineTests : IDisposable
    {
        private readonly EventForgeDbContext _context;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<PromotionService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly PromotionService _promotionService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public PromotionRuleEngineTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<EventForgeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EventForgeDbContext(options);

            // Create mocks
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<PromotionService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Setup tenant context
            _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

            // Create service
            _promotionService = new PromotionService(
                _context,
                _mockAuditLogService.Object,
                _mockTenantContext.Object,
                _mockLogger.Object,
                _memoryCache);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_PercentageDiscount_CalculatesCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("10% Off All Items", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.Discount, discountPercentage: 10m);
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 20.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(20.00m, result.OriginalTotal);
            Assert.Equal(18.00m, result.FinalTotal); // 20 - (20 * 0.10)
            Assert.Equal(2.00m, result.TotalDiscountAmount);
            Assert.Single(result.AppliedPromotions);
            Assert.Equal("10% Off All Items", result.AppliedPromotions.First().PromotionName);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_CategoryDiscount_AppliesToCorrectCategory()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var otherCategoryId = Guid.NewGuid();

            var promotion = CreatePromotion("Electronics 15% Off", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.CategoryDiscount, discountPercentage: 15m);
            rule.CategoryIds = new List<Guid> { categoryId };
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Laptop",
                        UnitPrice = 100.00m,
                        Quantity = 1,
                        CategoryIds = new List<Guid> { categoryId } // In target category
                    },
                    new CartItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Book",
                        UnitPrice = 30.00m,
                        Quantity = 1,
                        CategoryIds = new List<Guid> { otherCategoryId } // Different category
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(130.00m, result.OriginalTotal);
            Assert.Equal(115.00m, result.FinalTotal); // 100 - 15 + 30
            Assert.Equal(15.00m, result.TotalDiscountAmount);

            // Check that only the laptop got the discount
            var laptopItem = result.CartItems.First(i => i.ProductName == "Laptop");
            Assert.Equal(85.00m, laptopItem.FinalLineTotal); // 100 - 15
            Assert.Single(laptopItem.AppliedPromotions);

            var bookItem = result.CartItems.First(i => i.ProductName == "Book");
            Assert.Equal(30.00m, bookItem.FinalLineTotal); // No discount
            Assert.Empty(bookItem.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_CartAmountDiscount_RequiresMinimumAmount()
        {
            // Arrange
            var promotion = CreatePromotion("$10 off $50+", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.CartAmountDiscount,
                discountAmount: 10m, minOrderAmount: 50m);
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            // Test below minimum
            var applyDtoBelow = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        UnitPrice = 40.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Test above minimum
            var applyDtoAbove = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        UnitPrice = 60.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var resultBelow = await _promotionService.ApplyPromotionRulesAsync(applyDtoBelow);
            var resultAbove = await _promotionService.ApplyPromotionRulesAsync(applyDtoAbove);

            // Assert - Below minimum
            Assert.True(resultBelow.Success);
            Assert.Equal(40.00m, resultBelow.FinalTotal); // No discount
            Assert.Empty(resultBelow.AppliedPromotions);
            Assert.Contains("doesn't meet minimum", resultBelow.Messages.FirstOrDefault() ?? "");

            // Assert - Above minimum
            Assert.True(resultAbove.Success);
            Assert.Equal(50.00m, resultAbove.FinalTotal); // 60 - 10
            Assert.Single(resultAbove.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_BuyXGetY_CalculatesCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("Buy 2 Get 1 Free", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.BuyXGetY);
            rule.RequiredQuantity = 2;
            rule.FreeQuantity = 1;
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 15.00m,
                        Quantity = 5 // Should get 2 free (2 sets of buy 2 get 1)
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(75.00m, result.OriginalTotal); // 15 * 5
            Assert.Equal(45.00m, result.FinalTotal); // 75 - (2 * 15) for 2 free items
            Assert.Equal(30.00m, result.TotalDiscountAmount);
            Assert.Single(result.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_FixedPrice_OnlyAppliesWhenBeneficial()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("Fixed Price $12", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.FixedPrice);
            rule.FixedPrice = 12.00m;
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            // Test with higher price (beneficial)
            var applyDtoHigher = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Expensive Product",
                        UnitPrice = 20.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Test with lower price (not beneficial)
            var applyDtoLower = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Cheap Product",
                        UnitPrice = 8.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var resultHigher = await _promotionService.ApplyPromotionRulesAsync(applyDtoHigher);
            var resultLower = await _promotionService.ApplyPromotionRulesAsync(applyDtoLower);

            // Assert - Higher price (should apply)
            Assert.True(resultHigher.Success);
            Assert.Equal(12.00m, resultHigher.FinalTotal);
            Assert.Single(resultHigher.AppliedPromotions);

            // Assert - Lower price (should not apply)
            Assert.True(resultLower.Success);
            Assert.Equal(8.00m, resultLower.FinalTotal);
            Assert.Empty(resultLower.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_BundlePromotion_CalculatesCorrectly()
        {
            // Arrange
            var productAId = Guid.NewGuid();
            var productBId = Guid.NewGuid();

            var promotion = CreatePromotion("Bundle A+B for $60", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.Bundle);
            rule.FixedPrice = 60.00m;
            rule.Products = new List<PromotionRuleProduct>
            {
                new PromotionRuleProduct { ProductId = productAId },
                new PromotionRuleProduct { ProductId = productBId }
            };
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productAId,
                        ProductName = "Product A",
                        UnitPrice = 30.00m,
                        Quantity = 1
                    },
                    new CartItemDto
                    {
                        ProductId = productBId,
                        ProductName = "Product B",
                        UnitPrice = 40.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(70.00m, result.OriginalTotal); // 30 + 40
            Assert.Equal(60.00m, result.FinalTotal); // Bundle price
            Assert.Equal(10.00m, result.TotalDiscountAmount);
            Assert.Single(result.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_CouponRequired_OnlyAppliesWithCoupon()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("Coupon 20% Off", priority: 1);
            promotion.CouponCode = "SAVE20";
            var rule = CreatePromotionRule(promotion, PromotionRuleType.Coupon, discountPercentage: 20m);
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var baseDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 50.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Test without coupon
            var withoutCoupon = baseDto;
            withoutCoupon.CouponCodes = new List<string>();

            // Test with coupon
            var withCoupon = new ApplyPromotionRulesDto
            {
                CartItems = baseDto.CartItems,
                CouponCodes = new List<string> { "SAVE20" },
                Currency = "EUR"
            };

            // Act
            var resultWithoutCoupon = await _promotionService.ApplyPromotionRulesAsync(withoutCoupon);
            var resultWithCoupon = await _promotionService.ApplyPromotionRulesAsync(withCoupon);

            // Assert - Without coupon
            Assert.True(resultWithoutCoupon.Success);
            Assert.Equal(50.00m, resultWithoutCoupon.FinalTotal); // No discount
            Assert.Empty(resultWithoutCoupon.AppliedPromotions);

            // Assert - With coupon
            Assert.True(resultWithCoupon.Success);
            Assert.Equal(40.00m, resultWithCoupon.FinalTotal); // 50 - 10
            Assert.Single(resultWithCoupon.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_TimeLimited_HonorsOrderDateTime()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("Time Limited 15% Off", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.TimeLimited, discountPercentage: 15m);
            rule.ValidDays = new List<DayOfWeek> { DayOfWeek.Monday };
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var baseDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 100.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Test on Monday (valid day)
            var mondayDto = baseDto;
            mondayDto.OrderDateTime = new DateTime(2023, 12, 4); // A Monday

            // Test on Tuesday (invalid day)
            var tuesdayDto = new ApplyPromotionRulesDto
            {
                CartItems = baseDto.CartItems,
                OrderDateTime = new DateTime(2023, 12, 5), // A Tuesday
                Currency = "EUR"
            };

            // Act
            var mondayResult = await _promotionService.ApplyPromotionRulesAsync(mondayDto);
            var tuesdayResult = await _promotionService.ApplyPromotionRulesAsync(tuesdayDto);

            // Assert - Monday (should apply)
            Assert.True(mondayResult.Success);
            Assert.Equal(85.00m, mondayResult.FinalTotal); // 100 - 15
            Assert.Single(mondayResult.AppliedPromotions);

            // Assert - Tuesday (should not apply)
            Assert.True(tuesdayResult.Success);
            Assert.Equal(100.00m, tuesdayResult.FinalTotal); // No discount
            Assert.Empty(tuesdayResult.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_ExclusiveRule_StopsFurtherApplications()
        {
            // Arrange
            var productId = Guid.NewGuid();

            // Exclusive promotion with higher priority
            var exclusivePromotion = CreatePromotion("Exclusive 30% Off", priority: 10);
            var exclusiveRule = CreatePromotionRule(exclusivePromotion, PromotionRuleType.Exclusive, discountPercentage: 30m);
            exclusivePromotion.Rules.Add(exclusiveRule);

            // Regular promotion with lower priority
            var regularPromotion = CreatePromotion("Regular 10% Off", priority: 5);
            var regularRule = CreatePromotionRule(regularPromotion, PromotionRuleType.Discount, discountPercentage: 10m);
            regularPromotion.Rules.Add(regularRule);

            await _context.Promotions.AddRangeAsync(exclusivePromotion, regularPromotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 100.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(70.00m, result.FinalTotal); // Only exclusive discount applied (100 - 30)
            Assert.Single(result.AppliedPromotions);
            Assert.Equal("Exclusive 30% Off", result.AppliedPromotions.First().PromotionName);
            Assert.Contains("stopping further applications", result.Messages.FirstOrDefault() ?? "");
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_NonCombinablePromotions_LocksAffectedLines()
        {
            // Arrange
            var productAId = Guid.NewGuid();
            var productBId = Guid.NewGuid();

            // Non-combinable promotion for Product A (higher priority)
            var nonCombinablePromotion = CreatePromotion("Non-Combinable 25% Off A", priority: 10, isCombinable: false);
            var nonCombinableRule = CreatePromotionRule(nonCombinablePromotion, PromotionRuleType.Discount, discountPercentage: 25m);
            nonCombinableRule.Products = new List<PromotionRuleProduct>
            {
                new PromotionRuleProduct { ProductId = productAId }
            };
            nonCombinablePromotion.Rules.Add(nonCombinableRule);

            // Regular combinable promotion for all products (lower priority)
            var combinablePromotion = CreatePromotion("Combinable 10% Off All", priority: 5, isCombinable: true);
            var combinableRule = CreatePromotionRule(combinablePromotion, PromotionRuleType.Discount, discountPercentage: 10m);
            combinablePromotion.Rules.Add(combinableRule);

            await _context.Promotions.AddRangeAsync(nonCombinablePromotion, combinablePromotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productAId,
                        ProductName = "Product A",
                        UnitPrice = 100.00m,
                        Quantity = 1
                    },
                    new CartItemDto
                    {
                        ProductId = productBId,
                        ProductName = "Product B",
                        UnitPrice = 50.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);

            // Product A should only have the non-combinable discount (25%)
            var productAItem = result.CartItems.First(i => i.ProductName == "Product A");
            Assert.Equal(75.00m, productAItem.FinalLineTotal); // 100 - 25
            Assert.Single(productAItem.AppliedPromotions);
            Assert.Equal("Non-Combinable 25% Off A", productAItem.AppliedPromotions.First().PromotionName);

            // Product B should only have the combinable discount (10%)
            var productBItem = result.CartItems.First(i => i.ProductName == "Product B");
            Assert.Equal(45.00m, productBItem.FinalLineTotal); // 50 - 5
            Assert.Single(productBItem.AppliedPromotions);
            Assert.Equal("Combinable 10% Off All", productBItem.AppliedPromotions.First().PromotionName);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_CurrencyRounding_UsesAwayFromZero()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotion = CreatePromotion("Precise Percentage", priority: 1);
            var rule = CreatePromotionRule(promotion, PromotionRuleType.Discount, discountPercentage: 33.333m);
            promotion.Rules.Add(rule);

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 3.00m, // 33.333% of 3.00 = 0.99999, should round to 1.00
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2.00m, result.FinalTotal); // 3.00 - 1.00 (rounded discount)
            Assert.Equal(1.00m, result.TotalDiscountAmount); // Should be exactly 1.00 due to rounding
        }

        #region Helper Methods

        private Promotion CreatePromotion(string name, int priority, bool isCombinable = true)
        {
            return new Promotion
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = name,
                Description = $"Test promotion: {name}",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                Priority = priority,
                IsCombinable = isCombinable,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Rules = new List<PromotionRule>()
            };
        }

        private PromotionRule CreatePromotionRule(
            Promotion promotion,
            PromotionRuleType ruleType,
            decimal? discountPercentage = null,
            decimal? discountAmount = null,
            decimal? minOrderAmount = null)
        {
            return new PromotionRule
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                PromotionId = promotion.Id,
                Promotion = promotion,
                RuleType = ruleType,
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount,
                MinOrderAmount = minOrderAmount,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user"
            };
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
            _memoryCache.Dispose();
        }
    }
}