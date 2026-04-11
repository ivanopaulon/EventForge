using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Prym.ManagementHub.Security;

namespace Prym.ManagementHub.Pages;

/// <summary>
/// Settings page model for the Hub web UI.
/// Displays and saves all <see cref="ManagementHubOptions"/> settings.
/// Changes are written to <c>appsettings.json</c> and applied in-memory immediately
/// (no restart required for most parameters).
/// </summary>
public class SettingsModel(
    ManagementHubOptions hubOptions,
    IWebHostEnvironment env,
    ILogger<SettingsModel> logger) : PageModel
{

    // ── Bind properties ───────────────────────────────────────────────────
    [BindProperty] public string PackageStorePath { get; set; } = string.Empty;
    [BindProperty] public string IncomingPackagesPath { get; set; } = string.Empty;
    [BindProperty] public string AdminApiKey { get; set; } = string.Empty;
    [BindProperty] public string EnrollmentToken { get; set; } = string.Empty;
    [BindProperty] public bool AllowAutoEnrollment { get; set; }
    [BindProperty] public string? BaseUrl { get; set; }
    [BindProperty] public int HeartbeatTimeoutSeconds { get; set; }
    [BindProperty] public int AgentStatusCheckIntervalSeconds { get; set; }
    [BindProperty] public int MaxConcurrentUpdates { get; set; }
    [BindProperty] public int MaxUploadSizeMb { get; set; }
    [BindProperty] public int PackageRetentionDays { get; set; }
    [BindProperty] public int PackageCleanupIntervalHours { get; set; }
    [BindProperty] public int LogRetentionDays { get; set; }
    [BindProperty] public string? LogDirectoryPath { get; set; }
    [BindProperty] public string UiUsername { get; set; } = string.Empty;
    /// <summary>
    /// New password submitted via the form. Empty means "keep the existing stored value".
    /// Never populated on GET — the current hash is never sent to the browser.
    /// </summary>
    [BindProperty] public string UiPassword { get; set; } = string.Empty;

    public bool IsDefaultCredentials { get; private set; }

    public void OnGet()
    {
        LoadFromOptions();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadFromOptions();
            return Page();
        }

        try
        {
            var appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
            var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            var updated = MergeSection(root, hubOptions);
            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(appSettingsPath, JsonSerializer.Serialize(updated, options));

            // Apply in-memory immediately
            hubOptions.PackageStorePath = PackageStorePath;
            hubOptions.IncomingPackagesPath = IncomingPackagesPath;
            hubOptions.AdminApiKey = AdminApiKey;
            hubOptions.EnrollmentToken = EnrollmentToken;
            hubOptions.AllowAutoEnrollment = AllowAutoEnrollment;
            hubOptions.BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl;
            hubOptions.HeartbeatTimeoutSeconds = HeartbeatTimeoutSeconds;
            hubOptions.AgentStatusCheckIntervalSeconds = AgentStatusCheckIntervalSeconds;
            hubOptions.MaxConcurrentUpdates = MaxConcurrentUpdates;
            hubOptions.MaxUploadSizeMb = MaxUploadSizeMb;
            hubOptions.PackageRetentionDays = PackageRetentionDays;
            hubOptions.PackageCleanupIntervalHours = PackageCleanupIntervalHours;
            hubOptions.Logging.RetentionDays = LogRetentionDays;
            hubOptions.Logging.DirectoryPath = string.IsNullOrWhiteSpace(LogDirectoryPath) ? null : LogDirectoryPath;
            hubOptions.UI.Username = UiUsername;
            // Only update password if a new one was typed; empty = keep existing stored value.
            var newPasswordToStore = string.IsNullOrEmpty(UiPassword)
                ? hubOptions.UI.Password   // keep existing (may already be hashed)
                : PasswordHasher.Hash(UiPassword);
            hubOptions.UI.Password = newPasswordToStore;

            logger.LogInformation("Hub settings saved via UI");
            TempData["Success"] = "Impostazioni salvate. Le credenziali UI sono attive immediatamente.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save Hub settings");
            TempData["Error"] = $"Errore durante il salvataggio: {ex.Message}";
        }

        return RedirectToPage();
    }

    private void LoadFromOptions()
    {
        PackageStorePath = hubOptions.PackageStorePath;
        IncomingPackagesPath = hubOptions.IncomingPackagesPath;
        AdminApiKey = hubOptions.AdminApiKey;
        EnrollmentToken = hubOptions.EnrollmentToken;
        AllowAutoEnrollment = hubOptions.AllowAutoEnrollment;
        BaseUrl = hubOptions.BaseUrl;
        HeartbeatTimeoutSeconds = hubOptions.HeartbeatTimeoutSeconds;
        AgentStatusCheckIntervalSeconds = hubOptions.AgentStatusCheckIntervalSeconds;
        MaxConcurrentUpdates = hubOptions.MaxConcurrentUpdates;
        MaxUploadSizeMb = hubOptions.MaxUploadSizeMb;
        PackageRetentionDays = hubOptions.PackageRetentionDays;
        PackageCleanupIntervalHours = hubOptions.PackageCleanupIntervalHours;
        LogRetentionDays = hubOptions.Logging.RetentionDays;
        LogDirectoryPath = hubOptions.Logging.DirectoryPath;
        UiUsername = hubOptions.UI.Username;
        UiPassword = string.Empty; // never send the stored hash to the browser
        // "default credentials" = still the well-known plaintext sentinel (not yet changed/hashed)
        IsDefaultCredentials = hubOptions.UI.Username == "admin" &&
                               !PasswordHasher.IsHashed(hubOptions.UI.Password) &&
                               hubOptions.UI.Password == "Admin#123!";
    }

    private Dictionary<string, object?> MergeSection(JsonElement root, ManagementHubOptions opts)
    {
        var result = new Dictionary<string, object?>();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == ManagementHubOptions.SectionName)
                continue;
            result[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
        }

        result[ManagementHubOptions.SectionName] = new Dictionary<string, object?>
        {
            ["PackageStorePath"] = PackageStorePath,
            ["IncomingPackagesPath"] = IncomingPackagesPath,
            ["AdminApiKey"] = AdminApiKey,
            ["EnrollmentToken"] = EnrollmentToken,
            ["AllowAutoEnrollment"] = AllowAutoEnrollment,
            ["BaseUrl"] = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl,
            ["HeartbeatTimeoutSeconds"] = HeartbeatTimeoutSeconds,
            ["AgentStatusCheckIntervalSeconds"] = AgentStatusCheckIntervalSeconds,
            ["MaxConcurrentUpdates"] = MaxConcurrentUpdates,
            ["MaxUploadSizeMb"] = MaxUploadSizeMb,
            ["PackageRetentionDays"] = PackageRetentionDays,
            ["PackageCleanupIntervalHours"] = PackageCleanupIntervalHours,
            ["Logging"] = new Dictionary<string, object?>
            {
                ["RetentionDays"] = LogRetentionDays,
                ["DirectoryPath"] = string.IsNullOrWhiteSpace(LogDirectoryPath) ? null : LogDirectoryPath
            },
            ["UI"] = new Dictionary<string, object?>
            {
                ["Username"] = UiUsername,
                ["Password"] = UiPassword
            }
        };

        return result;
    }
}
