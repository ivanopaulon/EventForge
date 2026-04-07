using Microsoft.EntityFrameworkCore;
using NCrontab;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service to clean up old log entries.
/// </summary>
public class LogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    public LogCleanupService(IServiceProvider serviceProvider, ILogger<LogCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _schedule = CrontabSchedule.Parse("0 2 * * *");
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogCleanupService started. Next run scheduled at {NextRun} UTC", _nextRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var delay = _nextRun - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            await CleanupLogsAsync(stoppingToken);

            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next log cleanup scheduled at {NextRun} UTC", _nextRun);
        }
    }

    private async Task CleanupLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting log cleanup process...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

            var retentionDaysConfig = await dbContext.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == "Logging.RetentionDays", cancellationToken);

            var retentionDays = 30;
            if (retentionDaysConfig != null && int.TryParse(retentionDaysConfig.Value, out var configuredDays))
            {
                retentionDays = configuredDays;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var deletedLoginAudits = await dbContext.LoginAudits
                .Where(l => l.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedAuditTrails = await dbContext.AuditTrails
                .Where(l => l.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedOperationLogs = await dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt < cutoffDate && l.Severity != "Critical")
                .ExecuteDeleteAsync(cancellationToken);

            var deletedPerformanceLogs = await dbContext.PerformanceLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Log cleanup completed: {LoginAudits} login audits, {AuditTrails} audit trails, {OperationLogs} operation logs, {PerformanceLogs} performance logs deleted (older than {Days} days)",
                deletedLoginAudits, deletedAuditTrails, deletedOperationLogs, deletedPerformanceLogs, retentionDays);

            var operationLog = new SystemOperationLog
            {
                OperationType = "Maintenance",
                Operation = "LogCleanup",
                Category = "Maintenance",
                Severity = "Information",
                Status = "Success",
                Action = "Delete",
                Details = $"Deleted {deletedLoginAudits + deletedAuditTrails + deletedOperationLogs + deletedPerformanceLogs} log entries older than {retentionDays} days",
                CreatedAt = DateTime.UtcNow,
                ExecutedAt = DateTime.UtcNow,
                ExecutedBy = "System"
            };

            dbContext.SystemOperationLogs.Add(operationLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log cleanup");
        }
    }
}
