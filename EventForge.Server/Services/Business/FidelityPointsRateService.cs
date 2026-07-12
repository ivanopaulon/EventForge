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
        Guid tierId,
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

        var activeCampaign = await context.FidelityPointsCampaigns
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(campaign => campaign.StartDate <= now && campaign.EndDate >= now)
            .OrderByDescending(campaign => campaign.StartDate)
            .ThenByDescending(campaign => campaign.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var rate = baseRate.Rate;
        var rounding = baseRate.RoundingMode;

        if (activeCampaign is not null)
        {
            var campaignTierMultiplier = await context.FidelityTierMultipliers
                .AsNoTracking()
                .Where(multiplier => multiplier.CampaignId == activeCampaign.Id && multiplier.TierId == tierId)
                .Select(multiplier => (decimal?)multiplier.Multiplier)
                .FirstOrDefaultAsync(cancellationToken)
                ?? 1.0m; // No multiplier configured for this tier in this campaign is not an error, defaults to 1.0.

            rate = baseRate.Rate * campaignTierMultiplier * activeCampaign.Multiplier;
            rounding = activeCampaign.RoundingMode;
        }
        // No active campaign: rate stays at baseRate.Rate with no tier differentiation —
        // tier-based differentiation only exists during an active campaign, by design.

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
