using EventForge.DTOs.Promotions;
using EventForge.DTOs.RetailCart;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.RetailCart;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventForge.Tests.Services.RetailCart
{
    /// <summary>
    /// Unit tests for RetailCartSessionService to ensure cart management functionality.
    /// </summary>
    public class RetailCartSessionServiceTests : IDisposable
    {
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly Mock<ILogger<RetailCartSessionService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly RetailCartSessionService _cartSessionService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public RetailCartSessionServiceTests()
        {
            // Create mocks
            _mockTenantContext = new Mock<ITenantContext>();
            _mockPromotionService = new Mock<IPromotionService>();
            _mockLogger = new Mock<ILogger<RetailCartSessionService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Setup tenant context
            _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

            // Setup default promotion service response (no promotions)
            _mockPromotionService.Setup(x => x.ApplyPromotionRulesAsync(It.IsAny<ApplyPromotionRulesDto>(), default))
                .ReturnsAsync((ApplyPromotionRulesDto dto, System.Threading.CancellationToken ct) => 
                    new PromotionApplicationResultDto
                    {
                        OriginalTotal = dto.CartItems.Sum(i => i.UnitPrice * i.Quantity),
                        FinalTotal = dto.CartItems.Sum(i => i.UnitPrice * i.Quantity),
                        TotalDiscountAmount = 0m,
                        Success = true,
                        CartItems = dto.CartItems.Select(item => new CartItemResultDto
                        {
                            ProductId = item.ProductId,
                            ProductCode = item.ProductCode,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            CategoryIds = item.CategoryIds,
                            OriginalLineTotal = item.UnitPrice * item.Quantity,
                            FinalLineTotal = item.UnitPrice * item.Quantity,
                            PromotionDiscount = 0m,
                            EffectiveDiscountPercentage = 0m,
                            AppliedPromotions = new List<AppliedPromotionDto>()
                        }).ToList(),
                        AppliedPromotions = new List<AppliedPromotionDto>()
                    });

            // Create service
            _cartSessionService = new RetailCartSessionService(
                _mockTenantContext.Object,
                _mockPromotionService.Object,
                _mockLogger.Object,
                _memoryCache);
        }

        [Fact]
        public async Task CreateSessionAsync_CreatesNewSession_WithCorrectProperties()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var createDto = new CreateCartSessionDto
            {
                CustomerId = customerId,
                SalesChannel = "online",
                Currency = "USD"
            };

            // Act
            var result = await _cartSessionService.CreateSessionAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal("online", result.SalesChannel);
            Assert.Equal("USD", result.Currency);
            Assert.Empty(result.Items);
            Assert.Empty(result.CouponCodes);
            Assert.Equal(0m, result.OriginalTotal);
            Assert.Equal(0m, result.FinalTotal);
            Assert.Equal(0m, result.TotalDiscountAmount);
        }

        [Fact]
        public async Task GetSessionAsync_ReturnsNull_WhenSessionNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _cartSessionService.GetSessionAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddItemAsync_AddsNewItem_CalculatesTotals()
        {
            // Arrange
            var session = await CreateTestSession();
            var productId = Guid.NewGuid();
            var addItemDto = new AddCartItemDto
            {
                ProductId = productId,
                ProductCode = "SKU123",
                ProductName = "Test Product",
                UnitPrice = 25.50m,
                Quantity = 2,
                CategoryIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = await _cartSessionService.AddItemAsync(session.Id, addItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            
            var item = result.Items.First();
            Assert.Equal(productId, item.ProductId);
            Assert.Equal("SKU123", item.ProductCode);
            Assert.Equal("Test Product", item.ProductName);
            Assert.Equal(25.50m, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(51.00m, item.OriginalLineTotal);
            Assert.Equal(51.00m, item.FinalLineTotal);
            
            Assert.Equal(51.00m, result.OriginalTotal);
            Assert.Equal(51.00m, result.FinalTotal);
        }

        [Fact]
        public async Task AddItemAsync_ExistingProduct_IncrementsQuantity()
        {
            // Arrange
            var session = await CreateTestSession();
            var productId = Guid.NewGuid();
            var addItemDto = new AddCartItemDto
            {
                ProductId = productId,
                ProductName = "Test Product",
                UnitPrice = 10.00m,
                Quantity = 1
            };

            // Add item first time
            await _cartSessionService.AddItemAsync(session.Id, addItemDto);

            // Add same item again
            addItemDto.Quantity = 2;

            // Act
            var result = await _cartSessionService.AddItemAsync(session.Id, addItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            
            var item = result.Items.First();
            Assert.Equal(3, item.Quantity); // 1 + 2
            Assert.Equal(30.00m, result.OriginalTotal); // 10 * 3
        }

        [Fact]
        public async Task RemoveItemAsync_RemovesItem_RecalculatesTotals()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            var itemId = session.Items.First().Id;

            // Act
            var result = await _cartSessionService.RemoveItemAsync(session.Id, itemId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0m, result.OriginalTotal);
            Assert.Equal(0m, result.FinalTotal);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_UpdatesQuantity_RecalculatesTotals()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            var itemId = session.Items.First().Id;
            var updateDto = new UpdateCartItemDto
            {
                Quantity = 5
            };

            // Act
            var result = await _cartSessionService.UpdateItemQuantityAsync(session.Id, itemId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            
            var item = result.Items.First();
            Assert.Equal(5, item.Quantity);
            Assert.Equal(50.00m, result.OriginalTotal); // 10 * 5
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_ZeroQuantity_RemovesItem()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            var itemId = session.Items.First().Id;
            var updateDto = new UpdateCartItemDto
            {
                Quantity = 0
            };

            // Act
            var result = await _cartSessionService.UpdateItemQuantityAsync(session.Id, itemId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0m, result.OriginalTotal);
        }

        [Fact]
        public async Task ApplyCouponsAsync_SetsCoupons_CallsPromotionService()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            var applyCouponsDto = new ApplyCouponsDto
            {
                CouponCodes = new List<string> { "SAVE10", "WELCOME" }
            };

            // Setup promotion service to return a discount
            _mockPromotionService.Setup(x => x.ApplyPromotionRulesAsync(It.IsAny<ApplyPromotionRulesDto>(), default))
                .ReturnsAsync((ApplyPromotionRulesDto dto, System.Threading.CancellationToken ct) => 
                    new PromotionApplicationResultDto
                    {
                        OriginalTotal = 10.00m,
                        FinalTotal = 9.00m,
                        TotalDiscountAmount = 1.00m,
                        Success = true,
                        CartItems = dto.CartItems.Select(item => new CartItemResultDto
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            OriginalLineTotal = 10.00m,
                            FinalLineTotal = 9.00m,
                            PromotionDiscount = 1.00m,
                            EffectiveDiscountPercentage = 10m,
                            AppliedPromotions = new List<AppliedPromotionDto>
                            {
                                new AppliedPromotionDto
                                {
                                    PromotionName = "Test Promotion",
                                    DiscountAmount = 1.00m
                                }
                            }
                        }).ToList(),
                        AppliedPromotions = new List<AppliedPromotionDto>
                        {
                            new AppliedPromotionDto
                            {
                                PromotionName = "Test Promotion",
                                DiscountAmount = 1.00m
                            }
                        }
                    });

            // Act
            var result = await _cartSessionService.ApplyCouponsAsync(session.Id, applyCouponsDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.CouponCodes.Count);
            Assert.Contains("SAVE10", result.CouponCodes);
            Assert.Contains("WELCOME", result.CouponCodes);
            Assert.Equal(9.00m, result.FinalTotal);
            Assert.Equal(1.00m, result.TotalDiscountAmount);
            Assert.Single(result.AppliedPromotions);

            // Verify promotion service was called with coupons
            _mockPromotionService.Verify(x => x.ApplyPromotionRulesAsync(
                It.Is<ApplyPromotionRulesDto>(dto => 
                    dto.CouponCodes != null && 
                    dto.CouponCodes.Contains("SAVE10") && 
                    dto.CouponCodes.Contains("WELCOME")), 
                default), Times.Once);
        }

        [Fact]
        public async Task ClearAsync_RemovesAllItems_ResetsState()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            
            // Add coupons first
            await _cartSessionService.ApplyCouponsAsync(session.Id, new ApplyCouponsDto
            {
                CouponCodes = new List<string> { "TEST" }
            });

            // Act
            var result = await _cartSessionService.ClearAsync(session.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Empty(result.CouponCodes);
            Assert.Equal(0m, result.OriginalTotal);
            Assert.Equal(0m, result.FinalTotal);
            Assert.Equal(0m, result.TotalDiscountAmount);
        }

        [Fact]
        public async Task GetTotalsAsync_RecalculatesPromotions()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();

            // Act
            var result = await _cartSessionService.GetTotalsAsync(session.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10.00m, result.OriginalTotal);
            Assert.Equal(10.00m, result.FinalTotal);
            
            // Verify promotion service was called
            _mockPromotionService.Verify(x => x.ApplyPromotionRulesAsync(
                It.IsAny<ApplyPromotionRulesDto>(), default), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TenantIsolation_DifferentTenants_CannotAccessEachOthersSessions()
        {
            // Arrange
            var session = await CreateTestSession();
            
            // Change tenant context
            var otherTenantId = Guid.NewGuid();
            _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(otherTenantId);

            // Act
            var result = await _cartSessionService.GetSessionAsync(session.Id);

            // Assert
            Assert.Null(result); // Should not find session from different tenant
        }

        [Fact]
        public async Task PromotionServiceFailure_ReturnsSessionWithoutPromotions()
        {
            // Arrange
            var session = await CreateTestSessionWithItem();
            
            // Setup promotion service to throw exception
            _mockPromotionService.Setup(x => x.ApplyPromotionRulesAsync(It.IsAny<ApplyPromotionRulesDto>(), default))
                .ThrowsAsync(new InvalidOperationException("Promotion service unavailable"));

            // Act
            var result = await _cartSessionService.GetTotalsAsync(session.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10.00m, result.OriginalTotal);
            Assert.Equal(10.00m, result.FinalTotal); // Should fallback to original totals
            Assert.Equal(0m, result.TotalDiscountAmount);
            Assert.Empty(result.AppliedPromotions);
        }

        #region Helper Methods

        private async Task<CartSessionDto> CreateTestSession()
        {
            var createDto = new CreateCartSessionDto
            {
                SalesChannel = "test",
                Currency = "EUR"
            };

            return await _cartSessionService.CreateSessionAsync(createDto);
        }

        private async Task<CartSessionDto> CreateTestSessionWithItem()
        {
            var session = await CreateTestSession();
            
            var addItemDto = new AddCartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                UnitPrice = 10.00m,
                Quantity = 1
            };

            return await _cartSessionService.AddItemAsync(session.Id, addItemDto) ?? session;
        }

        #endregion

        public void Dispose()
        {
            _memoryCache.Dispose();
        }
    }
}