using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.ManagementHub.Pages;

/// <summary>
/// API Keys management page model for the Hub web UI.
/// Displays all registered installations with their masked API keys,
/// online status, and allows revoke / reinstate / reissue / delete operations.
/// </summary>
public class ApiKeysModel(
    IInstallationService installationService,
    IConnectionTracker connectionTracker,
    ILogger<ApiKeysModel> logger) : PageModel
{
    public IReadOnlyList<InstallationRow> Installations { get; private set; } = [];

    public record InstallationRow(
        Installation Installation,
        bool IsOnline,
        string MaskedKey);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // ── Revoca ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostRevokeAsync(Guid id, string? reason)
    {
        var ok = await installationService.RevokeAsync(id, reason);
        if (!ok) { TempData["Error"] = "Installazione non trovata."; return RedirectToPage(); }

        logger.LogWarning("Installation revoked via UI: Id={Id} Reason={Reason}", id, reason);
        TempData["Warning"] = "Chiave API revocata. L'agent non potrà più connettersi fino alla reintegrazione.";
        return RedirectToPage();
    }

    // ── Reintegra ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostReinstateAsync(Guid id)
    {
        var ok = await installationService.ReinstateAsync(id);
        if (!ok) { TempData["Error"] = "Installazione non trovata."; return RedirectToPage(); }

        logger.LogInformation("Installation reinstated via UI: Id={Id}", id);
        TempData["Success"] = "Installazione reintegrata. La chiave API è di nuovo attiva.";
        return RedirectToPage();
    }

    // ── Ri-emissione chiave ───────────────────────────────────────────────
    public async Task<IActionResult> OnPostReissueAsync(Guid id)
    {
        var newKey = await installationService.ReissueApiKeyAsync(id);
        if (newKey is null) { TempData["Error"] = "Installazione non trovata."; return RedirectToPage(); }

        logger.LogWarning("API key reissued via UI: Id={Id}", id);
        // Show the new key once — it won't be displayed again
        TempData["NewKey"] = newKey;
        TempData["NewKeyInstallationId"] = id.ToString();
        TempData["Success"] = "Nuova chiave API generata. Copiala subito — non sarà più visibile.";
        return RedirectToPage();
    }

    // ── Elimina installazione ─────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var (ok, error) = await installationService.DeleteAsync(id);
        if (!ok)
        {
            TempData["Error"] = error ?? "Impossibile eliminare l'installazione.";
            return RedirectToPage();
        }

        logger.LogWarning("Installation deleted via ApiKeys UI: Id={Id}", id);
        TempData["Success"] = "Installazione eliminata definitivamente.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var installs = await installationService.GetAllAsync();
        var online = connectionTracker.GetOnlineInstallationIds();

        Installations = installs.Select(i => new InstallationRow(
            i,
            online.Contains(i.Id),
            MaskKey(i.ApiKey)
        )).ToList();
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8) return "••••••••";
        return key[..8] + new string('•', Math.Min(24, key.Length - 8));
    }
}
