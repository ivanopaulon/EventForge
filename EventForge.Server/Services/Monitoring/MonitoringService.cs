using EventForge.DTOs.Monitoring;
using EventForge.Server.Services.Performance;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Monitoring;

/// <inheritdoc />
public class MonitoringService : IMonitoringService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IMonitoringMetricsService _metricsService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IMonitoringMetricsService metricsService,
        IPerformanceMonitoringService performanceMonitoringService,
        ILogger<MonitoringService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _performanceMonitoringService = performanceMonitoringService ?? throw new ArgumentNullException(nameof(performanceMonitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MonitoringDashboardDto> GetDashboardAsync(
        int topN = 10,
        int recentErrorCount = 20,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for monitoring operations.");

        try
        {
            var metricsSnapshot = _metricsService.GetSnapshot();
            var perfStats = await _performanceMonitoringService.GetStatisticsAsync();

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
            var topPromotions = await _context.Promotions
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
            var nearLimitPromotions = await _context.Promotions
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
            var recentErrors = await _context.SystemOperationLogs
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
            _logger.LogError(ex, "Error assembling monitoring dashboard for tenant {TenantId}.", tenantId);
            throw;
        }
    }
}
