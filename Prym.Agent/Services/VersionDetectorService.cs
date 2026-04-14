namespace Prym.Agent.Services;

/// <summary>Detects the currently installed versions of Server and Client.</summary>
public class VersionDetectorService(AgentOptions options, ILogger<VersionDetectorService> logger)
{
    // ── Public API ────────────────────────────────────────────────────────────

    public Task<string?> GetServerVersionAsync() =>
        ReadComponentVersionAsync(
            options.Components.Server.Enabled,
            options.Components.Server.DeployPath,
            serverExeFallback: true);

    public Task<string?> GetClientVersionAsync() =>
        ReadComponentVersionAsync(
            options.Components.Client.Enabled,
            options.Components.Client.DeployPath,
            serverExeFallback: false);

    /// <summary>Returns the version of the running UpdateAgent process itself.</summary>
    public string GetAgentVersion()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var infoAttr = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(asm);
            return infoAttr?.InformationalVersion
                   ?? asm.GetName().Version?.ToString()
                   ?? "unknown";
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not determine agent version from assembly.");
            return "unknown";
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads the installed version for a component:
    ///   1. Returns <see langword="null"/> immediately when the component is disabled or its deploy
    ///      path is absent.
    ///   2. Reads <c>version.txt</c> from the deploy directory (preferred).
    ///   3. Falls back to <c>FileVersionInfo</c> on <c>EventForge.Server.exe</c> when
    ///      <paramref name="serverExeFallback"/> is <see langword="true"/>.
    /// </summary>
    private async Task<string?> ReadComponentVersionAsync(
        bool enabled, string? deployPath, bool serverExeFallback)
    {
        if (!enabled) return null;

        if (string.IsNullOrWhiteSpace(deployPath) || !Directory.Exists(deployPath))
        {
            logger.LogDebug("Deploy path not found or empty: '{Path}'", deployPath);
            return null;
        }

        try
        {
            var versionFile = Path.Combine(deployPath, "version.txt");
            if (File.Exists(versionFile))
            {
                var v = (await File.ReadAllTextAsync(versionFile)).Trim();
                if (!string.IsNullOrEmpty(v)) return v;
                logger.LogWarning(
                    "version.txt at '{Path}' exists but is empty{Fallback}.",
                    versionFile,
                    serverExeFallback ? " — falling back to assembly FileVersionInfo" : string.Empty);
            }

            if (serverExeFallback)
            {
                var exeFiles = Directory.GetFiles(deployPath, "EventForge.Server.exe",
                    SearchOption.TopDirectoryOnly);
                if (exeFiles.Length > 0)
                {
                    var fileVersion = System.Diagnostics.FileVersionInfo
                        .GetVersionInfo(exeFiles[0]).FileVersion;
                    if (!string.IsNullOrWhiteSpace(fileVersion)) return fileVersion;
                    logger.LogDebug("FileVersion is empty for {Exe}", exeFiles[0]);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect version from {Path}", deployPath);
        }

        return null;
    }
}
