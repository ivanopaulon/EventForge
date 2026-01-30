using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service to collect performance metrics.
/// </summary>
public class PerformanceCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PerformanceCollectorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public PerformanceCollectorService(IServiceProvider serviceProvider, ILogger<PerformanceCollectorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PerformanceCollectorService started. Collecting metrics every {Interval}", _interval);

        await CollectMetricsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await CollectMetricsAsync(stoppingToken);
        }
    }

    private async Task CollectMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;

            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

            var requestsPerMinute = await dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneMinuteAgo)
                .CountAsync(cancellationToken);

            var avgResponseTimeMs = await dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneMinuteAgo && l.DurationMs.HasValue)
                .AverageAsync(l => (double?)l.DurationMs, cancellationToken) ?? 0;

            var performanceLog = new PerformanceLog
            {
                Timestamp = DateTime.UtcNow,
                RequestsPerMinute = requestsPerMinute,
                AvgResponseTimeMs = avgResponseTimeMs,
                MemoryUsageMB = memoryUsageMB,
                CpuUsagePercent = 0
            };

            dbContext.PerformanceLogs.Add(performanceLog);

            var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
            var deletedOldLogs = await dbContext.PerformanceLogs
                .Where(l => l.Timestamp < twentyFourHoursAgo)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Performance metrics collected: {Requests} req/min, {Memory} MB, {AvgResponseTime} ms avg response (deleted {DeletedLogs} old logs)",
                requestsPerMinute, memoryUsageMB, avgResponseTimeMs, deletedOldLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }
}
