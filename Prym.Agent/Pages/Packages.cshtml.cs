using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.Agent.Pages;

/// <summary>
/// Packages page: shows all packages grouped by status
/// (available from Hub, downloading, ready to install, installed).
/// </summary>
public class PackagesModel(
    CommandTrackingService commandTracking,
    PendingInstallService pendingInstallService,
    VersionDetectorService versionDetector) : PageModel
{
    /// <summary>Packages announced via UpdateAvailable (available from Hub, may be newer or already current).</summary>
    public IReadOnlyList<NotifiedPackage> NotifiedPackages { get; private set; } = [];

    /// <summary>Packages currently being downloaded or where download failed.</summary>
    public IReadOnlyList<TrackedCommand> DownloadingPackages { get; private set; } = [];

    /// <summary>Packages downloaded and queued for installation.</summary>
    public IReadOnlyList<PendingUpdate> ReadyPackages { get; private set; } = [];

    /// <summary>Successfully installed packages.</summary>
    public IReadOnlyList<TrackedCommand> InstalledPackages { get; private set; } = [];

    /// <summary>Currently installed Server version.</summary>
    public string? ServerVersion { get; private set; }

    /// <summary>Currently installed Client version.</summary>
    public string? ClientVersion { get; private set; }

    public async Task OnGetAsync()
    {
        var allTracked = commandTracking.GetAll();
        DownloadingPackages = allTracked
            .Where(c => c.State == CommandState.Downloading ||
                        c.State == CommandState.Received ||
                        c.State == CommandState.DownloadFailed)
            .ToList();
        ReadyPackages = [.. pendingInstallService.GetAll()];
        InstalledPackages = allTracked
            .Where(c => c.State == CommandState.Installed ||
                        c.State == CommandState.Installing ||
                        c.State == CommandState.Failed)
            .ToList();

        // Exclude notified packages that have already progressed to a tracked lifecycle state
        // (downloading, ready to install, or installed) so they don't appear as "available"
        // after the agent has already acted on them.
        var actionedIds = new HashSet<Guid>(
            DownloadingPackages.Select(c => c.PackageId)
                .Concat(ReadyPackages.Select(p => p.PackageId))
                .Concat(InstalledPackages.Select(c => c.PackageId)));
        NotifiedPackages = commandTracking.GetNotified()
            .Where(n => !actionedIds.Contains(n.PackageId))
            .ToList();

        ServerVersion = await versionDetector.GetServerVersionAsync();
        ClientVersion = await versionDetector.GetClientVersionAsync();
    }
}
