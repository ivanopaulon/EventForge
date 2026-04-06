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
    IConnectionTracker connectionTracker,
    IHubContext<AgentHub> agentHubContext,
    UpdateHubOptions hubOptions,
    ILogger<PackagesModel> logger) : PageModel
{
    public IReadOnlyList<UpdatePackage> Packages { get; private set; } = [];
    public int OnlineCount { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // ── Carica pacchetto ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUploadAsync(
        string version, string component, string? releaseNotes, IFormFile? file)
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
            Checksum = checksum,
            FilePath = fileName,
            FileSizeBytes = file.Length
        };

        var created = await packageService.CreateAsync(package);
        logger.LogInformation("Package uploaded via UI: {Version} {Component} Id={Id}", version, component, created.Id);
        TempData["Success"] = $"Pacchetto {comp} {version} caricato con successo.";
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

        var notification = new UpdateAvailableMessage(
            pkg.Id, pkg.Version, pkg.Component.ToString(),
            $"/api/v1/packages/{pkg.Id}/download",
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
        OnlineCount = connectionTracker.GetOnlineInstallationIds().Count;
    }
}
