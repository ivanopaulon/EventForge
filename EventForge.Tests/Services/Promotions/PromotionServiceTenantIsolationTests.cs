using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Promotions;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Monitoring;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Promotions;

namespace EventForge.Tests.Services.Promotions;

/// <summary>
/// Cross-tenant isolation tests for <see cref="PromotionService"/>.
/// Closes the security gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 1).
/// </summary>
[Trait("Category", "Unit")]
public class PromotionServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _promotionAId = Guid.NewGuid();
    private readonly Guid _ruleAId = Guid.NewGuid();
    private readonly Guid _ruleProductAId = Guid.NewGuid();
    private readonly Guid _productAId = Guid.NewGuid();

    public PromotionServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _context.Promotions.Add(new Promotion
        {
            Id = _promotionAId,
            TenantId = _tenantAId,
            Name = "Promotion A",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        });

        _context.PromotionRules.Add(new PromotionRule
        {
            Id = _ruleAId,
            TenantId = _tenantAId,
            PromotionId = _promotionAId,
            RuleType = PromotionRuleType.Discount,
            DiscountPercentage = 10m
        });

        _context.PromotionRuleProducts.Add(new PromotionRuleProduct
        {
            Id = _ruleProductAId,
            TenantId = _tenantAId,
            PromotionRuleId = _ruleAId,
            ProductId = _productAId
        });

        _context.SaveChanges();
    }

    private PromotionService CreateService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new PromotionService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<PromotionService>>().Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<IMonitoringMetricsService>().Object);
    }

    [Fact]
    public async Task UpdatePromotionAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdatePromotionAsync(_promotionAId, new UpdatePromotionDto
        {
            Name = "Hacked",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        }, "attacker");

        Assert.Null(result);

        var unchanged = await _context.Promotions.AsNoTracking().FirstAsync(p => p.Id == _promotionAId);
        Assert.Equal("Promotion A", unchanged.Name);
    }

    [Fact]
    public async Task DeletePromotionAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeletePromotionAsync(_promotionAId, "attacker");

        Assert.False(result);

        var unchanged = await _context.Promotions.AsNoTracking().FirstAsync(p => p.Id == _promotionAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdatePromotionRuleAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdatePromotionRuleAsync(_promotionAId, _ruleAId, new UpdatePromotionRuleDto
        {
            RuleType = "Discount",
            DiscountPercentage = 99m
        }, "attacker");

        Assert.Null(result);

        var unchanged = await _context.PromotionRules.AsNoTracking().FirstAsync(r => r.Id == _ruleAId);
        Assert.Equal(10m, unchanged.DiscountPercentage);
    }

    [Fact]
    public async Task DeletePromotionRuleAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeletePromotionRuleAsync(_promotionAId, _ruleAId, "attacker");

        Assert.False(result);

        var unchanged = await _context.PromotionRules.AsNoTracking().FirstAsync(r => r.Id == _ruleAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task RemoveRuleProductAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.RemoveRuleProductAsync(_promotionAId, _ruleAId, _productAId, "attacker");

        Assert.False(result);

        var unchanged = await _context.PromotionRuleProducts.AsNoTracking().FirstAsync(rp => rp.Id == _ruleProductAId);
        Assert.False(unchanged.IsDeleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
