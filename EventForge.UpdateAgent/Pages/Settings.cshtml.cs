using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.UpdateAgent.Pages;

public class SettingsModel : PageModel
{
    private readonly AgentOptions _options;
    private readonly AgentStatusService _agentStatus;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(AgentOptions options, AgentStatusService agentStatus, ILogger<SettingsModel> logger)
    {
        _options     = options;
        _agentStatus = agentStatus;
        _logger      = logger;
    }

    public AgentOptions Options => _options;

    public void OnGet() { }

    public IActionResult OnPostSave(
        // Identity
        string InstallationName, string? Location, string? Tags,
        // Hub
        string HubUrl, string HubBaseUrl, string ApiKey, string? EnrollmentToken,
        int HeartbeatIntervalSeconds, int ReconnectDelaySeconds,
        // Download
        int DownloadTimeoutMinutes, int DownloadMaxRetries,
        // Install
        int Install_HealthCheckMaxAttempts, int Install_HealthCheckDelaySeconds,
        int Install_IisWarmupDelaySeconds, int Install_SqlCommandTimeoutSeconds,
        int Install_ScheduledCheckIntervalSeconds,
        // Backup
        int Backup_MaxBackupsToKeep, string? Backup_RootPath,
        // Logging
        int Logging_RetentionDays, string? Logging_DirectoryPath,
        // UI
        int UI_Port, string? UI_Username, string? UI_Password,
        // Components
        bool Server_Enabled, string? Server_DeployPath, string? Server_HealthCheckUrl,
        string? Server_IISSiteName, string? Server_AppPoolName, string? Server_ConnectionString,
        bool Client_Enabled, string? Client_DeployPath)
    {
        try
        {
            var tagList = (Tags ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // Mutate in-memory singleton (immediate effect for what doesn't need restart)
            _options.InstallationName                     = InstallationName;
            _options.Location                             = string.IsNullOrWhiteSpace(Location) ? null : Location;
            _options.Tags                                 = tagList;
            _options.HubUrl                               = HubUrl;
            _options.HubBaseUrl                           = HubBaseUrl;
            _options.ApiKey                               = ApiKey;
            _options.EnrollmentToken                      = EnrollmentToken ?? string.Empty;
            _options.HeartbeatIntervalSeconds             = HeartbeatIntervalSeconds;
            _options.ReconnectDelaySeconds                = ReconnectDelaySeconds;
            _options.DownloadTimeoutMinutes               = DownloadTimeoutMinutes;
            _options.DownloadMaxRetries                   = DownloadMaxRetries;
            _options.Install.HealthCheckMaxAttempts       = Install_HealthCheckMaxAttempts;
            _options.Install.HealthCheckDelaySeconds      = Install_HealthCheckDelaySeconds;
            _options.Install.IisWarmupDelaySeconds        = Install_IisWarmupDelaySeconds;
            _options.Install.SqlCommandTimeoutSeconds     = Install_SqlCommandTimeoutSeconds;
            _options.Install.ScheduledCheckIntervalSeconds = Install_ScheduledCheckIntervalSeconds;
            _options.Backup.MaxBackupsToKeep              = Backup_MaxBackupsToKeep;
            _options.Backup.RootPath                      = string.IsNullOrWhiteSpace(Backup_RootPath) ? null : Backup_RootPath;
            _options.Logging.RetentionDays                = Logging_RetentionDays;
            _options.Logging.DirectoryPath                = string.IsNullOrWhiteSpace(Logging_DirectoryPath) ? null : Logging_DirectoryPath;
            _options.UI.Port                              = UI_Port;
            _options.UI.Username                          = UI_Username ?? string.Empty;
            _options.UI.Password                          = UI_Password ?? string.Empty;
            _options.Components.Server.Enabled            = Server_Enabled;
            _options.Components.Server.DeployPath         = Server_DeployPath ?? string.Empty;
            _options.Components.Server.HealthCheckUrl     = Server_HealthCheckUrl ?? string.Empty;
            _options.Components.Server.IISSiteName        = Server_IISSiteName ?? string.Empty;
            _options.Components.Server.AppPoolName        = Server_AppPoolName ?? string.Empty;
            _options.Components.Server.ConnectionString   = Server_ConnectionString ?? string.Empty;
            _options.Components.Client.Enabled            = Client_Enabled;
            _options.Components.Client.DeployPath         = Client_DeployPath ?? string.Empty;

            // Persist to appsettings.json
            PersistToAppSettings(_options);

            TempData["Success"] = "Configurazione salvata. Riavvia il servizio per applicare i parametri che richiedono riavvio.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            TempData["Error"] = $"Errore durante il salvataggio: {ex.Message}";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostReRegister()
    {
        // Signal to AgentWorker to re-send RegisterInstallation
        _agentStatus.RequestReRegister();
        TempData["Info"] = "Ri-registrazione inviata. L'agent invierà RegisterInstallation al prossimo ciclo heartbeat.";
        return RedirectToPage();
    }

    private static readonly string AppSettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static void PersistToAppSettings(AgentOptions opts)
    {
        JsonNode root;
        if (System.IO.File.Exists(AppSettingsPath))
        {
            var text = System.IO.File.ReadAllText(AppSettingsPath);
            root = JsonNode.Parse(text) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var section = root[AgentOptions.SectionName] as JsonObject ?? new JsonObject();

        section["HubUrl"]               = opts.HubUrl;
        section["ApiKey"]               = opts.ApiKey;
        section["EnrollmentToken"]      = opts.EnrollmentToken;
        section["InstallationName"]     = opts.InstallationName;
        section["Location"]             = opts.Location;
        section["HubBaseUrl"]           = opts.HubBaseUrl;
        section["HeartbeatIntervalSeconds"] = opts.HeartbeatIntervalSeconds;
        section["ReconnectDelaySeconds"]    = opts.ReconnectDelaySeconds;
        section["DownloadTimeoutMinutes"]   = opts.DownloadTimeoutMinutes;
        section["DownloadMaxRetries"]       = opts.DownloadMaxRetries;

        var tags = new JsonArray();
        foreach (var t in opts.Tags) tags.Add(t);
        section["Tags"] = tags;

        var install = new JsonObject
        {
            ["HealthCheckMaxAttempts"]        = opts.Install.HealthCheckMaxAttempts,
            ["HealthCheckDelaySeconds"]       = opts.Install.HealthCheckDelaySeconds,
            ["IisWarmupDelaySeconds"]         = opts.Install.IisWarmupDelaySeconds,
            ["SqlCommandTimeoutSeconds"]      = opts.Install.SqlCommandTimeoutSeconds,
            ["ScheduledCheckIntervalSeconds"] = opts.Install.ScheduledCheckIntervalSeconds
        };
        section["Install"] = install;

        var backup = new JsonObject
        {
            ["MaxBackupsToKeep"] = opts.Backup.MaxBackupsToKeep,
            ["RootPath"]         = opts.Backup.RootPath
        };
        section["Backup"] = backup;

        var logging = new JsonObject
        {
            ["RetentionDays"]  = opts.Logging.RetentionDays,
            ["DirectoryPath"]  = opts.Logging.DirectoryPath
        };
        section["Logging"] = logging;

        var ui = new JsonObject
        {
            ["Port"]     = opts.UI.Port,
            ["Username"] = opts.UI.Username,
            ["Password"] = opts.UI.Password
        };
        section["UI"] = ui;

        var server = new JsonObject
        {
            ["Enabled"]           = opts.Components.Server.Enabled,
            ["DeployPath"]        = opts.Components.Server.DeployPath,
            ["HealthCheckUrl"]    = opts.Components.Server.HealthCheckUrl,
            ["IISSiteName"]       = opts.Components.Server.IISSiteName,
            ["AppPoolName"]       = opts.Components.Server.AppPoolName,
            ["ConnectionString"]  = opts.Components.Server.ConnectionString
        };

        var client = new JsonObject
        {
            ["Enabled"]    = opts.Components.Client.Enabled,
            ["DeployPath"] = opts.Components.Client.DeployPath
        };

        section["Components"] = new JsonObject { ["Server"] = server, ["Client"] = client };

        root[AgentOptions.SectionName] = section;

        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        System.IO.File.WriteAllText(AppSettingsPath, root.ToJsonString(writeOptions));
    }
}
