using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Immediate (upgrade-only) fidelity tier evaluation. Uses the tenant scope of the current request.
/// </summary>
public class FidelityTierEvaluationService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    ILogger<FidelityTierEvaluationService> logger) : IFidelityTierEvaluationService
{
    public async Task<bool> EvaluateUpgradeAsync(Guid fidelityCardId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity tier evaluation.");
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId.Value)
            .FirstOrDefaultAsync(c => c.Id == fidelityCardId, cancellationToken);

        if (card is null)
        {
            return false;
        }

        var tiers = await FidelityTierEvaluation.LoadTiersWithRulesAsync(context, tenantId.Value, cancellationToken);
        if (tiers.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var currentSortOrder = FidelityTierEvaluation.GetSortOrder(tiers, card.TierId);

        var best = await FidelityTierEvaluation.FindBestQualifyingTierAsync(
            context, tenantId.Value, card.Id, tiers, now, cancellationToken);

        // Upgrade only: never lower the tier here.
        if (best is null || best.Value.SortOrder <= currentSortOrder)
        {
            return false;
        }

        card.TierId = best.Value.TierId;
        card.TierEnteredAt = now;
        card.ModifiedAt = now;

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fidelity card {CardId} promoted to tier {TierId} (SortOrder {SortOrder}).",
            card.Id, best.Value.TierId, best.Value.SortOrder);

        return true;
    }
}

/// <summary>
/// Shared, tenant-agnostic helpers for fidelity tier evaluation, reused by the immediate promotion
/// service and the periodic reevaluation background service.
/// </summary>
public static class FidelityTierEvaluation
{
    public sealed record TierWithRule(FidelityTier Tier, FidelityTierRule? Rule);

    public static async Task<List<TierWithRule>> LoadTiersWithRulesAsync(
        EventForgeDbContext context, Guid tenantId, CancellationToken ct)
    {
        var tiers = await context.FidelityTiers
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

        var tierIds = tiers.Select(t => t.Id).ToList();

        var rules = await context.FidelityTierRules
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(r => tierIds.Contains(r.TierId))
            .ToListAsync(ct);

        return tiers
            .Select(t => new TierWithRule(t, rules.FirstOrDefault(r => r.TierId == t.Id)))
            .ToList();
    }

    public static int GetSortOrder(IReadOnlyCollection<TierWithRule> tiers, Guid? tierId)
    {
        if (!tierId.HasValue)
        {
            return int.MinValue;
        }

        var match = tiers.FirstOrDefault(t => t.Tier.Id == tierId.Value);
        return match is null ? int.MinValue : match.Tier.SortOrder;
    }

    /// <summary>
    /// Computes the customer's completed spend for a fidelity card over a trailing window.
    /// Spend is sourced from closed <see cref="SaleSession"/> rows linked to the card via
    /// <see cref="SaleSession.FidelityCardId"/> (the direct sales-amount link in this codebase).
    /// </summary>
    public static async Task<decimal> ComputeSpendAsync(
        EventForgeDbContext context,
        Guid tenantId,
        Guid cardId,
        int evaluationPeriodMonths,
        DateTime now,
        CancellationToken ct)
    {
        var windowStart = now.AddMonths(-Math.Max(1, evaluationPeriodMonths));

        var total = await context.SaleSessions
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(s => s.FidelityCardId == cardId
                        && s.Status == SaleSessionStatus.Closed
                        && (s.ClosedAt ?? s.CreatedAt) >= windowStart)
            .SumAsync(s => (decimal?)s.FinalTotal, ct);

        return total ?? 0m;
    }

    /// <summary>
    /// Finds the highest-SortOrder tier whose spend rule is satisfied. The base tier (lowest
    /// SortOrder, or any tier with no rule/threshold) is always considered a match.
    /// </summary>
    public static async Task<(Guid TierId, int SortOrder)?> FindBestQualifyingTierAsync(
        EventForgeDbContext context,
        Guid tenantId,
        Guid cardId,
        IReadOnlyCollection<TierWithRule> tiers,
        DateTime now,
        CancellationToken ct)
    {
        (Guid TierId, int SortOrder)? best = null;

        foreach (var entry in tiers.OrderBy(t => t.Tier.SortOrder))
        {
            var threshold = entry.Rule?.MinimumSpendThreshold;
            var qualifies = !threshold.HasValue;

            if (threshold.HasValue)
            {
                var months = entry.Rule!.EvaluationPeriodMonths;
                var spend = await ComputeSpendAsync(context, tenantId, cardId, months, now, ct);
                qualifies = spend >= threshold.Value;
            }

            if (qualifies)
            {
                if (best is null || entry.Tier.SortOrder > best.Value.SortOrder)
                {
                    best = (entry.Tier.Id, entry.Tier.SortOrder);
                }
            }
        }

        return best;
    }
}
