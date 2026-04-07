using EventForge.UpdateHub.Configuration;

namespace EventForge.UpdateHub.Services;

/// <summary>
/// Validates the Hub configuration at startup: ensures required directories exist
/// (creating them if absent) and logs a diagnostic warning for each anomaly found.
/// Call <see cref="Run"/> once, immediately after <see cref="UpdateHubOptions"/> is bound.
/// </summary>
public static class StartupValidator
{
    /// <summary>
    /// Runs all startup checks. Returns <c>true</c> if no critical issues were found.
    /// </summary>
    public static bool Run(UpdateHubOptions options, ILogger logger)
    {
        var ok = true;

        // ── Required directories ──────────────────────────────────────────────
        ok &= EnsureDirectory(options.PackageStorePath,    "PackageStorePath",    logger, required: true);
        ok &= EnsureDirectory(options.IncomingPackagesPath,"IncomingPackagesPath",logger, required: true);

        if (!string.IsNullOrWhiteSpace(options.Logging.DirectoryPath))
            ok &= EnsureDirectory(options.Logging.DirectoryPath, "Logging.DirectoryPath", logger, required: false);

        // ── Optional deploy-source hints (UI defaults) ────────────────────────
        WarnIfMissing(options.DefaultServerDeployPath, "DefaultServerDeployPath", logger);
        WarnIfMissing(options.DefaultClientDeployPath, "DefaultClientDeployPath", logger);

        // ── Security warnings ─────────────────────────────────────────────────
        if (options.UI.Password == "Admin#123!")
            logger.LogWarning("[StartupValidator] Hub UI password is still the default. Change it in Settings before going to production.");

        if (string.IsNullOrWhiteSpace(options.AdminApiKey))
            logger.LogWarning("[StartupValidator] AdminApiKey is empty — admin REST endpoints are disabled.");

        if (options.AllowAutoEnrollment && string.IsNullOrWhiteSpace(options.EnrollmentToken))
            logger.LogWarning("[StartupValidator] AllowAutoEnrollment=true but EnrollmentToken is empty; auto-enrollment is effectively disabled.");

        // ── Upload size sanity ────────────────────────────────────────────────
        if (options.MaxUploadSizeMb <= 0)
            logger.LogWarning("[StartupValidator] MaxUploadSizeMb is {Value} — uploads will be rejected. Set a positive value.", options.MaxUploadSizeMb);

        logger.LogInformation("[StartupValidator] Hub startup validation complete. Issues found: {Issues}",
            ok ? "none" : "see warnings above");

        return ok;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool EnsureDirectory(string? path, string paramName, ILogger logger, bool required)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (required)
            {
                logger.LogWarning("[StartupValidator] {Param} is empty; using default.", paramName);
            }
            return true; // empty path resolved elsewhere (e.g. AppContext.BaseDirectory fallback)
        }

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

    private static void WarnIfMissing(string? path, string paramName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!Directory.Exists(path))
            logger.LogWarning("[StartupValidator] {Param} path does not exist: {Path}. Create it or update the configuration.", paramName, path);
    }
}
