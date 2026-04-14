using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.ManagementHub.Pages;

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
    IUpdateThrottleService updateThrottle,
    ManagementHubOptions hubOptions,
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
        // Sanitize version string to prevent path traversal in the generated filename.
        var safeVersion = FileNameHelper.SanitizeForFileName(version);
        var fileName = $"{comp.ToString().ToLowerInvariant()}-{safeVersion}-{Guid.NewGuid():N}.zip";
        var filePath = Path.Combine(storePath, fileName);

        string checksum;
        try
        {
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File upload I/O error for package {Version} {Component} File={File}",
                version, component, fileName);
            if (System.IO.File.Exists(filePath))
                try { System.IO.File.Delete(filePath); } catch { /* best effort cleanup */ }
            TempData["Error"] = $"Errore durante il salvataggio del file: {ex.Message}";
            return RedirectToPage();
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

        try
        {
            var created = await packageService.CreateAsync(package);
            logger.LogInformation("Package uploaded via UI: {Version} {Component} Id={Id} Manual={Manual}",
                version, component, created.Id, isManualInstall);
            TempData["Success"] = $"Pacchetto {comp} {version} caricato con successo.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist package record for {Version} {Component} File={File}",
                version, component, fileName);
            if (System.IO.File.Exists(filePath))
                try { System.IO.File.Delete(filePath); } catch { /* best effort cleanup */ }
            TempData["Error"] = $"Errore durante il salvataggio del pacchetto nel database: {ex.Message}";
        }

        return RedirectToPage();
    }

    // ── Suggerisci versione (AJAX) ─────────────────────────────────────────
    public async Task<IActionResult> OnPostSuggestVersionAsync(string component, string versionType = "minor")
    {
        if (!versionType.Equals("major", StringComparison.OrdinalIgnoreCase) &&
            !versionType.Equals("minor", StringComparison.OrdinalIgnoreCase))
            versionType = "minor";

        // "Both" → suggest the higher of the two components' last versions
        if (component.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            var serverSuggested = await packageService.GetSuggestedNextVersionAsync(PackageComponent.Server, versionType);
            var clientSuggested = await packageService.GetSuggestedNextVersionAsync(PackageComponent.Client, versionType);

            // Pick the higher version so both components advance to the same version
            var pick = CompareVersionStrings(serverSuggested, clientSuggested) >= 0
                ? serverSuggested : clientSuggested;

            return new JsonResult(new { version = pick, component = "Both", type = versionType });
        }

        if (!Enum.TryParse<PackageComponent>(component, true, out var comp))
            return new JsonResult(new { error = "Componente non valido." });

        var suggested = await packageService.GetSuggestedNextVersionAsync(comp, versionType);
        return new JsonResult(new { version = suggested, component = comp.ToString(), type = versionType });
    }

    /// <summary>
    /// Compares two version strings of the form "major.minor.patch[-suffix]".
    /// Returns positive if <paramref name="a"/> is greater, negative if less, 0 if equal.
    /// </summary>
    private static int CompareVersionStrings(string a, string b)
    {
        if (Version.TryParse(a.Split('-')[0], out var va) && Version.TryParse(b.Split('-')[0], out var vb))
            return va.CompareTo(vb);
        return string.Compare(a, b, StringComparison.Ordinal);
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
        string? gitCommit, bool isManualInstall,
        string? clientFolderPath)          // used only when component == "Both"
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            TempData["Error"] = "Il percorso della cartella è obbligatorio.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            TempData["Error"] = "La versione è obbligatoria.";
            return RedirectToPage();
        }

        var versionTrimmed     = version.Trim();
        var releaseNotesTrimmed = string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes.Trim();
        var gitCommitTrimmed   = string.IsNullOrWhiteSpace(gitCommit) ? null : gitCommit.Trim();

        // ── Server + Client in one shot ───────────────────────────────────
        if (component.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            var serverFolder = folderPath.Trim();
            var clientFolder = clientFolderPath?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clientFolder))
            {
                TempData["Error"] = "Per 'Server + Client' occorre specificare anche la cartella del Client.";
                return RedirectToPage();
            }

            var errors   = new List<string>();
            var successes = new List<string>();

            foreach (var (comp, folder) in new[]
            {
                (PackageComponent.Server, serverFolder),
                (PackageComponent.Client, clientFolder)
            })
            {
                try
                {
                    var created = await packageBuildService.BuildFromFolderAsync(
                        folder, comp, versionTrimmed, releaseNotesTrimmed, gitCommitTrimmed, isManualInstall);

                    logger.LogInformation("Package built via UI (Both): {Component} {Version} Id={Id} Manual={Manual}",
                        comp, versionTrimmed, created.Id, isManualInstall);
                    successes.Add($"{comp} {versionTrimmed} (Id: {created.Id})");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Build from folder failed for {Component}: {Folder}", comp, folder);
                    errors.Add($"{comp}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
                TempData["Error"] = "Errori: " + string.Join(" | ", errors) +
                    (successes.Count > 0 ? " — Creati: " + string.Join(", ", successes) : string.Empty);
            else
                TempData["Success"] = $"Pacchetti creati: {string.Join(", ", successes)}.";

            return RedirectToPage();
        }

        // ── Single component ──────────────────────────────────────────────
        if (!Enum.TryParse<PackageComponent>(component, true, out var singleComp))
        {
            TempData["Error"] = "Componente non valido.";
            return RedirectToPage();
        }

        try
        {
            var created = await packageBuildService.BuildFromFolderAsync(
                folderPath.Trim(), singleComp, versionTrimmed,
                releaseNotesTrimmed, gitCommitTrimmed, isManualInstall);

            logger.LogInformation("Package built via UI: {Component} {Version} Id={Id} Manual={Manual}",
                singleComp, versionTrimmed, created.Id, isManualInstall);
            TempData["Success"] = $"Pacchetto {singleComp} {versionTrimmed} creato con successo dalla cartella.";
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

        await updateThrottle.AcquireAsync(HttpContext.RequestAborted);

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

    // ── Elimina pacchetto ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var pkg = await packageService.GetByIdAsync(id);
        if (pkg is null) { TempData["Error"] = "Pacchetto non trovato."; return RedirectToPage(); }

        if (pkg.Status != PackageStatus.Archived)
        {
            TempData["Error"] = $"Il pacchetto {pkg.Component} {pkg.Version} non è archiviato. Archivialo prima di eliminarlo.";
            return RedirectToPage();
        }

        await packageService.DeleteAsync(id);
        logger.LogInformation("Package deleted via UI: {Component} {Version} Id={Id}", pkg.Component, pkg.Version, id);
        TempData["Success"] = $"Pacchetto {pkg.Component} {pkg.Version} eliminato definitivamente.";
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
