using EventForge.DTOs.Promotions;
using EventForge.Server.Data.Entities.Promotions;
using EventForge.Server.Services.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.Promotions
{
    /// <summary>
    /// Unit tests for PromotionService focusing on promotion engine functionality.
    /// </summary>
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
            Assert.Contains("Unit price cannot be negative", result.Messages);
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
            Assert.Single(result.CartItems);
            
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

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

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
            Assert.Single(cartItem.AppliedPromotions);
            
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
    }
}