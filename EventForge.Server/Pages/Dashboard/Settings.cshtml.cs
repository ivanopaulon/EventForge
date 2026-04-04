using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class SettingsModel : PageModel
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<SettingsModel> _logger;

    // ── SMTP ──────────────────────────────────────────────────────────
    [BindProperty] public string SmtpServer { get; set; } = string.Empty;
    [BindProperty] public int SmtpPort { get; set; } = 587;
    [BindProperty] public bool SmtpUseSsl { get; set; } = true;
    [BindProperty] public string SmtpUsername { get; set; } = string.Empty;
    [BindProperty] public string SmtpPassword { get; set; } = string.Empty;
    [BindProperty] public string SmtpFromEmail { get; set; } = string.Empty;
    [BindProperty] public string SmtpFromName { get; set; } = string.Empty;

    // ── Logging ───────────────────────────────────────────────────────
    [BindProperty] public string LogLevel { get; set; } = "Information";
    [BindProperty] public int LogRetentionDays { get; set; } = 30;

    // ── Rate limiting ─────────────────────────────────────────────────
    [BindProperty] public int LoginLimit { get; set; } = 5;
    [BindProperty] public int ApiLimit { get; set; } = 100;
    [BindProperty] public int TokenRefreshLimit { get; set; } = 1;

    // ── Feature flags ─────────────────────────────────────────────────
    [BindProperty] public bool FeatureDocumentCollaboration { get; set; } = true;
    [BindProperty] public bool FeatureAdvancedReporting { get; set; } = true;
    [BindProperty] public bool FeatureDetailedAudit { get; set; } = false;

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public SmtpTestResultDto? SmtpTestResult { get; set; }

    public SettingsModel(IConfigurationService configService, ILogger<SettingsModel> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if (TempData["SuccessMessage"] is string ok) SuccessMessage = ok;
        if (TempData["ErrorMessage"] is string err) ErrorMessage = err;

        await LoadAllSettingsAsync();
    }

    // ── SMTP ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSmtpAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("SMTP_Server", SmtpServer, null, ct);
            await _configService.SetValueAsync("SMTP_Port", SmtpPort.ToString(), null, ct);
            await _configService.SetValueAsync("SMTP_EnableSSL", SmtpUseSsl.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("SMTP_Username", SmtpUsername, null, ct);
            await _configService.SetValueAsync("SMTP_FromEmail", SmtpFromEmail, null, ct);
            await _configService.SetValueAsync("SMTP_FromName", SmtpFromName, null, ct);

            // Password only if filled in (avoid overwriting with blank)
            if (!string.IsNullOrWhiteSpace(SmtpPassword))
                await _configService.SetValueAsync("SMTP_Password", SmtpPassword, null, ct);

            _logger.LogInformation("SMTP settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni SMTP salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SMTP settings");
            TempData["ErrorMessage"] = $"Errore durante il salvataggio: {ex.Message}";
        }
        return RedirectToPage(new { tab = "config" });
    }

    public async Task<IActionResult> OnPostTestSmtpAsync(string testEmail)
    {
        await LoadAllSettingsAsync();
        try
        {
            var testDto = new SmtpTestDto
            {
                ToEmail = testEmail,
                Subject = "EventForge SMTP Test",
                Body = "Questo è un messaggio di test inviato da EventForge Server Dashboard."
            };
            SmtpTestResult = await _configService.TestSmtpAsync(testDto, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            SmtpTestResult = new SmtpTestResultDto { Success = false, ErrorMessage = ex.Message };
        }
        return Page();
    }

    // ── Logging ───────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateLoggingAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Logging_Level", LogLevel, null, ct);
            await _configService.SetValueAsync("Logging_RetentionDays", LogRetentionDays.ToString(), null, ct);

            _logger.LogInformation("Logging settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni di logging salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving logging settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "config" });
    }

    // ── Rate limiting ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateRateLimitAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("RateLimiting_LoginLimit", LoginLimit.ToString(), null, ct);
            await _configService.SetValueAsync("RateLimiting_ApiLimit", ApiLimit.ToString(), null, ct);
            await _configService.SetValueAsync("RateLimiting_TokenRefreshLimit", TokenRefreshLimit.ToString(), null, ct);

            _logger.LogInformation("Rate limiting settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni rate limiting salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rate limiting settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "performance" });
    }

    // ── Feature flags ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateFeaturesAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Feature_DocumentCollaboration", FeatureDocumentCollaboration.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Feature_AdvancedReporting", FeatureAdvancedReporting.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Feature_DetailedAudit", FeatureDetailedAudit.ToString().ToLower(), null, ct);

            _logger.LogInformation("Feature flags updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Feature flags salvati.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving feature flags");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "features" });
    }

    private async Task LoadAllSettingsAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            SmtpServer = await _configService.GetValueAsync("SMTP_Server", "smtp.example.com", ct);
            SmtpPort = int.TryParse(await _configService.GetValueAsync("SMTP_Port", "587", ct), out var p) ? p : 587;
            SmtpUseSsl = (await _configService.GetValueAsync("SMTP_EnableSSL", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SmtpUsername = await _configService.GetValueAsync("SMTP_Username", "", ct);
            SmtpFromEmail = await _configService.GetValueAsync("SMTP_FromEmail", "noreply@eventforge.com", ct);
            SmtpFromName = await _configService.GetValueAsync("SMTP_FromName", "EventForge", ct);
            // Password not loaded on GET for security

            LogLevel = await _configService.GetValueAsync("Logging_Level", "Information", ct);
            LogRetentionDays = int.TryParse(await _configService.GetValueAsync("Logging_RetentionDays", "30", ct), out var r) ? r : 30;

            LoginLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_LoginLimit", "5", ct), out var l1) ? l1 : 5;
            ApiLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_ApiLimit", "100", ct), out var l2) ? l2 : 100;
            TokenRefreshLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_TokenRefreshLimit", "1", ct), out var l3) ? l3 : 1;

            FeatureDocumentCollaboration = (await _configService.GetValueAsync("Feature_DocumentCollaboration", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            FeatureAdvancedReporting = (await _configService.GetValueAsync("Feature_AdvancedReporting", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            FeatureDetailedAudit = (await _configService.GetValueAsync("Feature_DetailedAudit", "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from configuration service");
            ErrorMessage = "Impossibile caricare alcune impostazioni dal database.";
        }
    }
}
