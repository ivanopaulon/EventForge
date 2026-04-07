using Prym.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Prym.Server.Services.Dashboard;

/// <summary>
/// Implementation of performance metrics service.
/// </summary>
public class PerformanceMetricsService(
    PrymDbContext dbContext,
    IConfiguration configuration,
    ILogger<PerformanceMetricsService> logger) : IPerformanceMetricsService
{

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
            logger.LogWarning(ex, "Failed to get memory usage");
        }

        try
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentLogs = await dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneMinuteAgo)
                .CountAsync(cancellationToken);

            metrics.RequestsPerMinute = recentLogs;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to calculate requests per minute");
        }

        try
        {
            var slowQueryThresholdMs = configuration.GetValue<double>("Performance:SlowQueryThresholdMs", 1000);
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var slowQueries = await dbContext.SystemOperationLogs
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
                QueryPreview = q.Operation!.Length > 100 ? q.Operation.Substring(0, Math.Min(97, q.Operation.Length)) + "..." : q.Operation,
                AvgDurationMs = q.AvgDuration,
                ExecutionCount = q.Count,
                LastSeen = q.LastSeen,
                Context = "Database"
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get slow queries");
        }

        return metrics;
    }

}
