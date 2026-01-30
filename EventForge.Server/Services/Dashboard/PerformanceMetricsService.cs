using EventForge.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Implementation of performance metrics service.
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PerformanceMetricsService> _logger;

    public PerformanceMetricsService(
        EventForgeDbContext dbContext,
        IConfiguration configuration,
        ILogger<PerformanceMetricsService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new PerformanceMetrics
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var process = Process.GetCurrentProcess();
            metrics.MemoryUsageMB = process.WorkingSet64 / 1024 / 1024;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory usage");
        }

        try
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentLogs = await _dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneMinuteAgo)
                .CountAsync(cancellationToken);

            metrics.RequestsPerMinute = recentLogs;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to calculate requests per minute");
        }

        try
        {
            var slowQueryThresholdMs = _configuration.GetValue<double>("Performance:SlowQueryThresholdMs", 1000);
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var slowQueries = await _dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneHourAgo &&
                           l.DurationMs.HasValue &&
                           l.DurationMs.Value > slowQueryThresholdMs &&
                           l.Category == "Database")
                .GroupBy(l => l.Operation)
                .Select(g => new
                {
                    Operation = g.Key,
                    AvgDuration = g.Average(l => l.DurationMs!.Value),
                    Count = g.Count(),
                    LastSeen = g.Max(l => l.CreatedAt)
                })
                .OrderByDescending(q => q.AvgDuration)
                .Take(10)
                .ToListAsync(cancellationToken);

            metrics.SlowQueries = slowQueries.Select(q => new SlowQueryDto
            {
                QueryPreview = q.Operation.Length > 100 ? q.Operation.Substring(0, Math.Min(97, q.Operation.Length)) + "..." : q.Operation,
                AvgDurationMs = q.AvgDuration,
                ExecutionCount = q.Count,
                LastSeen = q.LastSeen,
                Context = "Database"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get slow queries");
        }

        return metrics;
    }
}
