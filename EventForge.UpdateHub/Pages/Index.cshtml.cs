using EventForge.UpdateHub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.UpdateHub.Pages;

/// <summary>
/// Dashboard page model for the Hub web UI.
/// Displays KPI cards (totals/online/offline), the list of registered installations,
/// and the packages that are ready to deploy.
/// </summary>
public class IndexModel(
    IInstallationService installationService,
    IPackageService packageService,
    IConnectionTracker connectionTracker,
    IHubContext<AgentHub> agentHubContext,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<Installation> Installations { get; private set; } = [];
    public IReadOnlyList<UpdatePackage> ReadyPackages { get; private set; } = [];
    public int TotalInstallations { get; private set; }
    public int OnlineCount { get; private set; }
    public int OfflineCount { get; private set; }
    public int PackageCount { get; private set; }

    public async Task OnGetAsync()
    {
        Installations = await installationService.GetAllAsync();
        ReadyPackages = await packageService.GetByStatusAsync(PackageStatus.ReadyToDeploy);
        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        TotalInstallations = Installations.Count;
        OnlineCount = onlineIds.Count;
        OfflineCount = TotalInstallations - OnlineCount;
        PackageCount = (await packageService.GetAllAsync()).Count;
    }

    /// <summary>Send a specific package update to a single installation.</summary>
    public async Task<IActionResult> OnPostSendUpdateAsync(Guid installationId, Guid packageId)
    {
        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null) return NotFound();

        var connectionId = connectionTracker.GetConnectionId(installationId);
        if (connectionId is null)
        {
            TempData["Error"] = "Installazione non online.";
            return RedirectToPage();
        }

        var installation = await installationService.GetByIdAsync(installationId);
        var history = await installationService.StartUpdateHistoryAsync(
            installationId, packageId,
            installation?.InstalledVersionServer,
            installation?.InstalledVersionClient);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var command = new StartUpdateCommand(
            history.Id, pkg.Id, pkg.Version,
            pkg.Component.ToString(),
            $"{baseUrl}/api/v1/packages/{pkg.Id}/download",
            pkg.Checksum,
            IsManualInstall: installation?.UpdateMode == InstallationUpdateMode.Manual);

        await agentHubContext.Clients.Client(connectionId).SendAsync("StartUpdate", command);
        await packageService.SetStatusAsync(packageId, PackageStatus.Deploying);

        logger.LogInformation("StartUpdate sent: Package={PackageId} Installation={InstallationId}", packageId, installationId);
        TempData["Success"] = $"Aggiornamento {pkg.Component} {pkg.Version} inviato all'installazione.";
        return RedirectToPage();
    }

    /// <summary>Broadcast a package update to ALL online installations.</summary>
    public async Task<IActionResult> OnPostBroadcastUpdateAsync(Guid packageId)
    {
        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null) return NotFound();

        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        if (onlineIds.Count == 0)
        {
            TempData["Error"] = "Nessuna installazione online.";
            return RedirectToPage();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var downloadUrl = $"{baseUrl}/api/v1/packages/{pkg.Id}/download";

        foreach (var id in onlineIds)
        {
            var connectionId = connectionTracker.GetConnectionId(id);
            if (connectionId is null) continue;

            var installation = await installationService.GetByIdAsync(id);
            var history = await installationService.StartUpdateHistoryAsync(
                id, packageId,
                installation?.InstalledVersionServer,
                installation?.InstalledVersionClient);

            var command = new StartUpdateCommand(
                history.Id, pkg.Id, pkg.Version,
                pkg.Component.ToString(),
                downloadUrl,
                pkg.Checksum,
                IsManualInstall: false);

            await agentHubContext.Clients.Client(connectionId).SendAsync("StartUpdate", command);
        }

        await packageService.SetStatusAsync(packageId, PackageStatus.Deploying);

        logger.LogInformation("Broadcast StartUpdate: Package={PackageId} to {Count} installations", packageId, onlineIds.Count);
        TempData["Success"] = $"Aggiornamento {pkg.Component} {pkg.Version} inviato a {onlineIds.Count} installazioni online.";
        return RedirectToPage();
    }

    /// <summary>Archive (disable) a package so it no longer appears as ready.</summary>
    public async Task<IActionResult> OnPostArchivePackageAsync(Guid packageId)
    {
        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null) return NotFound();

        await packageService.SetStatusAsync(packageId, PackageStatus.Archived);
        TempData["Success"] = $"Pacchetto {pkg.Component} {pkg.Version} archiviato.";
        return RedirectToPage();
    }
}
