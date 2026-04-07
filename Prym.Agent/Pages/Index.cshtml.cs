using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.Agent.Pages;

/// <summary>
/// Dashboard page model for the Agent local web UI.
/// Displays Hub connection state, installed component versions,
/// the pending update queue, and maintenance window information.
/// </summary>
public class IndexModel(
    AgentStatusService agentStatus,
    AgentOptions agentOptions,
    PendingInstallService pendingInstallService,
    UpdateExecutorService updateExecutor,
    VersionDetectorService versionDetector,
    CommandTrackingService commandTracking,
    ILogger<IndexModel> logger) : PageModel
{
    public string InstallationId { get; private set; } = string.Empty;
    public string InstallationName { get; private set; } = string.Empty;
    public bool ServerEnabled { get; private set; }
    public bool ClientEnabled { get; private set; }
    public string HubState { get; private set; } = "Disconnected";
    public DateTime? LastHeartbeat { get; private set; }
    public string? ServerVersion { get; private set; }
    public string? ClientVersion { get; private set; }
    public bool QueueBlocked { get; private set; }
    public string? BlockedReason { get; private set; }
    public Guid? BlockedByPackageId { get; private set; }
    public int MaxAutoRetries { get; private set; }
    public int PendingCount { get; private set; }
    public List<PendingUpdate> PendingUpdates { get; private set; } = [];
    public DateTime? NextWindow { get; private set; }
    public IReadOnlyList<TrackedCommand> TrackedCommands { get; private set; } = [];

    public void OnGet()
    {
        InstallationId = agentOptions.InstallationId;
        InstallationName = agentOptions.InstallationName;
        ServerEnabled = agentOptions.Components.Server.Enabled;
        ClientEnabled = agentOptions.Components.Client.Enabled;
        HubState = agentStatus.HubConnectionState;
        LastHeartbeat = agentStatus.LastHeartbeatAt;
        ServerVersion = versionDetector.GetServerVersion();
        ClientVersion = versionDetector.GetClientVersion();
        QueueBlocked = pendingInstallService.IsBlocked;
        BlockedReason = pendingInstallService.BlockedReason;
        BlockedByPackageId = pendingInstallService.BlockedByPackageId;
        MaxAutoRetries = agentOptions.Install.MaxAutoRetries;
        PendingUpdates = [.. pendingInstallService.GetAll()];
        PendingCount = PendingUpdates.Count;
        NextWindow = pendingInstallService.GetNextWindowStart();
        TrackedCommands = commandTracking.GetAll();
    }

    public IActionResult OnPostInstallNow(Guid packageId)
    {
        var pending = pendingInstallService.GetByPackageId(packageId);
        if (pending is null)
        {
            TempData["Error"] = $"Package {packageId} not found in queue.";
            return RedirectToPage();
        }

        var head = pendingInstallService.GetNext();
        if (head is null || head.PackageId != packageId)
        {
            TempData["Error"] = "Only the first item in the queue can be installed out-of-schedule. Sequential order must be preserved.";
            return RedirectToPage();
        }

        // Fire-and-forget on a background thread; the page returns immediately.
        _ = Task.Run(async () =>
        {
            try
            {
                await updateExecutor.InstallFromZipAsync(pending.Command, pending.LocalZipPath, CancellationToken.None);
                pendingInstallService.Remove(packageId);
            }
            catch (Exception ex)
            {
                pendingInstallService.Block(packageId,
                    $"InstallNow (UI) failed for {pending.Command.Component} {pending.Command.Version}: {ex.Message}");
                logger.LogError(ex, "InstallNow from UI failed for {PackageId}", packageId);
            }
        });

        TempData["Info"] = $"Installation of {pending.Command.Component} {pending.Command.Version} started in background.";
        return RedirectToPage();
    }

    public IActionResult OnPostUnblockQueue()
    {
        pendingInstallService.Unblock(skipAndRemove: false);
        TempData["Info"] = "Queue unblocked. The failed update will be retried on the next maintenance window.";
        return RedirectToPage();
    }

    public IActionResult OnPostUnblockSkip()
    {
        pendingInstallService.Unblock(skipAndRemove: true);
        TempData["Warning"] = "Queue unblocked and the failed update was SKIPPED. Dependent migrations will not run.";
        return RedirectToPage();
    }
}
