using EventForge.UpdateHub.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.UpdateHub.Pages;

/// <summary>
/// Packages management page model for the Hub web UI.
/// Lists all update packages, allows uploading new ones,
/// changing their lifecycle status, and broadcasting to all online agents.
/// </summary>
[RequestSizeLimit(524_288_000)] // 500 MB
public class PackagesModel(
    IPackageService packageService,
    IPackageBuildService packageBuildService,
    IInstallationService installationService,
    IConnectionTracker connectionTracker,
    IHubContext<AgentHub> agentHubContext,
    UpdateHubOptions hubOptions,
    ILogger<PackagesModel> logger) : PageModel
{
    public IReadOnlyList<UpdatePackage> Packages { get; private set; } = [];
    public int OnlineCount { get; private set; }

    /// <summary>Installations that are currently online, used to populate the targeted-deploy modal.</summary>
    public IReadOnlyList<OnlineInstallationRow> OnlineInstallations { get; private set; } = [];

    /// <summary>Pre-configured default deploy paths exposed to the view for UI hints.</summary>
    public string? DefaultServerDeployPath => hubOptions.DefaultServerDeployPath;
    public string? DefaultClientDeployPath => hubOptions.DefaultClientDeployPath;

    /// <summary>Row projected for the targeted-deploy modal (minimal set of fields needed).</summary>
    public record OnlineInstallationRow(Guid Id, string Name, string? Location, bool HasServer, bool HasClient);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // ── Carica pacchetto ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUploadAsync(
        string version, string component, string? releaseNotes, string? gitCommit,
        bool isManualInstall, IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            TempData["Error"] = "La versione è obbligatoria.";
            return RedirectToPage();
        }

        if (!Enum.TryParse<PackageComponent>(component, true, out var comp))
        {
            TempData["Error"] = "Componente non valido.";
            return RedirectToPage();
        }

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Il file è obbligatorio.";
            return RedirectToPage();
        }

        var storePath = hubOptions.PackageStorePath;
        Directory.CreateDirectory(storePath);
        var fileName = $"{comp.ToString().ToLowerInvariant()}-{version}-{Guid.NewGuid():N}.zip";
        var filePath = Path.Combine(storePath, fileName);

        string checksum;
        using (var fileStream = System.IO.File.Create(filePath))
        using (var hashAlg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[81920];
            using var source = file.OpenReadStream();
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer)) > 0)
            {
                hashAlg.AppendData(buffer, 0, bytesRead);
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
            checksum = Convert.ToHexStringLower(hashAlg.GetCurrentHash());
        }

        if (await packageService.ExistsByChecksumAsync(checksum))
        {
            System.IO.File.Delete(filePath);
            TempData["Error"] = "Un pacchetto identico (stesso checksum SHA-256) è già presente.";
            return RedirectToPage();
        }

        var package = new UpdatePackage
        {
            Version = version,
            Component = comp,
            ReleaseNotes = string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes,
            GitCommit = string.IsNullOrWhiteSpace(gitCommit) ? null : gitCommit.Trim(),
            Checksum = checksum,
            FilePath = fileName,
            FileSizeBytes = file.Length,
            IsManualInstall = isManualInstall
        };

        var created = await packageService.CreateAsync(package);
        logger.LogInformation("Package uploaded via UI: {Version} {Component} Id={Id} Manual={Manual}",
            version, component, created.Id, isManualInstall);
        TempData["Success"] = $"Pacchetto {comp} {version} caricato con successo.";
        return RedirectToPage();
    }

    // ── Rileva metadati cartella (AJAX) ───────────────────────────────────
    public async Task<IActionResult> OnPostDetectFolderAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return new JsonResult(new { error = "Percorso cartella obbligatorio." });

        var info = await packageBuildService.DetectFromFolderAsync(folderPath.Trim());

        if (info.DetectionError is not null)
            return new JsonResult(new { error = info.DetectionError });

        return new JsonResult(new
        {
            component = info.Component?.ToString(),
            version = info.Version,
            gitCommit = info.GitCommit
        });
    }

    // ── Crea pacchetto da cartella ────────────────────────────────────────
    public async Task<IActionResult> OnPostBuildFromFolderAsync(
        string folderPath, string component, string version, string? releaseNotes,
        string? gitCommit, bool isManualInstall)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            TempData["Error"] = "Il percorso della cartella è obbligatorio.";
            return RedirectToPage();
        }

        if (!Enum.TryParse<PackageComponent>(component, true, out var comp))
        {
            TempData["Error"] = "Componente non valido.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            TempData["Error"] = "La versione è obbligatoria.";
            return RedirectToPage();
        }

        try
        {
            var created = await packageBuildService.BuildFromFolderAsync(
                folderPath.Trim(), comp, version.Trim(),
                releaseNotes, string.IsNullOrWhiteSpace(gitCommit) ? null : gitCommit.Trim(),
                isManualInstall);

            logger.LogInformation("Package built via UI: {Component} {Version} Id={Id} Manual={Manual}",
                comp, version, created.Id, isManualInstall);
            TempData["Success"] = $"Pacchetto {comp} {version} creato con successo dalla cartella.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Build from folder failed: {Folder}", folderPath);
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    // ── Cambia stato pacchetto ────────────────────────────────────────────
    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<PackageStatus>(status, true, out var newStatus))
        {
            TempData["Error"] = "Stato non valido.";
            return RedirectToPage();
        }

        var pkg = await packageService.GetByIdAsync(id);
        if (pkg is null) { TempData["Error"] = "Pacchetto non trovato."; return RedirectToPage(); }

        await packageService.SetStatusAsync(id, newStatus);
        logger.LogInformation("Package {Id} status changed to {Status} via UI", id, newStatus);
        TempData["Success"] = $"Stato pacchetto {pkg.Component} {pkg.Version} aggiornato a {newStatus}.";
        return RedirectToPage();
    }

    // ── Targeted deploy a singola installazione ────────────────────────────
    public async Task<IActionResult> OnPostDeployToAsync(Guid packageId, Guid installationId)
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
        if (installation is null) { TempData["Error"] = "Installazione non trovata."; return RedirectToPage(); }

        var history = await installationService.StartUpdateHistoryAsync(
            installationId, packageId,
            installation.InstalledVersionServer,
            installation.InstalledVersionClient);

        var baseUrl = !string.IsNullOrWhiteSpace(hubOptions.BaseUrl)
            ? hubOptions.BaseUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";

        var command = new StartUpdateCommand(
            history.Id, pkg.Id, pkg.Version,
            pkg.Component.ToString(),
            $"{baseUrl}/api/v1/packages/{pkg.Id}/download",
            pkg.Checksum,
            IsManualInstall: pkg.IsManualInstall || installation.UpdateMode == InstallationUpdateMode.Manual);

        await agentHubContext.Clients.Client(connectionId).SendAsync("StartUpdate", command);
        await packageService.SetStatusAsync(packageId, PackageStatus.Deploying);

        logger.LogInformation("Targeted deploy via UI: Package={PackageId} Installation={InstallationId}", packageId, installationId);
        TempData["Success"] = $"Aggiornamento {pkg.Component} {pkg.Version} inviato a {installation.Name}.";
        return RedirectToPage();
    }

    // ── Broadcast a tutte le installazioni online ─────────────────────────
    public async Task<IActionResult> OnPostBroadcastAsync(Guid packageId)
    {
        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null) { TempData["Error"] = "Pacchetto non trovato."; return RedirectToPage(); }

        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        if (onlineIds.Count == 0)
        {
            TempData["Error"] = "Nessuna installazione online al momento.";
            return RedirectToPage();
        }

        var baseUrl = !string.IsNullOrWhiteSpace(hubOptions.BaseUrl)
            ? hubOptions.BaseUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";

        var notification = new UpdateAvailableMessage(
            pkg.Id, pkg.Version, pkg.Component.ToString(),
            $"{baseUrl}/api/v1/packages/{pkg.Id}/download",
            pkg.Checksum, pkg.ReleaseNotes);

        await agentHubContext.Clients.All.SendAsync("UpdateAvailable", notification);
        await packageService.SetStatusAsync(packageId, PackageStatus.Deploying);

        logger.LogInformation("Broadcast UpdateAvailable via UI: Package={PackageId} to {Count} installations", packageId, onlineIds.Count);
        TempData["Success"] = $"Broadcast {pkg.Component} {pkg.Version} inviato a {onlineIds.Count} installazioni online.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Packages = await packageService.GetAllAsync();
        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        OnlineCount = onlineIds.Count;

        var allInstallations = await installationService.GetAllAsync();
        OnlineInstallations = allInstallations
            .Where(i => !i.IsRevoked && onlineIds.Contains(i.Id))
            .Select(i => new OnlineInstallationRow(
                i.Id, i.Name, i.Location,
                i.Components == InstallationComponents.Server || i.Components == InstallationComponents.Both,
                i.Components == InstallationComponents.Client || i.Components == InstallationComponents.Both))
            .ToList();
    }
}
