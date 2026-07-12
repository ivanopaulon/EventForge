using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.RetailCart;
using EventForge.Server.Services.Tenants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Promotions;
using Prym.DTOs.RetailCart;

namespace EventForge.Tests.Services.RetailCart;

/// <summary>
/// Cross-tenant isolation tests for <see cref="RetailCartSessionService"/>.
/// Verifies the security requirement described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 1):
/// cart sessions are stored under a composite key that includes the tenant id, so a session created by
/// Tenant A can never be resolved, modified, or removed by a caller whose tenant context is Tenant B.
/// </summary>
[Trait("Category", "Unit")]
public class RetailCartSessionServiceTenantIsolationTests
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    private static RetailCartSessionService CreateService(Guid tenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(tenantId);

        var mockPromotionService = new Mock<IPromotionService>();
        mockPromotionService
            .Setup(x => x.ApplyPromotionRulesAsync(It.IsAny<ApplyPromotionRulesDto>(), default))
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

        return new RetailCartSessionService(
            mockTenantContext.Object,
            mockPromotionService.Object,
            new Mock<ILogger<RetailCartSessionService>>().Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task RemoveItemAsync_FromOtherTenant_DoesNotAffectSession()
    {
        var serviceA = CreateService(_tenantAId);
        var serviceB = CreateService(_tenantBId);

        var sessionDto = await serviceA.CreateSessionAsync(new CreateCartSessionDto { Currency = "EUR" });
        var addedSession = await serviceA.AddItemAsync(sessionDto.Id, new AddCartItemDto
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P1",
            ProductName = "Product 1",
            UnitPrice = 10m,
            Quantity = 2
        });

        var itemId = addedSession!.Items.Single().Id;

        // Tenant B tries to remove Tenant A's item using the same session/item ids.
        var resultForB = await serviceB.RemoveItemAsync(sessionDto.Id, itemId);
        Assert.Null(resultForB);

        // Tenant A's session and item must be untouched.
        var sessionForA = await serviceA.GetSessionAsync(sessionDto.Id);
        Assert.NotNull(sessionForA);
        Assert.Single(sessionForA!.Items);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_FromOtherTenant_DoesNotAffectSession()
    {
        var serviceA = CreateService(_tenantAId);
        var serviceB = CreateService(_tenantBId);

        var sessionDto = await serviceA.CreateSessionAsync(new CreateCartSessionDto { Currency = "EUR" });
        var addedSession = await serviceA.AddItemAsync(sessionDto.Id, new AddCartItemDto
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P1",
            ProductName = "Product 1",
            UnitPrice = 10m,
            Quantity = 2
        });

        var itemId = addedSession!.Items.Single().Id;

        var resultForB = await serviceB.UpdateItemQuantityAsync(sessionDto.Id, itemId, new UpdateCartItemDto { Quantity = 99 });
        Assert.Null(resultForB);

        var sessionForA = await serviceA.GetSessionAsync(sessionDto.Id);
        Assert.NotNull(sessionForA);
        Assert.Equal(2, sessionForA!.Items.Single().Quantity);
    }
}
