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
    /// Unit tests for PromotionService focusing on promotion engine functionality.
    /// </summary>
    [Trait("Category", "Unit")]
    public class PromotionServiceTests : IDisposable
    {
        private readonly EventForgeDbContext _context;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<PromotionService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly PromotionService _promotionService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public PromotionServiceTests()
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
            _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

            // Create service
            _promotionService = new PromotionService(
                _context,
                _mockAuditLogService.Object,
                _mockTenantContext.Object,
                _mockLogger.Object,
                _memoryCache);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithEmptyCart_ReturnsValidationError()
        {
            // Arrange
            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>(),
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cart cannot be empty", result.Messages);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithInvalidCurrency_ReturnsValidationError()
        {
            // Arrange
            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        UnitPrice = 10.00m,
                        Quantity = 1
                    }
                },
                Currency = ""
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Currency is required", result.Messages);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithNegativePrice_ReturnsValidationError()
        {
            // Arrange
            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        UnitPrice = -10.00m,
                        Quantity = 1
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Unit price cannot be negative for product Test Product", result.Messages);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithValidCartNoPromotions_ReturnsSuccessWithOriginalPrices()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 10.00m,
                        Quantity = 2,
                        ExistingLineDiscount = 0m
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(20.00m, result.OriginalTotal);
            Assert.Equal(20.00m, result.FinalTotal);
            Assert.Equal(0m, result.TotalDiscountAmount);
            _ = Assert.Single(result.CartItems);

            var cartItem = result.CartItems.First();
            Assert.Equal(productId, cartItem.ProductId);
            Assert.Equal(20.00m, cartItem.OriginalLineTotal);
            Assert.Equal(20.00m, cartItem.FinalLineTotal);
            Assert.Equal(0m, cartItem.PromotionDiscount);
            Assert.Empty(cartItem.AppliedPromotions);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithExistingLineDiscount_AppliesCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 10.00m,
                        Quantity = 2,
                        ExistingLineDiscount = 10m // 10% existing discount
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(20.00m, result.OriginalTotal);
            Assert.Equal(18.00m, result.FinalTotal); // 20 - 10% = 18
            Assert.Equal(2.00m, result.TotalDiscountAmount);

            var cartItem = result.CartItems.First();
            Assert.Equal(20.00m, cartItem.OriginalLineTotal);
            Assert.Equal(18.00m, cartItem.FinalLineTotal);
            Assert.Equal(10m, cartItem.EffectiveDiscountPercentage);
        }

        [Fact]
        public async Task ApplyPromotionRulesAsync_WithPercentagePromotion_AppliesCorrectDiscount()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();
            var ruleId = Guid.NewGuid();

            // Create test promotion and rule in database
            var promotion = new Promotion
            {
                Id = promotionId,
                Name = "10% Off",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Priority = 1,
                IsCombinable = true,
                TenantId = _tenantId,
                IsActive = true,
                Rules = new List<PromotionRule>
                {
                    new PromotionRule
                    {
                        Id = ruleId,
                        PromotionId = promotionId,
                        RuleType = PromotionRuleType.Discount,
                        DiscountPercentage = 10m,
                        TenantId = _tenantId,
                        IsActive = true
                    }
                }
            };

            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        UnitPrice = 10.00m,
                        Quantity = 2,
                        ExistingLineDiscount = 0m
                    }
                },
                Currency = "EUR"
            };

            // Act
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(20.00m, result.OriginalTotal);
            Assert.Equal(18.00m, result.FinalTotal); // 20 - 10% = 18
            Assert.Equal(2.00m, result.TotalDiscountAmount);

            var cartItem = result.CartItems.First();
            Assert.Equal(20.00m, cartItem.OriginalLineTotal);
            Assert.Equal(18.00m, cartItem.FinalLineTotal);
            Assert.Equal(2.00m, cartItem.PromotionDiscount);
            Assert.Equal(10m, cartItem.EffectiveDiscountPercentage);
            _ = Assert.Single(cartItem.AppliedPromotions);

            var appliedPromotion = cartItem.AppliedPromotions.First();
            Assert.Equal(promotionId, appliedPromotion.PromotionId);
            Assert.Equal("10% Off", appliedPromotion.PromotionName);
            Assert.Equal(2.00m, appliedPromotion.DiscountAmount);
            Assert.Equal(10m, appliedPromotion.DiscountPercentage);
        }

        public void Dispose()
        {
            _context.Dispose();
            _memoryCache.Dispose();
        }

        #region ValidateCouponAsync Tests

        [Fact]
        public async Task ValidateCouponAsync_WithValidCoupon_ReturnsPromotionDto()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = promotionId,
                Name = "Summer Sale",
                CouponCode = "SUMMER10",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = 100,
                CurrentUses = 5,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.ValidateCouponAsync("SUMMER10");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promotionId, result.Id);
            Assert.Equal("Summer Sale", result.Name);
            Assert.Equal(5, result.CurrentUses);
            Assert.Equal(95, result.RemainingUses);
        }

        [Fact]
        public async Task ValidateCouponAsync_WithCaseInsensitiveCoupon_ReturnsPromotionDto()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Case Test",
                CouponCode = "UPPER10",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.ValidateCouponAsync("upper10");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Case Test", result.Name);
        }

        [Fact]
        public async Task ValidateCouponAsync_WithExpiredPromotion_ReturnsNull()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Expired Promo",
                CouponCode = "EXPIRED",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.ValidateCouponAsync("EXPIRED");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateCouponAsync_WithMaxUsesReached_ReturnsNull()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Limited Promo",
                CouponCode = "LIMITED",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = 10,
                CurrentUses = 10,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.ValidateCouponAsync("LIMITED");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateCouponAsync_WithNullMaxUses_ReturnsPromotion()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Unlimited Promo",
                CouponCode = "UNLIMITED",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = null,
                CurrentUses = 999,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.ValidateCouponAsync("UNLIMITED");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RemainingUses);
        }

        [Fact]
        public async Task ValidateCouponAsync_WithEmptyCouponCode_ReturnsNull()
        {
            // Act
            var result = await _promotionService.ValidateCouponAsync(string.Empty);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region IncrementUsageAsync Tests

        [Fact]
        public async Task IncrementUsageAsync_WithValidPromotion_IncrementsCounter()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = promotionId,
                Name = "Increment Test",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = 10,
                CurrentUses = 3,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.IncrementUsageAsync(promotionId);

            // Assert
            Assert.True(result);
            var updated = await _context.Promotions.FindAsync(promotionId);
            Assert.Equal(4, updated!.CurrentUses);
        }

        [Fact]
        public async Task IncrementUsageAsync_WithMaxUsesReached_ReturnsFalse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = promotionId,
                Name = "MaxUses Test",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = 5,
                CurrentUses = 5,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.IncrementUsageAsync(promotionId);

            // Assert
            Assert.False(result);
            var unchanged = await _context.Promotions.FindAsync(promotionId);
            Assert.Equal(5, unchanged!.CurrentUses);
        }

        [Fact]
        public async Task IncrementUsageAsync_WithNullMaxUses_IncrementsUnlimited()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = promotionId,
                Name = "Unlimited Increment Test",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                MaxUses = null,
                CurrentUses = 999,
                TenantId = _tenantId,
                IsActive = true
            };
            _ = _context.Promotions.Add(promotion);
            _ = await _context.SaveChangesAsync();

            // Act
            var result = await _promotionService.IncrementUsageAsync(promotionId);

            // Assert
            Assert.True(result);
            var updated = await _context.Promotions.FindAsync(promotionId);
            Assert.Equal(1000, updated!.CurrentUses);
        }

        [Fact]
        public async Task IncrementUsageAsync_WithNonExistentPromotion_ReturnsFalse()
        {
            // Act
            var result = await _promotionService.IncrementUsageAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}