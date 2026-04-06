using EventForge.UpdateAgent.Configuration;

namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Validates the Agent configuration at startup: ensures required directories exist
/// (creating them if absent) and logs a diagnostic warning for each anomaly found.
/// Call <see cref="Run"/> once, immediately after <see cref="AgentOptions"/> is bound.
/// </summary>
public static class StartupValidator
{
    /// <summary>
    /// Runs all startup checks. Returns <c>true</c> if no critical issues were found.
    /// </summary>
    public static bool Run(AgentOptions options, ILogger logger)
    {
        var ok = true;

        // ── Resolve the backup root (same logic as BackupService) ─────────────
        var backupRoot = !string.IsNullOrWhiteSpace(options.Backup.RootPath)
            ? options.Backup.RootPath
            : Path.Combine(AppContext.BaseDirectory, "backups");

        // ── Required directories (always created) ─────────────────────────────
        ok &= EnsureDirectory(backupRoot, "Backup.RootPath", logger);
        ok &= EnsureDirectory(Path.Combine(AppContext.BaseDirectory, "updates"), "updates (temp download)", logger);

        if (!string.IsNullOrWhiteSpace(options.Logging.DirectoryPath))
            ok &= EnsureDirectory(options.Logging.DirectoryPath, "Logging.DirectoryPath", logger);

        // ── Component deploy paths ────────────────────────────────────────────
        if (options.Components.Server.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Components.Server.DeployPath))
            {
                logger.LogError("[StartupValidator] Components.Server.Enabled=true but DeployPath is empty.");
                ok = false;
            }
            else
            {
                ok &= EnsureDirectory(options.Components.Server.DeployPath, "Components.Server.DeployPath", logger);
            }

            if (string.IsNullOrWhiteSpace(options.Components.Server.IISSiteName))
                logger.LogWarning("[StartupValidator] Components.Server.IISSiteName is empty — IIS stop/start will be skipped during updates.");

            if (string.IsNullOrWhiteSpace(options.Components.Server.HealthCheckUrl))
                logger.LogWarning("[StartupValidator] Components.Server.HealthCheckUrl is empty — post-deploy health check will be skipped.");

            if (string.IsNullOrWhiteSpace(options.Components.Server.NotificationBaseUrl))
                logger.LogWarning("[StartupValidator] Components.Server.NotificationBaseUrl is empty — browser clients won't be notified of maintenance.");
        }

        if (options.Components.Client.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Components.Client.DeployPath))
            {
                logger.LogError("[StartupValidator] Components.Client.Enabled=true but DeployPath is empty.");
                ok = false;
            }
            else
            {
                ok &= EnsureDirectory(options.Components.Client.DeployPath, "Components.Client.DeployPath", logger);
            }

            if (string.IsNullOrWhiteSpace(options.Components.Client.NotificationBaseUrl))
                logger.LogWarning("[StartupValidator] Components.Client.NotificationBaseUrl is empty — browsers won't be notified after client deployment.");
        }

        // ── Hub connection ────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(options.HubUrl))
            logger.LogWarning("[StartupValidator] HubUrl is empty — Agent will not connect to UpdateHub. Set the Hub SignalR endpoint.");

        if (string.IsNullOrWhiteSpace(options.InstallationName))
            logger.LogWarning("[StartupValidator] InstallationName is empty — the Agent will register without a human-readable name.");

        // ── Security ──────────────────────────────────────────────────────────
        if (options.UI.Password == "Admin#123!")
            logger.LogWarning("[StartupValidator] Agent UI password is still the default. Change it in Settings before going to production.");

        if (string.IsNullOrWhiteSpace(options.ApiKey) && string.IsNullOrWhiteSpace(options.EnrollmentToken))
            logger.LogWarning("[StartupValidator] Both ApiKey and EnrollmentToken are empty — Agent cannot authenticate with the Hub.");

        // ── IIS appcmd.exe presence check (only if Server component enabled) ──
        if (options.Components.Server.Enabled && !string.IsNullOrWhiteSpace(options.Components.Server.IISSiteName))
        {
            var appcmd = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"inetsrv\appcmd.exe");
            if (!File.Exists(appcmd))
                logger.LogWarning("[StartupValidator] appcmd.exe not found at {Path}. IIS management will fail during updates.", appcmd);
        }

        logger.LogInformation("[StartupValidator] Agent startup validation complete. Issues found: {Issues}",
            ok ? "none" : "see warnings above");

        return ok;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool EnsureDirectory(string path, string paramName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                logger.LogInformation("[StartupValidator] Created missing directory for {Param}: {Path}", paramName, path);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[StartupValidator] Cannot create directory for {Param}: {Path}", paramName, path);
            return false;
        }
    }
}
