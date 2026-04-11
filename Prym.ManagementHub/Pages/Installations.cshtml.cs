using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.ManagementHub.Pages;

/// <summary>
/// Installations management page model for the Hub web UI.
/// Lists all registered installations with full runtime details,
/// allows registering new ones and sending targeted updates.
/// </summary>
public class InstallationsModel(
    IInstallationService installationService,
    IConnectionTracker connectionTracker,
    IPackageService packageService,
    IHubContext<AgentHub> agentHubContext,
    ILogger<InstallationsModel> logger) : PageModel
{
    public IReadOnlyList<InstallationRow> Installations { get; private set; } = [];
    public IReadOnlyList<UpdatePackage> ReadyPackages { get; private set; } = [];

    public record InstallationRow(Installation Installation, bool IsOnline, IReadOnlyList<UpdateHistorySummary> RecentUpdates);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // ── Registra nuova installazione ──────────────────────────────────────
    public async Task<IActionResult> OnPostRegisterAsync(
        string name, string? location, InstallationComponents components, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Il nome è obbligatorio.";
            return RedirectToPage();
        }

        var apiKey = Convert.ToHexStringLower(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var installation = new Installation
        {
            Name = name,
            Location = string.IsNullOrWhiteSpace(location) ? null : location,
            Components = components,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            ApiKey = apiKey
        };

        var created = await installationService.CreateAsync(installation);
        logger.LogInformation("Installation registered via UI: {Name} ({Id})", created.Name, created.Id);

        TempData["NewKey"] = apiKey;
        TempData["NewKeyInstallationId"] = created.Id.ToString();
        TempData["NewKeyInstallationName"] = created.Name;
        TempData["Success"] = $"Installazione '{created.Name}' registrata. Copia la chiave API subito — non sarà più visibile.";
        return RedirectToPage();
    }

    // ── Invia aggiornamento a singola installazione ───────────────────────
    public async Task<IActionResult> OnPostSendUpdateAsync(Guid installationId, Guid packageId)
    {
        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null) { TempData["Error"] = "Pacchetto non trovato."; return RedirectToPage(); }

        var connectionId = connectionTracker.GetConnectionId(installationId);
        if (connectionId is null)
        {
            TempData["Error"] = "L'installazione non è attualmente online.";
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

        logger.LogInformation("StartUpdate sent via UI: Package={PackageId} Installation={InstallationId}", packageId, installationId);
        TempData["Success"] = $"Aggiornamento {pkg.Component} {pkg.Version} inviato all'installazione.";
        return RedirectToPage();
    }

    // ── Imposta modalità aggiornamento ────────────────────────────────────
    public async Task<IActionResult> OnPostSetUpdateModeAsync(Guid installationId, InstallationUpdateMode mode)
    {
        await installationService.SetUpdateModeAsync(installationId, mode);
        TempData["Success"] = $"Modalità aggiornamento aggiornata.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var installs = await installationService.GetAllAsync();
        var online = connectionTracker.GetOnlineInstallationIds();
        ReadyPackages = await packageService.GetByStatusAsync(PackageStatus.ReadyToDeploy);

        var installationIds = installs.Select(i => i.Id);
        var historyMap = await installationService.GetAllRecentHistoryAsync(installationIds, maxPerInstallation: 5);

        Installations = installs.Select(i =>
        {
            historyMap.TryGetValue(i.Id, out var history);
            return new InstallationRow(i, online.Contains(i.Id), history ?? []);
        }).ToList();
    }
}
