namespace EventForge.UpdateAgent.Workers;

/// <summary>
/// Background service that checks once per minute whether there are pending updates
/// and the current time falls inside a configured maintenance window, then installs
/// them one at a time in strict queue order.
///
/// Sequential ordering guarantee:
///   • Only the head of the queue (lowest QueuePosition) is ever installed.
///   • If installation fails the queue is BLOCKED via <see cref="PendingInstallService.Block"/>.
///   • No subsequent update runs until an operator calls Unblock() (from the UI or Hub).
/// </summary>
public class ScheduledInstallWorker(
    PendingInstallService pendingInstallService,
    UpdateExecutorService updateExecutor,
    CommandTrackingService commandTracking,
    AgentOptions options,
    ILogger<ScheduledInstallWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScheduledInstallWorker started (check interval: {Interval}s).",
            options.Install.ScheduledCheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool bypassMaintenanceWindow = false;
            try
            {
                using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                delayCts.CancelAfter(TimeSpan.FromSeconds(options.Install.ScheduledCheckIntervalSeconds));

                // Wait for the scheduled interval OR an operator-triggered immediate install.
                await pendingInstallService.WaitForInstallTriggerAsync(delayCts.Token);
                bypassMaintenanceWindow = true;
                logger.LogInformation("Immediate install triggered — bypassing maintenance window check.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                // Normal scheduled interval elapsed — proceed with window check.
            }

            try
            {
                await TryInstallNextAsync(stoppingToken, bypassMaintenanceWindow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in ScheduledInstallWorker loop.");
            }
        }

        logger.LogInformation("ScheduledInstallWorker stopped.");
    }

    private async Task TryInstallNextAsync(CancellationToken ct, bool bypassMaintenanceWindow = false)
    {
        if (pendingInstallService.IsBlocked)
        {
            logger.LogDebug("Install queue is blocked — skipping. Reason: {Reason}",
                pendingInstallService.BlockedReason);
            return;
        }

        if (!bypassMaintenanceWindow && !pendingInstallService.IsInMaintenanceWindow())
        {
            logger.LogDebug("Outside maintenance window — skipping scheduled check.");
            return;
        }

        var next = pendingInstallService.GetNext();
        if (next is null)
        {
            logger.LogDebug("No pending updates.");
            return;
        }

        if (!File.Exists(next.LocalZipPath))
        {
            logger.LogWarning("Zip file missing for pending update {PackageId} ({Component} {Version}) — removing from queue.",
                next.PackageId, next.Command.Component, next.Command.Version);
            pendingInstallService.Remove(next.PackageId);
            return;
        }

        logger.LogInformation(
            "Maintenance window active — installing queued update {Component} {Version} (PackageId={PackageId}).",
            next.Command.Component, next.Command.Version, next.PackageId);

        try
        {
            commandTracking.SetState(next.PackageId, CommandState.Installing);
            await updateExecutor.InstallFromZipAsync(next.Command, next.LocalZipPath, ct);
            // Success: remove from queue so the next entry becomes the head.
            pendingInstallService.Remove(next.PackageId);
            commandTracking.SetState(next.PackageId, CommandState.Installed);
            logger.LogInformation("Scheduled install succeeded: {Component} {Version}", next.Command.Component, next.Command.Version);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Service is shutting down; leave the entry in the queue for the next run.
            // Reset state to Downloaded: the zip is still on disk and the entry is still queued,
            // so on next startup ScheduledInstallWorker will pick it up and retry.
            commandTracking.SetState(next.PackageId, CommandState.Downloaded);
            logger.LogWarning("Install cancelled (shutdown) for {Component} {Version} — entry remains in queue.",
                next.Command.Component, next.Command.Version);
            throw;
        }
        catch (Exception ex)
        {
            commandTracking.SetState(next.PackageId, CommandState.Failed, ex.Message);
            // Block the queue so no subsequent updates (with potentially dependent migrations) run.
            pendingInstallService.Block(next.PackageId,
                $"Install failed for {next.Command.Component} {next.Command.Version}: {ex.Message}");
            // InstallFromZipAsync already reported the failure to the Hub via OnProgress.
        }
    }
}
