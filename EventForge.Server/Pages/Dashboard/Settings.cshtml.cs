using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class SettingsModel : PageModel
{
    private readonly IConfigurationService _configService;
    private readonly IConfiguration _configuration;
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

    // ── Security ──────────────────────────────────────────────────────
    [BindProperty] public bool SecurityEnforceHttps { get; set; } = true;
    [BindProperty] public bool SecurityEnableHsts { get; set; } = true;
    [BindProperty] public int SecurityHstsMaxAge { get; set; } = 31536000;

    // ── JWT ───────────────────────────────────────────────────────────
    [BindProperty] public int JwtExpirationMinutes { get; set; } = 600;
    [BindProperty] public int JwtClockSkewMinutes { get; set; } = 5;

    // ── Password policy ───────────────────────────────────────────────
    [BindProperty] public int PasswordMinLength { get; set; } = 8;
    [BindProperty] public int PasswordMaxLength { get; set; } = 128;
    [BindProperty] public bool PasswordRequireUppercase { get; set; } = true;
    [BindProperty] public bool PasswordRequireLowercase { get; set; } = true;
    [BindProperty] public bool PasswordRequireDigits { get; set; } = true;
    [BindProperty] public bool PasswordRequireSpecialChars { get; set; } = true;
    [BindProperty] public string PasswordSpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?";
    [BindProperty] public int PasswordMaxAge { get; set; } = 90;
    [BindProperty] public int PasswordHistory { get; set; } = 5;

    // ── Account lockout ───────────────────────────────────────────────
    [BindProperty] public int LockoutMaxFailedAttempts { get; set; } = 5;
    [BindProperty] public int LockoutDurationMinutes { get; set; } = 30;
    [BindProperty] public bool LockoutResetOnSuccess { get; set; } = true;

    // ── CORS ──────────────────────────────────────────────────────────
    [BindProperty] public string CorsAllowedOrigins { get; set; } = string.Empty;

    // ── Performance monitoring ────────────────────────────────────────
    [BindProperty] public int PerfSlowRequestThresholdMs { get; set; } = 200;
    [BindProperty] public int PerfSlowQueryThresholdSeconds { get; set; } = 2;
    [BindProperty] public int PerfMaxSlowQueryHistory { get; set; } = 100;
    [BindProperty] public bool PerfEnableDetailedLogging { get; set; } = true;
    [BindProperty] public bool PerfLogAllQueries { get; set; } = false;

    // ── Pagination ────────────────────────────────────────────────────
    [BindProperty] public int PaginationDefaultPageSize { get; set; } = 20;
    [BindProperty] public int PaginationMaxPageSize { get; set; } = 1000;
    [BindProperty] public int PaginationMaxExportPageSize { get; set; } = 10000;
    [BindProperty] public int PaginationRecommendedPageSize { get; set; } = 100;

    // ── CSV Import ────────────────────────────────────────────────────
    [BindProperty] public int CsvMaxFileSizeMb { get; set; } = 10;
    [BindProperty] public int CsvBatchSize { get; set; } = 100;
    [BindProperty] public string CsvDefaultCurrency { get; set; } = "EUR";
    [BindProperty] public int CsvMaxRowsPreview { get; set; } = 10;

    // ── Price history ─────────────────────────────────────────────────
    [BindProperty] public int PriceHistoryRetentionDays { get; set; } = 730;
    [BindProperty] public bool PriceHistoryEnableAutoLogging { get; set; } = true;
    [BindProperty] public int PriceHistoryCacheMinutes { get; set; } = 5;
    [BindProperty] public int PriceHistoryDefaultPageSize { get; set; } = 20;
    [BindProperty] public int PriceHistoryMaxPageSize { get; set; } = 100;
    [BindProperty] public decimal PriceHistorySignificantChangePercent { get; set; } = 5.0m;

    // ── Supplier suggestion ───────────────────────────────────────────
    [BindProperty] public decimal SuggestionWeightPrice { get; set; } = 0.4m;
    [BindProperty] public decimal SuggestionWeightLeadTime { get; set; } = 0.25m;
    [BindProperty] public decimal SuggestionWeightReliability { get; set; } = 0.2m;
    [BindProperty] public decimal SuggestionWeightTrend { get; set; } = 0.15m;
    [BindProperty] public int SuggestionMinDataPoints { get; set; } = 3;
    [BindProperty] public int SuggestionTrendPeriodDays { get; set; } = 180;
    [BindProperty] public int SuggestionCacheMinutes { get; set; } = 5;
    [BindProperty] public int SuggestionConfidenceLow { get; set; } = 60;
    [BindProperty] public int SuggestionConfidenceHigh { get; set; } = 80;
    [BindProperty] public bool SuggestionEnableAuto { get; set; } = true;
    [BindProperty] public decimal SuggestionPriceChangeThreshold { get; set; } = 10.0m;

    // ── Supplier alerts ───────────────────────────────────────────────
    [BindProperty] public decimal AlertPriceIncreasePercent { get; set; } = 5.0m;
    [BindProperty] public decimal AlertPriceDecreasePercent { get; set; } = 10.0m;
    [BindProperty] public decimal AlertVolatilityPercent { get; set; } = 15.0m;
    [BindProperty] public int AlertDaysWithoutUpdate { get; set; } = 90;
    [BindProperty] public bool AlertEnableEmail { get; set; } = true;
    [BindProperty] public int AlertMaxPerDigest { get; set; } = 50;
    [BindProperty] public int AlertRetentionResolvedDays { get; set; } = 90;
    [BindProperty] public int AlertRetentionDismissedDays { get; set; } = 30;
    [BindProperty] public int AlertHeartbeatSeconds { get; set; } = 30;

    // ── VAT Lookup ────────────────────────────────────────────────────
    [BindProperty] public string VatLookupUrlTemplate { get; set; } = string.Empty;
    [BindProperty] public int VatLookupTimeoutSeconds { get; set; } = 10;

    // ── UpdateHub ─────────────────────────────────────────────────────
    [BindProperty] public string UpdateHubBaseUrl { get; set; } = string.Empty;
    [BindProperty] public string UpdateHubAdminApiKey { get; set; } = string.Empty;
    [BindProperty] public string UpdateHubMaintenanceSecret { get; set; } = string.Empty;

    // ── Agent ─────────────────────────────────────────────────────────
    [BindProperty] public string AgentLocalUrl { get; set; } = string.Empty;
    [BindProperty] public string AgentUsername { get; set; } = string.Empty;
    [BindProperty] public string AgentPassword { get; set; } = string.Empty;
    [BindProperty] public int AgentPollIntervalSeconds { get; set; } = 30;
    [BindProperty] public int AgentAutoRestartAfterMinutes { get; set; } = 5;

    // ── WhatsApp ──────────────────────────────────────────────────────
    [BindProperty] public bool WhatsAppEnabled { get; set; } = false;
    [BindProperty] public string WhatsAppPhoneNumberId { get; set; } = string.Empty;
    [BindProperty] public string WhatsAppAccessToken { get; set; } = string.Empty;
    [BindProperty] public string WhatsAppVerifyToken { get; set; } = string.Empty;
    [BindProperty] public string WhatsAppApiVersion { get; set; } = "v19.0";

    // ── HttpClient (server self-reference) ────────────────────────────
    [BindProperty] public string HttpClientBaseAddress { get; set; } = string.Empty;
    [BindProperty] public int HttpClientPort { get; set; } = 7241;

    // ── Syncfusion ────────────────────────────────────────────────────
    [BindProperty] public string SyncfusionLicenseKey { get; set; } = string.Empty;

    // ── Bootstrap ─────────────────────────────────────────────────────
    [BindProperty] public string BootstrapDefaultAdminUsername { get; set; } = string.Empty;
    [BindProperty] public string BootstrapDefaultAdminEmail { get; set; } = string.Empty;
    [BindProperty] public bool BootstrapAutoCreateAdmin { get; set; } = false;
    [BindProperty] public string BootstrapStoreOperatorPassword { get; set; } = string.Empty;

    // ── Serilog (programmatic config parameters) ──────────────────────
    [BindProperty] public bool SerilogEnableConsole { get; set; } = false;
    [BindProperty] public string SerilogFilePath { get; set; } = "Logs/log-.log";
    [BindProperty] public int SerilogFileRetention { get; set; } = 7;

    // ── Read-only infrastructure info (from IConfiguration) ──────────
    public string? InfoDatabaseProvider { get; set; }
    public string? InfoConnectionStringDefault { get; set; }
    public string? InfoConnectionStringLogDb { get; set; }
    public string? InfoConnectionStringRedis { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public SmtpTestResultDto? SmtpTestResult { get; set; }

    public SettingsModel(IConfigurationService configService, IConfiguration configuration, ILogger<SettingsModel> logger)
    {
        _configService = configService;
        _configuration = configuration;
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
                Subject = "PRYM SMTP Test",
                Body = "Questo è un messaggio di test inviato da PRYM Server Dashboard."
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

    // ── Security ──────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSecurityAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Security_EnforceHttps", SecurityEnforceHttps.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Security_EnableHsts", SecurityEnableHsts.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Security_HstsMaxAge", SecurityHstsMaxAge.ToString(), null, ct);
            await _configService.SetValueAsync("Jwt_ExpirationMinutes", JwtExpirationMinutes.ToString(), null, ct);
            await _configService.SetValueAsync("Jwt_ClockSkewMinutes", JwtClockSkewMinutes.ToString(), null, ct);

            _logger.LogInformation("Security settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni di sicurezza salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving security settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "sicurezza" });
    }

    // ── Password policy ───────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdatePasswordPolicyAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("PasswordPolicy_MinLength", PasswordMinLength.ToString(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_MaxLength", PasswordMaxLength.ToString(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_RequireUppercase", PasswordRequireUppercase.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_RequireLowercase", PasswordRequireLowercase.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_RequireDigits", PasswordRequireDigits.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_RequireSpecialChars", PasswordRequireSpecialChars.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_SpecialCharacters", PasswordSpecialCharacters, null, ct);
            await _configService.SetValueAsync("PasswordPolicy_MaxAge", PasswordMaxAge.ToString(), null, ct);
            await _configService.SetValueAsync("PasswordPolicy_History", PasswordHistory.ToString(), null, ct);

            _logger.LogInformation("Password policy updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Politica password salvata.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving password policy");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "sicurezza" });
    }

    // ── Account lockout ───────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateLockoutAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("AccountLockout_MaxFailedAttempts", LockoutMaxFailedAttempts.ToString(), null, ct);
            await _configService.SetValueAsync("AccountLockout_DurationMinutes", LockoutDurationMinutes.ToString(), null, ct);
            await _configService.SetValueAsync("AccountLockout_ResetOnSuccess", LockoutResetOnSuccess.ToString().ToLower(), null, ct);

            _logger.LogInformation("Account lockout settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni blocco account salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving account lockout settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "sicurezza" });
    }

    // ── CORS ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateCorsAsync()
    {
        try
        {
            await _configService.SetValueAsync("Cors_AllowedOrigins", CorsAllowedOrigins, null, HttpContext.RequestAborted);
            _logger.LogInformation("CORS settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni CORS salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving CORS settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "sicurezza" });
    }

    // ── Performance monitoring ────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdatePerfMonitoringAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Performance_SlowRequestThresholdMs", PerfSlowRequestThresholdMs.ToString(), null, ct);
            await _configService.SetValueAsync("Performance_SlowQueryThresholdSeconds", PerfSlowQueryThresholdSeconds.ToString(), null, ct);
            await _configService.SetValueAsync("Performance_MaxSlowQueryHistory", PerfMaxSlowQueryHistory.ToString(), null, ct);
            await _configService.SetValueAsync("Performance_EnableDetailedLogging", PerfEnableDetailedLogging.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Performance_LogAllQueries", PerfLogAllQueries.ToString().ToLower(), null, ct);

            _logger.LogInformation("Performance monitoring settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni monitoraggio performance salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving performance monitoring settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "performance" });
    }

    // ── Pagination ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdatePaginationAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Pagination_DefaultPageSize", PaginationDefaultPageSize.ToString(), null, ct);
            await _configService.SetValueAsync("Pagination_MaxPageSize", PaginationMaxPageSize.ToString(), null, ct);
            await _configService.SetValueAsync("Pagination_MaxExportPageSize", PaginationMaxExportPageSize.ToString(), null, ct);
            await _configService.SetValueAsync("Pagination_RecommendedPageSize", PaginationRecommendedPageSize.ToString(), null, ct);

            _logger.LogInformation("Pagination settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni paginazione salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving pagination settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "parametri" });
    }

    // ── CSV Import ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateCsvImportAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            var maxBytes = (long)Math.Clamp(CsvMaxFileSizeMb, 1, 2048) * 1024 * 1024;
            await _configService.SetValueAsync("CsvImport_MaxFileSizeBytes", maxBytes.ToString(), null, ct);
            await _configService.SetValueAsync("CsvImport_BatchSize", CsvBatchSize.ToString(), null, ct);
            await _configService.SetValueAsync("CsvImport_DefaultCurrency", CsvDefaultCurrency, null, ct);
            await _configService.SetValueAsync("CsvImport_MaxRowsPreview", CsvMaxRowsPreview.ToString(), null, ct);

            _logger.LogInformation("CSV import settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni importazione CSV salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving CSV import settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "parametri" });
    }

    // ── Price history ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdatePriceHistoryAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("PriceHistory_RetentionDays", PriceHistoryRetentionDays.ToString(), null, ct);
            await _configService.SetValueAsync("PriceHistory_EnableAutoLogging", PriceHistoryEnableAutoLogging.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("PriceHistory_CacheMinutes", PriceHistoryCacheMinutes.ToString(), null, ct);
            await _configService.SetValueAsync("PriceHistory_DefaultPageSize", PriceHistoryDefaultPageSize.ToString(), null, ct);
            await _configService.SetValueAsync("PriceHistory_MaxPageSize", PriceHistoryMaxPageSize.ToString(), null, ct);
            await _configService.SetValueAsync("PriceHistory_SignificantChangePercent", PriceHistorySignificantChangePercent.ToString(System.Globalization.CultureInfo.InvariantCulture), null, ct);

            _logger.LogInformation("Price history settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni storico prezzi salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving price history settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "parametri" });
    }

    // ── Supplier suggestion ───────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSupplierSuggestionAsync()
    {
        try
        {
            if (SuggestionConfidenceLow >= SuggestionConfidenceHigh)
            {
                TempData["ErrorMessage"] = "La soglia di confidenza bassa deve essere inferiore a quella alta.";
                return RedirectToPage(new { tab = "parametri" });
            }

            var ct = HttpContext.RequestAborted;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            await _configService.SetValueAsync("SupplierSuggestion_WeightPrice", SuggestionWeightPrice.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_WeightLeadTime", SuggestionWeightLeadTime.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_WeightReliability", SuggestionWeightReliability.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_WeightTrend", SuggestionWeightTrend.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_MinDataPoints", SuggestionMinDataPoints.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_TrendPeriodDays", SuggestionTrendPeriodDays.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_CacheMinutes", SuggestionCacheMinutes.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_ConfidenceLow", SuggestionConfidenceLow.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_ConfidenceHigh", SuggestionConfidenceHigh.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_EnableAuto", SuggestionEnableAuto.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("SupplierSuggestion_PriceChangeThreshold", SuggestionPriceChangeThreshold.ToString(inv), null, ct);

            _logger.LogInformation("Supplier suggestion settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni suggerimento fornitori salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving supplier suggestion settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "parametri" });
    }

    // ── Supplier alerts ───────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSupplierAlertsAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            await _configService.SetValueAsync("SupplierAlerts_PriceIncreasePercent", AlertPriceIncreasePercent.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_PriceDecreasePercent", AlertPriceDecreasePercent.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_VolatilityPercent", AlertVolatilityPercent.ToString(inv), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_DaysWithoutUpdate", AlertDaysWithoutUpdate.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_EnableEmail", AlertEnableEmail.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_MaxPerDigest", AlertMaxPerDigest.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_RetentionResolvedDays", AlertRetentionResolvedDays.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_RetentionDismissedDays", AlertRetentionDismissedDays.ToString(), null, ct);
            await _configService.SetValueAsync("SupplierAlerts_HeartbeatSeconds", AlertHeartbeatSeconds.ToString(), null, ct);

            _logger.LogInformation("Supplier alerts settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni alert fornitori salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving supplier alerts settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "parametri" });
    }

    // ── VAT Lookup ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateVatLookupAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("VatLookup_UrlTemplate", VatLookupUrlTemplate, null, ct);
            await _configService.SetValueAsync("VatLookup_TimeoutSeconds", VatLookupTimeoutSeconds.ToString(), null, ct);

            _logger.LogInformation("VAT lookup settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni lookup IVA salvate.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving VAT lookup settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "integrazioni" });
    }

    // ── UpdateHub ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateHubAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("UpdateHub_BaseUrl", UpdateHubBaseUrl, null, ct);
            if (!string.IsNullOrWhiteSpace(UpdateHubAdminApiKey))
                await _configService.SetValueAsync("UpdateHub_AdminApiKey", UpdateHubAdminApiKey, null, ct);
            if (!string.IsNullOrWhiteSpace(UpdateHubMaintenanceSecret))
                await _configService.SetValueAsync("UpdateHub_MaintenanceSecret", UpdateHubMaintenanceSecret, null, ct);

            _logger.LogInformation("UpdateHub settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni UpdateHub salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving UpdateHub settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "integrazioni" });
    }

    // ── Agent ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateAgentAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Agent_LocalUrl", AgentLocalUrl, null, ct);
            await _configService.SetValueAsync("Agent_Username", AgentUsername, null, ct);
            if (!string.IsNullOrWhiteSpace(AgentPassword))
                await _configService.SetValueAsync("Agent_Password", AgentPassword, null, ct);
            await _configService.SetValueAsync("Agent_PollIntervalSeconds", AgentPollIntervalSeconds.ToString(), null, ct);
            await _configService.SetValueAsync("Agent_AutoRestartAfterMinutes", AgentAutoRestartAfterMinutes.ToString(), null, ct);

            _logger.LogInformation("Agent settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni Agent salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving agent settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "integrazioni" });
    }

    // ── WhatsApp ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateWhatsAppAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("WhatsApp_Enabled", WhatsAppEnabled.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("WhatsApp_PhoneNumberId", WhatsAppPhoneNumberId, null, ct);
            await _configService.SetValueAsync("WhatsApp_VerifyToken", WhatsAppVerifyToken, null, ct);
            await _configService.SetValueAsync("WhatsApp_ApiVersion", WhatsAppApiVersion, null, ct);

            // AccessToken only if filled in (avoid overwriting with blank)
            if (!string.IsNullOrWhiteSpace(WhatsAppAccessToken))
                await _configService.SetValueAsync("WhatsApp_AccessToken", WhatsAppAccessToken, null, ct);

            _logger.LogInformation("WhatsApp settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni WhatsApp salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving WhatsApp settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "integrazioni" });
    }

    // ── HttpClient ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateHttpClientAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("HttpClient_BaseAddress", HttpClientBaseAddress, null, ct);
            await _configService.SetValueAsync("HttpClient_Port", HttpClientPort.ToString(), null, ct);

            _logger.LogInformation("HttpClient settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni HttpClient salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving HttpClient settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "avanzate" });
    }

    // ── Syncfusion ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSyncfusionAsync()
    {
        try
        {
            await _configService.SetValueAsync("Syncfusion_LicenseKey", SyncfusionLicenseKey, null, HttpContext.RequestAborted);
            _logger.LogInformation("Syncfusion license key updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Chiave di licenza Syncfusion salvata. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Syncfusion license key");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "avanzate" });
    }

    // ── Bootstrap ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateBootstrapAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Bootstrap_DefaultAdminUsername", BootstrapDefaultAdminUsername, null, ct);
            await _configService.SetValueAsync("Bootstrap_DefaultAdminEmail", BootstrapDefaultAdminEmail, null, ct);
            await _configService.SetValueAsync("Bootstrap_AutoCreateAdmin", BootstrapAutoCreateAdmin.ToString().ToLower(), null, ct);
            if (!string.IsNullOrWhiteSpace(BootstrapStoreOperatorPassword))
                await _configService.SetValueAsync("Bootstrap_StoreOperatorPassword", BootstrapStoreOperatorPassword, null, ct);

            _logger.LogInformation("Bootstrap settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni bootstrap salvate. Si applicano solo al prossimo avvio se il database non è già inizializzato.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving bootstrap settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "avanzate" });
    }

    // ── Serilog ───────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateSerilogAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            await _configService.SetValueAsync("Serilog_EnableConsole", SerilogEnableConsole.ToString().ToLower(), null, ct);
            await _configService.SetValueAsync("Serilog_FilePath", SerilogFilePath, null, ct);
            await _configService.SetValueAsync("Serilog_FileRetention", SerilogFileRetention.ToString(), null, ct);

            _logger.LogInformation("Serilog settings updated by {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Impostazioni Serilog salvate. Riavvio necessario per applicare le modifiche.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Serilog settings");
            TempData["ErrorMessage"] = $"Errore: {ex.Message}";
        }
        return RedirectToPage(new { tab = "avanzate" });
    }

    private async Task LoadAllSettingsAsync()
    {
        var ct = HttpContext.RequestAborted;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        try
        {
            // ── SMTP ──
            SmtpServer = await _configService.GetValueAsync("SMTP_Server", "smtp.example.com", ct);
            SmtpPort = int.TryParse(await _configService.GetValueAsync("SMTP_Port", "587", ct), out var p) ? p : 587;
            SmtpUseSsl = (await _configService.GetValueAsync("SMTP_EnableSSL", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SmtpUsername = await _configService.GetValueAsync("SMTP_Username", "", ct);
            SmtpFromEmail = await _configService.GetValueAsync("SMTP_FromEmail", "noreply@eventforge.com", ct);
            SmtpFromName = await _configService.GetValueAsync("SMTP_FromName", "PRYM", ct);
            // Password not loaded on GET for security

            // ── Logging ──
            LogLevel = await _configService.GetValueAsync("Logging_Level", "Information", ct);
            LogRetentionDays = int.TryParse(await _configService.GetValueAsync("Logging_RetentionDays", "30", ct), out var r) ? r : 30;

            // ── Rate limiting ──
            LoginLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_LoginLimit", "5", ct), out var l1) ? l1 : 5;
            ApiLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_ApiLimit", "100", ct), out var l2) ? l2 : 100;
            TokenRefreshLimit = int.TryParse(await _configService.GetValueAsync("RateLimiting_TokenRefreshLimit", "1", ct), out var l3) ? l3 : 1;

            // ── Feature flags ──
            FeatureDocumentCollaboration = (await _configService.GetValueAsync("Feature_DocumentCollaboration", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            FeatureAdvancedReporting = (await _configService.GetValueAsync("Feature_AdvancedReporting", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            FeatureDetailedAudit = (await _configService.GetValueAsync("Feature_DetailedAudit", "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);

            // ── Security ──
            SecurityEnforceHttps = (await _configService.GetValueAsync("Security_EnforceHttps", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SecurityEnableHsts = (await _configService.GetValueAsync("Security_EnableHsts", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SecurityHstsMaxAge = int.TryParse(await _configService.GetValueAsync("Security_HstsMaxAge", "31536000", ct), out var hsts) ? hsts : 31536000;

            // ── JWT ──
            JwtExpirationMinutes = int.TryParse(await _configService.GetValueAsync("Jwt_ExpirationMinutes", "600", ct), out var jwtExp) ? jwtExp : 600;
            JwtClockSkewMinutes = int.TryParse(await _configService.GetValueAsync("Jwt_ClockSkewMinutes", "5", ct), out var jwtSkew) ? jwtSkew : 5;

            // ── Password policy ──
            PasswordMinLength = int.TryParse(await _configService.GetValueAsync("PasswordPolicy_MinLength", "8", ct), out var pMin) ? pMin : 8;
            PasswordMaxLength = int.TryParse(await _configService.GetValueAsync("PasswordPolicy_MaxLength", "128", ct), out var pMax) ? pMax : 128;
            PasswordRequireUppercase = (await _configService.GetValueAsync("PasswordPolicy_RequireUppercase", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PasswordRequireLowercase = (await _configService.GetValueAsync("PasswordPolicy_RequireLowercase", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PasswordRequireDigits = (await _configService.GetValueAsync("PasswordPolicy_RequireDigits", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PasswordRequireSpecialChars = (await _configService.GetValueAsync("PasswordPolicy_RequireSpecialChars", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PasswordSpecialCharacters = await _configService.GetValueAsync("PasswordPolicy_SpecialCharacters", "!@#$%^&*()_+-=[]{}|;:,.<>?", ct);
            PasswordMaxAge = int.TryParse(await _configService.GetValueAsync("PasswordPolicy_MaxAge", "90", ct), out var pAge) ? pAge : 90;
            PasswordHistory = int.TryParse(await _configService.GetValueAsync("PasswordPolicy_History", "5", ct), out var pHist) ? pHist : 5;

            // ── Account lockout ──
            LockoutMaxFailedAttempts = int.TryParse(await _configService.GetValueAsync("AccountLockout_MaxFailedAttempts", "5", ct), out var lockMax) ? lockMax : 5;
            LockoutDurationMinutes = int.TryParse(await _configService.GetValueAsync("AccountLockout_DurationMinutes", "30", ct), out var lockDur) ? lockDur : 30;
            LockoutResetOnSuccess = (await _configService.GetValueAsync("AccountLockout_ResetOnSuccess", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);

            // ── CORS ──
            CorsAllowedOrigins = await _configService.GetValueAsync("Cors_AllowedOrigins", "https://localhost:7009", ct);

            // ── Performance monitoring ──
            PerfSlowRequestThresholdMs = int.TryParse(await _configService.GetValueAsync("Performance_SlowRequestThresholdMs", "200", ct), out var perfSlow) ? perfSlow : 200;
            PerfSlowQueryThresholdSeconds = int.TryParse(await _configService.GetValueAsync("Performance_SlowQueryThresholdSeconds", "2", ct), out var perfQuery) ? perfQuery : 2;
            PerfMaxSlowQueryHistory = int.TryParse(await _configService.GetValueAsync("Performance_MaxSlowQueryHistory", "100", ct), out var perfHist) ? perfHist : 100;
            PerfEnableDetailedLogging = (await _configService.GetValueAsync("Performance_EnableDetailedLogging", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PerfLogAllQueries = (await _configService.GetValueAsync("Performance_LogAllQueries", "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);

            // ── Pagination ──
            PaginationDefaultPageSize = int.TryParse(await _configService.GetValueAsync("Pagination_DefaultPageSize", "20", ct), out var pagDef) ? pagDef : 20;
            PaginationMaxPageSize = int.TryParse(await _configService.GetValueAsync("Pagination_MaxPageSize", "1000", ct), out var pagMax) ? pagMax : 1000;
            PaginationMaxExportPageSize = int.TryParse(await _configService.GetValueAsync("Pagination_MaxExportPageSize", "10000", ct), out var pagExp) ? pagExp : 10000;
            PaginationRecommendedPageSize = int.TryParse(await _configService.GetValueAsync("Pagination_RecommendedPageSize", "100", ct), out var pagRec) ? pagRec : 100;

            // ── CSV Import ──
            var csvMaxBytes = long.TryParse(await _configService.GetValueAsync("CsvImport_MaxFileSizeBytes", "10485760", ct), out var csvBytes) ? csvBytes : 10485760L;
            CsvMaxFileSizeMb = (int)Math.Clamp(csvMaxBytes / 1024 / 1024, 1, 2048);
            CsvBatchSize = int.TryParse(await _configService.GetValueAsync("CsvImport_BatchSize", "100", ct), out var csvBatch) ? csvBatch : 100;
            CsvDefaultCurrency = await _configService.GetValueAsync("CsvImport_DefaultCurrency", "EUR", ct);
            CsvMaxRowsPreview = int.TryParse(await _configService.GetValueAsync("CsvImport_MaxRowsPreview", "10", ct), out var csvPrev) ? csvPrev : 10;

            // ── Price history ──
            PriceHistoryRetentionDays = int.TryParse(await _configService.GetValueAsync("PriceHistory_RetentionDays", "730", ct), out var phRet) ? phRet : 730;
            PriceHistoryEnableAutoLogging = (await _configService.GetValueAsync("PriceHistory_EnableAutoLogging", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            PriceHistoryCacheMinutes = int.TryParse(await _configService.GetValueAsync("PriceHistory_CacheMinutes", "5", ct), out var phCache) ? phCache : 5;
            PriceHistoryDefaultPageSize = int.TryParse(await _configService.GetValueAsync("PriceHistory_DefaultPageSize", "20", ct), out var phDef) ? phDef : 20;
            PriceHistoryMaxPageSize = int.TryParse(await _configService.GetValueAsync("PriceHistory_MaxPageSize", "100", ct), out var phMax) ? phMax : 100;
            PriceHistorySignificantChangePercent = decimal.TryParse(await _configService.GetValueAsync("PriceHistory_SignificantChangePercent", "5.0", ct), System.Globalization.NumberStyles.Any, inv, out var phSig) ? phSig : 5.0m;

            // ── Supplier suggestion ──
            SuggestionWeightPrice = decimal.TryParse(await _configService.GetValueAsync("SupplierSuggestion_WeightPrice", "0.4", ct), System.Globalization.NumberStyles.Any, inv, out var swP) ? swP : 0.4m;
            SuggestionWeightLeadTime = decimal.TryParse(await _configService.GetValueAsync("SupplierSuggestion_WeightLeadTime", "0.25", ct), System.Globalization.NumberStyles.Any, inv, out var swL) ? swL : 0.25m;
            SuggestionWeightReliability = decimal.TryParse(await _configService.GetValueAsync("SupplierSuggestion_WeightReliability", "0.2", ct), System.Globalization.NumberStyles.Any, inv, out var swR) ? swR : 0.2m;
            SuggestionWeightTrend = decimal.TryParse(await _configService.GetValueAsync("SupplierSuggestion_WeightTrend", "0.15", ct), System.Globalization.NumberStyles.Any, inv, out var swT) ? swT : 0.15m;
            SuggestionMinDataPoints = int.TryParse(await _configService.GetValueAsync("SupplierSuggestion_MinDataPoints", "3", ct), out var sMin) ? sMin : 3;
            SuggestionTrendPeriodDays = int.TryParse(await _configService.GetValueAsync("SupplierSuggestion_TrendPeriodDays", "180", ct), out var sTrend) ? sTrend : 180;
            SuggestionCacheMinutes = int.TryParse(await _configService.GetValueAsync("SupplierSuggestion_CacheMinutes", "5", ct), out var sCache) ? sCache : 5;
            SuggestionConfidenceLow = int.TryParse(await _configService.GetValueAsync("SupplierSuggestion_ConfidenceLow", "60", ct), out var sCLow) ? sCLow : 60;
            SuggestionConfidenceHigh = int.TryParse(await _configService.GetValueAsync("SupplierSuggestion_ConfidenceHigh", "80", ct), out var sCHigh) ? sCHigh : 80;
            SuggestionEnableAuto = (await _configService.GetValueAsync("SupplierSuggestion_EnableAuto", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SuggestionPriceChangeThreshold = decimal.TryParse(await _configService.GetValueAsync("SupplierSuggestion_PriceChangeThreshold", "10.0", ct), System.Globalization.NumberStyles.Any, inv, out var sPct) ? sPct : 10.0m;

            // ── Supplier alerts ──
            AlertPriceIncreasePercent = decimal.TryParse(await _configService.GetValueAsync("SupplierAlerts_PriceIncreasePercent", "5.0", ct), System.Globalization.NumberStyles.Any, inv, out var aPI) ? aPI : 5.0m;
            AlertPriceDecreasePercent = decimal.TryParse(await _configService.GetValueAsync("SupplierAlerts_PriceDecreasePercent", "10.0", ct), System.Globalization.NumberStyles.Any, inv, out var aPD) ? aPD : 10.0m;
            AlertVolatilityPercent = decimal.TryParse(await _configService.GetValueAsync("SupplierAlerts_VolatilityPercent", "15.0", ct), System.Globalization.NumberStyles.Any, inv, out var aVol) ? aVol : 15.0m;
            AlertDaysWithoutUpdate = int.TryParse(await _configService.GetValueAsync("SupplierAlerts_DaysWithoutUpdate", "90", ct), out var aDWU) ? aDWU : 90;
            AlertEnableEmail = (await _configService.GetValueAsync("SupplierAlerts_EnableEmail", "true", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            AlertMaxPerDigest = int.TryParse(await _configService.GetValueAsync("SupplierAlerts_MaxPerDigest", "50", ct), out var aMPD) ? aMPD : 50;
            AlertRetentionResolvedDays = int.TryParse(await _configService.GetValueAsync("SupplierAlerts_RetentionResolvedDays", "90", ct), out var aRR) ? aRR : 90;
            AlertRetentionDismissedDays = int.TryParse(await _configService.GetValueAsync("SupplierAlerts_RetentionDismissedDays", "30", ct), out var aRD) ? aRD : 30;
            AlertHeartbeatSeconds = int.TryParse(await _configService.GetValueAsync("SupplierAlerts_HeartbeatSeconds", "30", ct), out var aHB) ? aHB : 30;

            // ── VAT Lookup ──
            VatLookupUrlTemplate = await _configService.GetValueAsync("VatLookup_UrlTemplate", "https://api.vatcomply.com/vat?vat={country}{vat}", ct);
            VatLookupTimeoutSeconds = int.TryParse(await _configService.GetValueAsync("VatLookup_TimeoutSeconds", "10", ct), out var vatTo) ? vatTo : 10;

            // ── UpdateHub ──
            UpdateHubBaseUrl = await _configService.GetValueAsync("UpdateHub_BaseUrl", "", ct);
            // API key and maintenance secret not loaded for security

            // ── Agent ──
            AgentLocalUrl = await _configService.GetValueAsync("Agent_LocalUrl", "http://localhost:5780", ct);
            AgentUsername = await _configService.GetValueAsync("Agent_Username", "admin", ct);
            // Password not loaded for security
            AgentPollIntervalSeconds = int.TryParse(await _configService.GetValueAsync("Agent_PollIntervalSeconds", "30", ct), out var aPoll) ? aPoll : 30;
            AgentAutoRestartAfterMinutes = int.TryParse(await _configService.GetValueAsync("Agent_AutoRestartAfterMinutes", "5", ct), out var aRestart) ? aRestart : 5;

            // ── WhatsApp ──
            WhatsAppEnabled = (await _configService.GetValueAsync("WhatsApp_Enabled", _configuration["WhatsApp:Enabled"] ?? "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            WhatsAppPhoneNumberId = await _configService.GetValueAsync("WhatsApp_PhoneNumberId", _configuration["WhatsApp:PhoneNumberId"] ?? "", ct);
            WhatsAppVerifyToken = await _configService.GetValueAsync("WhatsApp_VerifyToken", _configuration["WhatsApp:VerifyToken"] ?? "", ct);
            WhatsAppApiVersion = await _configService.GetValueAsync("WhatsApp_ApiVersion", _configuration["WhatsApp:ApiVersion"] ?? "v19.0", ct);
            // AccessToken not loaded for security

            // ── HttpClient ──
            HttpClientBaseAddress = await _configService.GetValueAsync("HttpClient_BaseAddress", _configuration["HttpClient:BaseAddress"] ?? "https://localhost", ct);
            HttpClientPort = int.TryParse(await _configService.GetValueAsync("HttpClient_Port", _configuration["HttpClient:Port"] ?? "7241", ct), out var hcPort) ? hcPort : 7241;

            // ── Syncfusion ──
            SyncfusionLicenseKey = await _configService.GetValueAsync("Syncfusion_LicenseKey", "", ct);

            // ── Bootstrap ──
            BootstrapDefaultAdminUsername = await _configService.GetValueAsync("Bootstrap_DefaultAdminUsername", _configuration["Bootstrap:DefaultAdminUsername"] ?? "superadmin", ct);
            BootstrapDefaultAdminEmail = await _configService.GetValueAsync("Bootstrap_DefaultAdminEmail", _configuration["Bootstrap:DefaultAdminEmail"] ?? "superadmin@localhost", ct);
            BootstrapAutoCreateAdmin = (await _configService.GetValueAsync("Bootstrap_AutoCreateAdmin", _configuration["Bootstrap:AutoCreateAdmin"] ?? "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            // Bootstrap passwords not loaded for security

            // ── Serilog ──
            SerilogEnableConsole = (await _configService.GetValueAsync("Serilog_EnableConsole", _configuration["Serilog:EnableConsole"] ?? "false", ct)).Equals("true", StringComparison.OrdinalIgnoreCase);
            SerilogFilePath = await _configService.GetValueAsync("Serilog_FilePath", _configuration["Serilog:FilePath"] ?? "Logs/log-.log", ct);
            SerilogFileRetention = int.TryParse(await _configService.GetValueAsync("Serilog_FileRetention", _configuration["Serilog:FileRetention"] ?? "7", ct), out var sRet) ? sRet : 7;

            // ── Read-only infra info ──
            InfoDatabaseProvider = _configuration["DatabaseProvider"];
            InfoConnectionStringDefault = MaskConnectionString(_configuration.GetConnectionString("DefaultConnection"));
            InfoConnectionStringLogDb = MaskConnectionString(_configuration.GetConnectionString("LogDb"));
            InfoConnectionStringRedis = _configuration.GetConnectionString("Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from configuration service");
            ErrorMessage = "Impossibile caricare alcune impostazioni dal database.";
        }
    }

    /// <summary>Masks sensitive values in a connection string for safe display.</summary>
    private static readonly System.Text.RegularExpressions.Regex _connStrSensitivePattern =
        new(@"(Password|PWD|Pass)\s*=\s*[^;]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string? MaskConnectionString(string? cs)
    {
        if (string.IsNullOrEmpty(cs)) return cs;
        return _connStrSensitivePattern.Replace(cs, "$1=****");
    }
}
