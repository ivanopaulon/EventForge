using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.Agent.Pages;

/// <summary>
/// Settings page model for the Agent local web UI.
/// Displays and saves all <see cref="AgentOptions"/> parameters.
/// Most fields take effect immediately on the running process;
/// connection and port settings require a restart.
/// </summary>
public class SettingsModel(AgentOptions options, AgentStatusService agentStatus, ILogger<SettingsModel> logger) : PageModel
{
    public AgentOptions Options => options;

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
            options.InstallationName                     = InstallationName;
            options.Location                             = string.IsNullOrWhiteSpace(Location) ? null : Location;
            options.Tags                                 = tagList;
            options.HubUrl                               = HubUrl;
            options.HubBaseUrl                           = HubBaseUrl;
            options.ApiKey                               = ApiKey;
            options.EnrollmentToken                      = EnrollmentToken ?? string.Empty;
            options.HeartbeatIntervalSeconds             = HeartbeatIntervalSeconds;
            options.ReconnectDelaySeconds                = ReconnectDelaySeconds;
            options.DownloadTimeoutMinutes               = DownloadTimeoutMinutes;
            options.DownloadMaxRetries                   = DownloadMaxRetries;
            options.Install.HealthCheckMaxAttempts       = Install_HealthCheckMaxAttempts;
            options.Install.HealthCheckDelaySeconds      = Install_HealthCheckDelaySeconds;
            options.Install.IisWarmupDelaySeconds        = Install_IisWarmupDelaySeconds;
            options.Install.SqlCommandTimeoutSeconds     = Install_SqlCommandTimeoutSeconds;
            options.Install.ScheduledCheckIntervalSeconds = Install_ScheduledCheckIntervalSeconds;
            options.Backup.MaxBackupsToKeep              = Backup_MaxBackupsToKeep;
            options.Backup.RootPath                      = string.IsNullOrWhiteSpace(Backup_RootPath) ? null : Backup_RootPath;
            options.Logging.RetentionDays                = Logging_RetentionDays;
            options.Logging.DirectoryPath                = string.IsNullOrWhiteSpace(Logging_DirectoryPath) ? null : Logging_DirectoryPath;
            options.UI.Port                              = UI_Port;
            options.UI.Username                          = UI_Username ?? string.Empty;
            options.UI.Password                          = UI_Password ?? string.Empty;
            options.Components.Server.Enabled            = Server_Enabled;
            options.Components.Server.DeployPath         = Server_DeployPath ?? string.Empty;
            options.Components.Server.HealthCheckUrl     = Server_HealthCheckUrl ?? string.Empty;
            options.Components.Server.IISSiteName        = Server_IISSiteName ?? string.Empty;
            options.Components.Server.AppPoolName        = Server_AppPoolName ?? string.Empty;
            options.Components.Server.ConnectionString   = Server_ConnectionString ?? string.Empty;
            options.Components.Client.Enabled            = Client_Enabled;
            options.Components.Client.DeployPath         = Client_DeployPath ?? string.Empty;

            // Persist to appsettings.json
            PersistToAppSettings(options);

            // Immediately push identity/tag changes to the Hub without waiting for restart.
            agentStatus.RequestReRegister();

