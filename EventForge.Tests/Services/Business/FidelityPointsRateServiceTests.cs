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
    private readonly Guid _bronzeTierId = Guid.NewGuid();
    private readonly Guid _goldTierId = Guid.NewGuid();
    private readonly Guid _platinumTierId = Guid.NewGuid();

    public FidelityPointsRateServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockTenantContext = new Mock<ITenantContext>();
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _context.FidelityTiers.AddRange(
            new FidelityTier { Id = _bronzeTierId, TenantId = _tenantId, Name = "Bronze", SortOrder = 0 },
            new FidelityTier { Id = _goldTierId, TenantId = _tenantId, Name = "Gold", SortOrder = 2 },
            new FidelityTier { Id = _platinumTierId, TenantId = _tenantId, Name = "Platinum", SortOrder = 3 });
        _ = _context.SaveChanges();

        _service = new FidelityPointsRateService(_context, _mockTenantContext.Object);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_NoEntities_ReturnsDefaultRateAndFloorRounding()
    {
        var result = await _service.GetEffectiveRateAsync(_goldTierId);

        Assert.Equal(1m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Floor, result.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_WithBaseRateAndNoActiveCampaign_UsesBaseRateOnly()
    {
        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling,
            EffectiveFrom = DateTime.UtcNow.AddDays(-5)
        });
        _ = await _context.SaveChangesAsync();

        var result = await _service.GetEffectiveRateAsync(_goldTierId);

        // Without an active campaign there is no tier differentiation, by design.
        Assert.Equal(2m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Ceiling, result.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_WithActiveCampaignAndPerCampaignTierMultiplier_CombinesValues()
    {
        var now = DateTime.UtcNow;

        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Floor,
            EffectiveFrom = now.AddDays(-5)
        });

        var campaign = new FidelityPointsCampaign
        {
            TenantId = _tenantId,
            Name = "Summer Bonus",
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(1),
            Multiplier = 3m,
            RoundingMode = FidelityPointsRoundingMode.Nearest
        };
        _context.FidelityPointsCampaigns.Add(campaign);
        _ = await _context.SaveChangesAsync();

        _context.FidelityTierMultipliers.AddRange(
            new FidelityTierMultiplier
            {
                TenantId = _tenantId,
                CampaignId = campaign.Id,
                TierId = _goldTierId,
                Multiplier = 1.5m
            },
            new FidelityTierMultiplier
            {
                TenantId = _tenantId,
                CampaignId = campaign.Id,
                TierId = _platinumTierId,
                Multiplier = 2.5m
            });
        _ = await _context.SaveChangesAsync();

        var goldResult = await _service.GetEffectiveRateAsync(_goldTierId);
        var platinumResult = await _service.GetEffectiveRateAsync(_platinumTierId);
        // Bronze has no multiplier configured in this campaign — defaults to 1.0.
        var bronzeResult = await _service.GetEffectiveRateAsync(_bronzeTierId);

        Assert.Equal(9m, goldResult.Rate); // baseRate (2) * campaign tier multiplier for Gold (1.5) * campaign multiplier (3) = 9
        Assert.Equal(15m, platinumResult.Rate); // baseRate (2) * campaign tier multiplier for Platinum (2.5) * campaign multiplier (3) = 15
        Assert.Equal(6m, bronzeResult.Rate); // baseRate (2) * default tier multiplier (1.0, none configured for Bronze) * campaign multiplier (3) = 6
        Assert.Equal(FidelityPointsRoundingMode.Nearest, goldResult.Rounding);
    }

    [Fact]
    public async Task GetEffectiveRateAsync_MultiplierFromDifferentCampaign_IsNotApplied()
    {
        var now = DateTime.UtcNow;

        _context.FidelityPointsBaseRates.Add(new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Floor,
            EffectiveFrom = now.AddDays(-5)
        });

        var pastCampaign = new FidelityPointsCampaign
        {
            TenantId = _tenantId,
            Name = "Past Campaign",
            StartDate = now.AddMonths(-2),
            EndDate = now.AddMonths(-1),
            Multiplier = 5m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling
        };
        var activeCampaign = new FidelityPointsCampaign
        {
            TenantId = _tenantId,
            Name = "Active Campaign",
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(1),
            Multiplier = 3m,
            RoundingMode = FidelityPointsRoundingMode.Nearest
        };
        _context.FidelityPointsCampaigns.AddRange(pastCampaign, activeCampaign);
        _ = await _context.SaveChangesAsync();

        // Multiplier belongs to the past (inactive) campaign — must not affect the active one.
        _context.FidelityTierMultipliers.Add(new FidelityTierMultiplier
        {
            TenantId = _tenantId,
            CampaignId = pastCampaign.Id,
            TierId = _goldTierId,
            Multiplier = 10m
        });
        _ = await _context.SaveChangesAsync();

        var result = await _service.GetEffectiveRateAsync(_goldTierId);

        Assert.Equal(6m, result.Rate); // baseRate (2) * default tier multiplier (1.0, past campaign's multiplier does not apply) * active campaign multiplier (3) = 6
        Assert.Equal(FidelityPointsRoundingMode.Nearest, result.Rounding);
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
        var otherCampaign = new FidelityPointsCampaign
        {
            TenantId = otherTenantId,
            Name = "Other Tenant Campaign",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            Multiplier = 10m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling
        };
        _context.FidelityPointsCampaigns.Add(otherCampaign);
        _ = await _context.SaveChangesAsync();

        _context.FidelityTierMultipliers.Add(new FidelityTierMultiplier
        {
            TenantId = otherTenantId,
            CampaignId = otherCampaign.Id,
            TierId = _bronzeTierId,
            Multiplier = 4m
        });
        _ = await _context.SaveChangesAsync();

        var result = await _service.GetEffectiveRateAsync(_bronzeTierId);

        Assert.Equal(1m, result.Rate);
        Assert.Equal(FidelityPointsRoundingMode.Floor, result.Rounding);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
