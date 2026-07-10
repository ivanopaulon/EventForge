using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EventForge.Tests.Services.Business;

[Trait("Category", "Unit")]
public class FidelityPointsRateServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly FidelityPointsRateService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public FidelityPointsRateServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockTenantContext = new Mock<ITenantContext>();
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _service = new FidelityPointsRateService(_context, _mockTenantContext.Object);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_NoEntities_ReturnsDefaultRateAndFloorRounding()
    {
        var result = await _service.GetEffectiveRateAsync(FidelityCardType.Gold);

        Assert.Equal(1m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Floor, result.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_WithBaseRateAndTierMultiplier_CombinesValues()
    {
        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling,
            EffectiveFrom = DateTime.UtcNow.AddDays(-5)
        });
        _context.FidelityTierMultipliers.Add(new FidelityTierMultiplier
        {
            TenantId = _tenantId,
            CardType = FidelityCardType.Gold,
            Multiplier = 1.5m
        });
        _ = await _context.SaveChangesAsync();

        var result = await _service.GetEffectiveRateAsync(FidelityCardType.Gold);

        Assert.Equal(3m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Ceiling, result.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_WithCampaignIgnoringTierMultiplier_ReturnsSameRateAcrossTiers()
    {
        var now = DateTime.UtcNow;

        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Floor,
            EffectiveFrom = now.AddDays(-5)
        });

        _context.FidelityTierMultipliers.AddRange(
            new FidelityTierMultiplier
            {
                TenantId = _tenantId,
                CardType = FidelityCardType.Bronze,
                Multiplier = 1.2m
            },
            new FidelityTierMultiplier
            {
                TenantId = _tenantId,
                CardType = FidelityCardType.Platinum,
                Multiplier = 2.5m
            });

        _context.FidelityPointsCampaigns.Add(new FidelityPointsCampaign
        {
            TenantId = _tenantId,
            Name = "Summer Bonus",
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(1),
            Multiplier = 3m,
            IgnoreTierMultiplier = true,
            RoundingMode = FidelityPointsRoundingMode.Nearest
        });
        _ = await _context.SaveChangesAsync();

        var bronzeResult = await _service.GetEffectiveRateAsync(FidelityCardType.Bronze);
        var platinumResult = await _service.GetEffectiveRateAsync(FidelityCardType.Platinum);

        Assert.Equal(6m, bronzeResult.Rate);
        Assert.Equal(6m, platinumResult.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Nearest, bronzeResult.Rounding);
        Assert.Equal(FidelityPointsRoundingMode.Nearest, platinumResult.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_EntitiesForOtherTenant_AreIgnored()
    {
        var otherTenantId = Guid.NewGuid();

        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = otherTenantId,
            Rate = 5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-2)
        });
        _context.FidelityTierMultipliers.Add(new FidelityTierMultiplier
        {
            TenantId = otherTenantId,
            CardType = FidelityCardType.Bronze,
            Multiplier = 4m
        });
        _context.FidelityPointsCampaigns.Add(new FidelityPointsCampaign
        {
            TenantId = otherTenantId,
            Name = "Other Tenant Campaign",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            Multiplier = 10m,
            IgnoreTierMultiplier = false,
            RoundingMode = FidelityPointsRoundingMode.Ceiling
        });
        _ = await _context.SaveChangesAsync();

        var result = await _service.GetEffectiveRateAsync(FidelityCardType.Bronze);

        Assert.Equal(1m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Floor, result.Rounding);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