            TempData["Success"] = "Configurazione salvata. Nome, posizione e tag aggiornati sull'Hub immediatamente. Per HubUrl/ApiKey usa \"Riconnetti\"; porta UI richiede riavvio servizio.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save settings");
            TempData["Error"] = $"Errore durante il salvataggio: {ex.Message}";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostReRegister()
    {
        // Signal to AgentWorker to re-send RegisterInstallation
        agentStatus.RequestReRegister();
        TempData["Info"] = "Ri-registrazione inviata. L'agent invierà RegisterInstallation al prossimo ciclo heartbeat.";
        return RedirectToPage();
    }

    public IActionResult OnPostReconnect()
    {
        agentStatus.RequestReconnect();
        TempData["Info"] = "Riconnessione richiesta. L'agent si disconnetterà e si riconnetterà all'Hub con le nuove impostazioni entro il prossimo heartbeat.";
        return RedirectToPage();
    }

    private static readonly string AppSettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static readonly JsonSerializerOptions _writeIndentOpts =
        new() { WriteIndented = true };

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

        // Also update the env-specific section (Environments:{env}:PrymAgent) for any keys that
        // are already overridden there, so they don't shadow the saved values on the next restart.
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var envSection = root["Environments"]?[env]?[AgentOptions.SectionName] as JsonObject;
        if (envSection is not null)
        {
            if (envSection["InstallationName"]     is not null) envSection["InstallationName"]     = opts.InstallationName;
            if (envSection["Location"]             is not null) envSection["Location"]             = opts.Location;
            if (envSection["HubUrl"]               is not null) envSection["HubUrl"]               = opts.HubUrl;
            if (envSection["HubBaseUrl"]           is not null) envSection["HubBaseUrl"]           = opts.HubBaseUrl;
            if (envSection["EnrollmentToken"]      is not null) envSection["EnrollmentToken"]      = opts.EnrollmentToken;
            if (envSection["HeartbeatIntervalSeconds"] is not null) envSection["HeartbeatIntervalSeconds"] = opts.HeartbeatIntervalSeconds;
            if (envSection["ReconnectDelaySeconds"]    is not null) envSection["ReconnectDelaySeconds"]    = opts.ReconnectDelaySeconds;
            if (envSection["Tags"] is not null)
            {
                var envTags = new JsonArray();
                foreach (var t in opts.Tags) envTags.Add(t);
                envSection["Tags"] = envTags;
            }
            var envInstall = envSection["Install"] as JsonObject;
            if (envInstall is not null)
            {
                if (envInstall["HealthCheckMaxAttempts"]        is not null) envInstall["HealthCheckMaxAttempts"]        = opts.Install.HealthCheckMaxAttempts;
                if (envInstall["HealthCheckDelaySeconds"]       is not null) envInstall["HealthCheckDelaySeconds"]       = opts.Install.HealthCheckDelaySeconds;
                if (envInstall["IisWarmupDelaySeconds"]         is not null) envInstall["IisWarmupDelaySeconds"]         = opts.Install.IisWarmupDelaySeconds;
                if (envInstall["SqlCommandTimeoutSeconds"]      is not null) envInstall["SqlCommandTimeoutSeconds"]      = opts.Install.SqlCommandTimeoutSeconds;
                if (envInstall["ScheduledCheckIntervalSeconds"] is not null) envInstall["ScheduledCheckIntervalSeconds"] = opts.Install.ScheduledCheckIntervalSeconds;
            }
            var envComponents = envSection["Components"] as JsonObject;
            if (envComponents is not null)
            {
                var envServer = envComponents["Server"] as JsonObject;
                if (envServer is not null)
                {
                    if (envServer["Enabled"]          is not null) envServer["Enabled"]          = opts.Components.Server.Enabled;
                    if (envServer["DeployPath"]        is not null) envServer["DeployPath"]        = opts.Components.Server.DeployPath;
                    if (envServer["HealthCheckUrl"]    is not null) envServer["HealthCheckUrl"]    = opts.Components.Server.HealthCheckUrl;
                    if (envServer["IISSiteName"]       is not null) envServer["IISSiteName"]       = opts.Components.Server.IISSiteName;
                    if (envServer["AppPoolName"]       is not null) envServer["AppPoolName"]       = opts.Components.Server.AppPoolName;
                    if (envServer["ConnectionString"]  is not null) envServer["ConnectionString"]  = opts.Components.Server.ConnectionString;
                }
                var envClient = envComponents["Client"] as JsonObject;
                if (envClient is not null)
                {
                    if (envClient["Enabled"]    is not null) envClient["Enabled"]    = opts.Components.Client.Enabled;
                    if (envClient["DeployPath"] is not null) envClient["DeployPath"] = opts.Components.Client.DeployPath;
                }
            }
        }

        // Write atomically: .tmp first, then rename, to avoid corruption on crash.
        var tmpPath = AppSettingsPath + ".tmp";
        System.IO.File.WriteAllText(tmpPath, root.ToJsonString(_writeIndentOpts));
        System.IO.File.Move(tmpPath, AppSettingsPath, overwrite: true);
    }
}
