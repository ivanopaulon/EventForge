using EventForge.Server.Data;
using EventForge.Server.Services.Business;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Periodic (demotion-only) fidelity tier reevaluation. Once per day it scans every tenant's
/// fidelity cards and demotes any card whose current tier's evaluation window has elapsed and whose
/// recent spend no longer satisfies the tier's rule. Immediate promotions are handled inline by
/// <see cref="IFidelityTierEvaluationService"/>; this job never promotes.
/// </summary>
public class FidelityTierReevaluationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FidelityTierReevaluationBackgroundService> _logger;

    public FidelityTierReevaluationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FidelityTierReevaluationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Fidelity Tier Reevaluation Background Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReevaluateAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during fidelity tier reevaluation cycle");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Fidelity Tier Reevaluation Background Service stopped");
    }

    private async Task ReevaluateAllTenantsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var now = DateTime.UtcNow;

        // Background job: no ITenantContext scoping — query across all tenants explicitly.
        var tenantIds = await context.FidelityCards
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.TierId != null)
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(stoppingToken);

        foreach (var tenantId in tenantIds)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ReevaluateTenantAsync(context, tenantId, now, stoppingToken);
        }
    }

    private async Task ReevaluateTenantAsync(
        EventForgeDbContext context,
        Guid tenantId,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var tiers = await FidelityTierEvaluation.LoadTiersWithRulesAsync(context, tenantId, stoppingToken);
        if (tiers.Count == 0)
        {
            return;
        }

        var cards = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .Where(c => c.TierId != null)
            .ToListAsync(stoppingToken);

        var demoted = 0;

        foreach (var card in cards)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var current = tiers.FirstOrDefault(t => t.Tier.Id == card.TierId!.Value);
            if (current is null || current.Tier.SortOrder == 0)
            {
                continue; // Base tier (or unknown tier): nothing to demote.
            }

            // Only reevaluate once the current tier's evaluation window has elapsed.
            var months = Math.Max(1, current.Rule?.EvaluationPeriodMonths ?? 12);
            var enteredAt = card.TierEnteredAt ?? card.CreatedAt;
            if (enteredAt.AddMonths(months) > now)
            {
                continue;
            }

            var best = await FidelityTierEvaluation.FindBestQualifyingTierAsync(
                context, tenantId, card.Id, tiers, now, stoppingToken);

            var targetSortOrder = best?.SortOrder ?? 0;

            // Demotion only: never raise the tier here.
            if (best is not null && targetSortOrder < current.Tier.SortOrder)
            {
                card.TierId = best.Value.TierId;
                card.TierEnteredAt = now;
                card.ModifiedAt = now;
                demoted++;
            }
        }

        if (demoted > 0)
        {
            _ = await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation(
                "Fidelity tier reevaluation demoted {Count} card(s) for tenant {TenantId}.",
                demoted, tenantId);
        }
    }
}
