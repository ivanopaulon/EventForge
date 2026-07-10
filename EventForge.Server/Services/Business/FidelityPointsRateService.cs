using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Resolves the effective fidelity points accrual rate for the current tenant using
/// persisted EF entities for base rates, tier multipliers, and optional campaigns.
/// </summary>
public class FidelityPointsRateService(
    EventForgeDbContext context,
    ITenantContext tenantContext) : IFidelityPointsRateService
{
    public async Task<(decimal Rate, FidelityPointsRoundingMode Rounding)> GetEffectiveRateAsync(
        FidelityCardType cardType,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();
        var now = DateTime.UtcNow;

        var baseRate = await context.FidelityPointsBaseRates
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(rate => rate.EffectiveFrom <= now && (rate.EffectiveTo == null || rate.EffectiveTo >= now))
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ThenByDescending(rate => rate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? new FidelityPointsBaseRate();

        var tierMultiplier = await context.FidelityTierMultipliers
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(multiplier => multiplier.CardType == cardType)
            .Select(multiplier => (decimal?)multiplier.Multiplier)
            .FirstOrDefaultAsync(cancellationToken)
            ?? 1.0m;

        var activeCampaign = await context.FidelityPointsCampaigns
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(campaign => campaign.StartDate <= now && campaign.EndDate >= now)
            .OrderByDescending(campaign => campaign.StartDate)
            .ThenByDescending(campaign => campaign.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        decimal rate;
        var rounding = baseRate.RoundingMode;

        if (activeCampaign is not null)
        {
            rate = activeCampaign.IgnoreTierMultiplier
                ? baseRate.Rate * activeCampaign.Multiplier
                : baseRate.Rate * tierMultiplier * activeCampaign.Multiplier;
            rounding = activeCampaign.RoundingMode;
        }
        else
        {
            rate = baseRate.Rate * tierMultiplier;
        }

        return (rate, rounding);
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity points rate operations.");
        }

        return tenantId.Value;
    }
}
