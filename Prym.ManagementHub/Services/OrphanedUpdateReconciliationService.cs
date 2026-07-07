namespace Prym.ManagementHub.Services;

/// <summary>
/// Background service that periodically reconciles <see cref="UpdateHistory"/> records that are
/// stuck in <see cref="UpdateHistoryStatus.InProgress"/> because the Agent crashed, was shut down,
/// or lost connectivity before it could report completion.
///
/// <para>
/// An update is considered orphaned when all of the following are true:
/// <list type="bullet">
///   <item>Status is <c>InProgress</c> and phase is <b>not</b> <c>AwaitingMaintenanceWindow</c>
///         (that phase is a legitimate long-running wait).</item>
///   <item>The associated installation is currently <c>Offline</c>.</item>
///   <item><see cref="UpdateHistory.StartedAt"/> is older than the configured grace period
///         (default 15 minutes — long enough to survive normal SignalR reconnections).</item>
/// </list>
/// </para>
///
/// <para>
/// For each orphaned record the service:
/// <list type="number">
///   <item>Marks the history <c>Failed</c> with an explanatory message.</item>
///   <item>Releases the throttle slot via <see cref="IUpdateThrottleService.Release"/>.</item>
///   <item>Resets the associated package from <c>Deploying</c> back to <c>ReadyToDeploy</c>
///         so it is no longer blocked.</item>
/// </list>
/// Each record is reconciled in isolation — a failure for one record does not prevent processing
/// of the others; problematic records are retried on the next scheduled run.
/// </para>
/// </summary>
public class OrphanedUpdateReconciliationService(
    IServiceScopeFactory scopeFactory,
    IUpdateThrottleService updateThrottle,
    ManagementHubOptions hubOptions,
    ILogger<OrphanedUpdateReconciliationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)    {
        var interval = TimeSpan.FromSeconds(
            hubOptions.OrphanedUpdateCheckIntervalSeconds > 0
                ? hubOptions.OrphanedUpdateCheckIntervalSeconds
                : 300);

        logger.LogInformation(
            "OrphanedUpdateReconciliationService started. Grace={GraceMinutes}m Interval={IntervalSeconds}s",
            hubOptions.OrphanedUpdateGraceMinutes, interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (stoppingToken.IsCancellationRequested) break;

                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OrphanedUpdateReconciliationService encountered an unexpected error.");
            }
        }

        logger.LogInformation("OrphanedUpdateReconciliationService stopped.");
    }

    internal async Task ReconcileAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-hubOptions.OrphanedUpdateGraceMinutes);

        using var scope = scopeFactory.CreateScope();
        var installationService = scope.ServiceProvider.GetRequiredService<IInstallationService>();
        var packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();

        IReadOnlyList<Guid> orphanedIds;
        try
        {
            orphanedIds = await installationService.GetOrphanedInProgressHistoryAsync(cutoff, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OrphanedUpdateReconciliationService: failed to query orphaned history records.");
            return;
        }

        if (orphanedIds.Count == 0) return;

        logger.LogWarning(
            "OrphanedUpdateReconciliationService: found {Count} orphaned InProgress update(s) older than {GraceMinutes} minutes with Offline installations.",
            orphanedIds.Count, hubOptions.OrphanedUpdateGraceMinutes);

        foreach (var historyId in orphanedIds)
        {
            try
            {
                var packageId = await installationService.CompleteUpdateHistoryAsync(
                    historyId,
                    UpdateHistoryStatus.Failed,
                    $"Reconciled by OrphanedUpdateReconciliationService: installation went offline before reporting completion (grace period {hubOptions.OrphanedUpdateGraceMinutes} min elapsed).",
                    rolledBack: false,
                    ct);

                updateThrottle.Release();

                logger.LogWarning(
                    "OrphanedUpdateReconciliationService: marked history {HistoryId} as Failed and released throttle slot.",
                    historyId);

                if (packageId.HasValue)
                {
                    var pkg = await packageService.GetByIdAsync(packageId.Value, ct);
                    if (pkg?.Status == PackageStatus.Deploying)
                    {
                        await packageService.SetStatusAsync(packageId.Value, PackageStatus.ReadyToDeploy, ct);
                        logger.LogInformation(
                            "OrphanedUpdateReconciliationService: reset Package {PackageId} from Deploying to ReadyToDeploy.",
                            packageId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "OrphanedUpdateReconciliationService: error reconciling history {HistoryId} — will retry next run.",
                    historyId);
            }
        }
    }
}
