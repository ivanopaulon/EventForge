using Dapper;
using EventForge.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using System.Text.Json;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service that performs scheduled cleanup of old log entries.
/// Before any deletion it optionally creates a JSON backup of the rows being removed.
/// Configuration is read from SystemConfigurations on every run so changes take effect
/// without restarting the application.
///
/// Config keys (all in SystemConfigurations table):
///   Logging.RetentionDays        — rows older than N days are eligible for deletion (default 30)
///   Logging.CleanupCron          — cron expression for schedule (default "0 2 * * *" = 02:00 UTC)
///   Logging.BackupEnabled        — "true"/"false" — whether to write a JSON backup before delete (default true)
///   Logging.BackupAbortOnFailure — "true"/"false" — abort cleanup when backup fails (default true)
///   Logging.BackupDirectory      — absolute or relative path for backup files; empty = {ContentRoot}/Backups
///
/// Backup behaviour:
///   When BackupEnabled=true, only the rows that would be deleted are exported to per-table JSON files.
///   The EF-managed tables (LoginAudits, AuditTrails, SystemOperationLogs, PerformanceLogs) are read from
///   the main EventForge database. The Serilog Logs table is read from the LogDb connection string.
///   If BackupAbortOnFailure=true and the backup fails for any reason, no rows are deleted.
///
/// SignalR events (sent to "superadmin" group via AppHub):
///   LogCleanupStarted       — fired right before the run begins, carries RetentionDays and NextRun UTC
///   LogCleanupPhaseChanged  — "Backup" and "Deleting" phase notifications
///   LogCleanupCompleted     — fired after deletion (or abort), carries per-table counts, elapsed and BackupFile
/// </summary>
public class LogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<AppHub> _hubContext;

    // Defaults used when no SystemConfiguration row exists
    private const string DefaultCron             = "0 2 * * *";
    private const int    DefaultRetention        = 30;
    private const bool   DefaultBackup           = true;
    private const bool   DefaultAbortOnFailure   = true;

    public LogCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LogCleanupService> logger,
        IConfiguration configuration,
        IHubContext<AppHub> hubContext)
    {
        _serviceProvider  = serviceProvider;
        _logger           = logger;
        _configuration    = configuration;
        _hubContext        = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Re-read the cron expression on every iteration so config changes are
            // picked up without a restart.
            var cronExpr  = await ReadConfigAsync("Logging.CleanupCron", DefaultCron, stoppingToken);
            var schedule  = CrontabSchedule.Parse(cronExpr);
            var nextRun   = schedule.GetNextOccurrence(DateTime.UtcNow);

            _logger.LogInformation("LogCleanupService: next cleanup scheduled at {NextRun} UTC (cron: {Cron})",
                nextRun, cronExpr);

            var delay = nextRun - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await CleanupLogsAsync(stoppingToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Core cleanup logic
    // ─────────────────────────────────────────────────────────────────────────

    private async Task CleanupLogsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LogCleanupService: starting log cleanup...");
        var startedAt = DateTime.UtcNow;

        try
        {
            var retentionDaysStr   = await ReadConfigAsync("Logging.RetentionDays",        DefaultRetention.ToString(),      cancellationToken);
            var retentionDays      = int.TryParse(retentionDaysStr, out var d) ? d : DefaultRetention;
            var backupEnabledStr   = await ReadConfigAsync("Logging.BackupEnabled",         DefaultBackup.ToString(),         cancellationToken);
            var backupEnabled      = !string.Equals(backupEnabledStr, "false", StringComparison.OrdinalIgnoreCase);
            var abortOnFailureStr  = await ReadConfigAsync("Logging.BackupAbortOnFailure",  DefaultAbortOnFailure.ToString(), cancellationToken);
            var abortOnFailure     = !string.Equals(abortOnFailureStr, "false", StringComparison.OrdinalIgnoreCase);
            var backupDirectory    = await ReadConfigAsync("Logging.BackupDirectory",       string.Empty,                     cancellationToken);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation(
                "LogCleanupService: retention={RetentionDays}d, cutoff={Cutoff:u}, backup={BackupEnabled}, abortOnFailure={AbortOnFailure}",
                retentionDays, cutoffDate, backupEnabled, abortOnFailure);

            // ── Notify SuperAdmin clients that cleanup is about to start ─────
            await BroadcastToSuperAdminAsync(AppHub.LogCleanupStarted, new
            {
                RetentionDays    = retentionDays,
                CutoffDate       = cutoffDate,
                BackupEnabled    = backupEnabled,
                AbortOnFailure   = abortOnFailure,
                StartedAt        = startedAt
            }, cancellationToken);

            // ── Optional backup before deletion ─────────────────────────────
            string? backupFilePath = null;
            if (backupEnabled)
            {
                await BroadcastToSuperAdminAsync(AppHub.LogCleanupPhaseChanged, new
                {
                    Phase           = "Backup",
                    Detail          = "Creazione backup in corso…",
                    BackupSucceeded = (bool?)null,
                    ChangedAt       = DateTime.UtcNow
                }, cancellationToken);

                backupFilePath = await CreateLogBackupAsync(cutoffDate, backupDirectory, cancellationToken);

                // ── Abort if backup failed and the policy requires it ────────
                if (backupFilePath is null && abortOnFailure)
                {
                    const string abortReason = "Backup fallito — pulizia annullata per sicurezza (Logging.BackupAbortOnFailure=true).";
                    _logger.LogError("LogCleanupService: {Reason}", abortReason);

                    await BroadcastToSuperAdminAsync(AppHub.LogCleanupCompleted, new
                    {
                        Success      = false,
                        TotalDeleted = 0,
                        Error        = abortReason,
                        CompletedAt  = DateTime.UtcNow
                    }, cancellationToken);

                    // Record the aborted run in operation log
                    try
                    {
                        using var scope   = _serviceProvider.CreateScope();
                        var dbContext     = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();
                        dbContext.SystemOperationLogs.Add(new SystemOperationLog
                        {
                            OperationType = "Maintenance",
                            Operation     = "LogCleanup",
                            Category      = "Maintenance",
                            Severity      = "Error",
                            Status        = "Aborted",
                            Action        = "Delete",
                            Details       = abortReason,
                            CreatedAt     = DateTime.UtcNow,
                            ExecutedAt    = DateTime.UtcNow,
                            ExecutedBy    = "System"
                        });
                        await dbContext.SaveChangesAsync(CancellationToken.None);
                    }
                    catch (Exception inner)
                    {
                        _logger.LogError(inner, "LogCleanupService: failed to record aborted cleanup in operation log.");
                    }

                    return;
                }
            }

            // ── Notify phase transition to deletion ──────────────────────────
            // Always broadcast before deletes so the snackbar reflects the correct
            // phase regardless of whether backup was enabled.
            await BroadcastToSuperAdminAsync(AppHub.LogCleanupPhaseChanged, new
            {
                Phase           = "Deleting",
                Detail          = "Eliminazione log in corso…",
                BackupSucceeded = backupEnabled ? (bool?)(backupFilePath is not null) : null,
                ChangedAt       = DateTime.UtcNow
            }, cancellationToken);

            // ── EF-managed tables ────────────────────────────────────────────
            int deletedLoginAudits, deletedAuditTrails, deletedOperationLogs, deletedPerformanceLogs;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

                deletedLoginAudits = await dbContext.LoginAudits
                    .Where(l => l.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(cancellationToken);

                deletedAuditTrails = await dbContext.AuditTrails
                    .Where(l => l.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(cancellationToken);

                // Keep Critical operation logs regardless of age
                deletedOperationLogs = await dbContext.SystemOperationLogs
                    .Where(l => l.CreatedAt < cutoffDate && l.Severity != "Critical")
                    .ExecuteDeleteAsync(cancellationToken);

                deletedPerformanceLogs = await dbContext.PerformanceLogs
                    .Where(l => l.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // ── Serilog Logs table (managed by Serilog, not EF) ─────────────
            var deletedSerilogLogs = await DeleteSerilogLogsAsync(cutoffDate, cancellationToken);

            var totalDeleted = deletedLoginAudits + deletedAuditTrails
                + deletedOperationLogs + deletedPerformanceLogs + deletedSerilogLogs;

            var elapsedSeconds = (DateTime.UtcNow - startedAt).TotalSeconds;

            _logger.LogInformation(
                "LogCleanupService: cleanup completed in {Elapsed:0.0}s — {Total} rows deleted " +
                "(LoginAudits={LA}, AuditTrails={AT}, OperationLogs={OL}, PerformanceLogs={PL}, SerilogLogs={SL}) " +
                "older than {RetentionDays} days. BackupFile={BackupFile}",
                elapsedSeconds,
                totalDeleted,
                deletedLoginAudits, deletedAuditTrails, deletedOperationLogs, deletedPerformanceLogs, deletedSerilogLogs,
                retentionDays,
                backupFilePath ?? "none");

            // ── Notify SuperAdmin clients that cleanup completed ──────────────
            await BroadcastToSuperAdminAsync(AppHub.LogCleanupCompleted, new
            {
                Success           = true,
                TotalDeleted      = totalDeleted,
                LoginAudits       = deletedLoginAudits,
                AuditTrails       = deletedAuditTrails,
                OperationLogs     = deletedOperationLogs,
                PerformanceLogs   = deletedPerformanceLogs,
                SerilogLogs       = deletedSerilogLogs,
                RetentionDays     = retentionDays,
                BackupFile        = backupFilePath ?? "none",
                ElapsedSeconds    = elapsedSeconds,
                CompletedAt       = DateTime.UtcNow
            }, cancellationToken);

            // ── Audit record for the cleanup itself ──────────────────────────
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

                dbContext.SystemOperationLogs.Add(new SystemOperationLog
                {
                    OperationType = "Maintenance",
                    Operation     = "LogCleanup",
                    Category      = "Maintenance",
                    Severity      = "Information",
                    Status        = "Success",
                    Action        = "Delete",
                    Details       = $"Deleted {totalDeleted} log entries older than {retentionDays} days. " +
                                    $"LoginAudits={deletedLoginAudits}, AuditTrails={deletedAuditTrails}, " +
                                    $"OperationLogs={deletedOperationLogs}, PerformanceLogs={deletedPerformanceLogs}, " +
                                    $"SerilogLogs={deletedSerilogLogs}. BackupFile={backupFilePath ?? "none"}",
                    CreatedAt     = DateTime.UtcNow,
                    ExecutedAt    = DateTime.UtcNow,
                    ExecutedBy    = "System"
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LogCleanupService: cleanup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogCleanupService: error during log cleanup.");

            // Notify SuperAdmin clients that cleanup failed
            try
            {
                await BroadcastToSuperAdminAsync(AppHub.LogCleanupCompleted, new
                {
                    Success      = false,
                    TotalDeleted = 0,
                    Error        = ex.Message,
                    CompletedAt  = DateTime.UtcNow
                }, CancellationToken.None);
            }
            catch { /* best-effort — do not mask original exception */ }

            // Record failed cleanup in operation log
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

                dbContext.SystemOperationLogs.Add(new SystemOperationLog
                {
                    OperationType = "Maintenance",
                    Operation     = "LogCleanup",
                    Category      = "Maintenance",
                    Severity      = "Error",
                    Status        = "Failed",
                    Action        = "Delete",
                    Details       = $"Cleanup failed: {ex.Message}",
                    CreatedAt     = DateTime.UtcNow,
                    ExecutedAt    = DateTime.UtcNow,
                    ExecutedBy    = "System"
                });

                await dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception inner)
            {
                _logger.LogError(inner, "LogCleanupService: failed to record error in operation log.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Serilog Logs table cleanup (managed outside EF)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> DeleteSerilogLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("LogDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("LogCleanupService: LogDb connection string not found — skipping Serilog Logs table cleanup.");
            return 0;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var deleted = await connection.ExecuteAsync(
                "DELETE FROM [Logs] WHERE [TimeStamp] < @Cutoff",
                new { Cutoff = cutoffDate });

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogCleanupService: failed to clean up Serilog Logs table.");
            return 0;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pre-deletion JSON backup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports all log rows that are about to be deleted into per-table JSON files stored under
    /// <paramref name="configuredDirectory"/> (or {ContentRoot}/Backups when that is empty).
    /// Only the tables involved in cleanup are exported: LoginAudits, AuditTrails,
    /// SystemOperationLogs and PerformanceLogs (via EF / main DB) and the Serilog Logs table
    /// (via the LogDb connection string, which may be a separate database).
    /// Returns the path of the created backup directory, or null on failure.
    /// When null is returned the caller must decide whether to abort or continue based on policy.
    /// </summary>
    private async Task<string?> CreateLogBackupAsync(DateTime cutoffDate, string configuredDirectory, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext   = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();
            var env         = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var timestamp  = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var baseDir    = !string.IsNullOrWhiteSpace(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(env.ContentRootPath, "Backups");
            var backupDir  = Path.Combine(baseDir, $"LogBackup_{timestamp}");
            Directory.CreateDirectory(backupDir);

            var options = new JsonSerializerOptions { WriteIndented = false };

            // LoginAudits (main EventForge DB via EF)
            var loginAudits = await dbContext.LoginAudits
                .AsNoTracking()
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            await WriteJsonFileAsync(Path.Combine(backupDir, "LoginAudits.json"), loginAudits, options, cancellationToken);

            // AuditTrails (main EventForge DB via EF)
            var auditTrails = await dbContext.AuditTrails
                .AsNoTracking()
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            await WriteJsonFileAsync(Path.Combine(backupDir, "AuditTrails.json"), auditTrails, options, cancellationToken);

            // SystemOperationLogs (main EventForge DB via EF, excluding Critical rows)
            var operationLogs = await dbContext.SystemOperationLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt < cutoffDate && l.Severity != "Critical")
                .ToListAsync(cancellationToken);

            await WriteJsonFileAsync(Path.Combine(backupDir, "SystemOperationLogs.json"), operationLogs, options, cancellationToken);

            // PerformanceLogs (main EventForge DB via EF)
            var performanceLogs = await dbContext.PerformanceLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            await WriteJsonFileAsync(Path.Combine(backupDir, "PerformanceLogs.json"), performanceLogs, options, cancellationToken);

            // Serilog Logs table (separate LogDb — may be a different database/server)
            await BackupSerilogLogsAsync(cutoffDate, backupDir, options, cancellationToken);

            _logger.LogInformation("LogCleanupService: backup created at {BackupDir}", backupDir);
            return backupDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogCleanupService: failed to create log backup.");
            return null;
        }
    }

    private async Task BackupSerilogLogsAsync(
        DateTime cutoffDate,
        string backupDir,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("LogDb");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var rows = (await connection.QueryAsync(
                "SELECT * FROM [Logs] WHERE [TimeStamp] < @Cutoff",
                new { Cutoff = cutoffDate })).AsList();

            await WriteJsonFileAsync(Path.Combine(backupDir, "SerilogLogs.json"), rows, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LogCleanupService: failed to backup Serilog Logs table.");
        }
    }

    private static async Task WriteJsonFileAsync<T>(
        string filePath,
        T data,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenWrite(filePath);
        await JsonSerializer.SerializeAsync(stream, data, options, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Config helper
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> ReadConfigAsync(string key, string defaultValue, CancellationToken cancellationToken)
    {
        try
        {
            using var scope   = _serviceProvider.CreateScope();
            var dbContext     = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

            var row = await dbContext.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);

            return row?.Value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SignalR broadcast helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Broadcasts a named event to all SuperAdmin clients (the "superadmin" group in AppHub).
    /// Errors are swallowed — SignalR notifications are best-effort.
    /// </summary>
    private async Task BroadcastToSuperAdminAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.Group("superadmin")
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LogCleanupService: failed to broadcast {EventName} via SignalR.", eventName);
        }
    }
}
