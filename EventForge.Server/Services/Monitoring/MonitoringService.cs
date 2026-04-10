using EventForge.DTOs.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Monitoring;

/// <inheritdoc />
public class MonitoringService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IMonitoringMetricsService metricsService,
    IPerformanceMonitoringService performanceMonitoringService,
    ILogger<MonitoringService> logger) : IMonitoringService
{

    /// <inheritdoc />
    public async Task<MonitoringDashboardDto> GetDashboardAsync(
        int topN = 10,
        int recentErrorCount = 20,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for monitoring operations.");

        try
        {
            var metricsSnapshot = metricsService.GetSnapshot();
            var perfStats = await performanceMonitoringService.GetStatisticsAsync();

            var pricingMetrics = new PricingMetricsDto
            {
                TotalPricingOperations = metricsSnapshot.TotalPricingOperations,
                SuccessfulPricingOperations = metricsSnapshot.SuccessfulPricingOperations,
                FailedPricingOperations = metricsSnapshot.FailedPricingOperations,
                AveragePricingResolutionMs = metricsSnapshot.AveragePricingResolutionMs,
                TotalCacheLookups = metricsSnapshot.TotalCacheLookups,
                CacheHits = metricsSnapshot.CacheHits,
                TotalDbQueries = perfStats.TotalQueries,
                SlowDbQueries = perfStats.SlowQueries,
                AverageDbQueryMs = perfStats.AverageQueryDuration.TotalMilliseconds
            };

            // Top N promotions by CurrentUses
            var topPromotions = await context.Promotions
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.TenantId == tenantId.Value)
                .OrderByDescending(p => p.CurrentUses)
                .Take(topN)
                .Select(p => new PromotionUsageItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CurrentUses = p.CurrentUses,
                    MaxUses = p.MaxUses,
                    IsActive = p.IsActive,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .ToListAsync(ct);

            // Promotions near limit (MaxUses set and CurrentUses > 80% of MaxUses)
            var nearLimitPromotions = await context.Promotions
                .AsNoTracking()
                .Where(p => !p.IsDeleted
                    && p.TenantId == tenantId.Value
                    && p.MaxUses.HasValue
                    && p.MaxUses.Value > 0
                    && (double)p.CurrentUses * 100 / p.MaxUses.Value >= 80)
                .OrderByDescending(p => p.CurrentUses)
                .Take(topN)
                .Select(p => new PromotionUsageItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CurrentUses = p.CurrentUses,
                    MaxUses = p.MaxUses,
                    IsActive = p.IsActive,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .ToListAsync(ct);

            // Recent error/warning system operation logs
            var recentErrors = await context.SystemOperationLogs
                .AsNoTracking()
                .Where(l => l.Severity == "Error" || l.Severity == "Warning" || l.Severity == "Critical")
                .OrderByDescending(l => l.ExecutedAt)
                .Take(recentErrorCount)
                .Select(l => new SystemHealthEntryDto
                {
                    Id = l.Id,
                    Timestamp = l.ExecutedAt,
                    Severity = l.Severity ?? "Unknown",
                    OperationType = l.OperationType,
                    Operation = l.Operation,
                    Details = l.Details
                })
                .ToListAsync(ct);

            return new MonitoringDashboardDto
            {
                SnapshotAt = DateTime.UtcNow,
                PricingMetrics = pricingMetrics,
                TopPromotions = topPromotions,
                NearLimitPromotions = nearLimitPromotions,
                RecentErrors = recentErrors
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
