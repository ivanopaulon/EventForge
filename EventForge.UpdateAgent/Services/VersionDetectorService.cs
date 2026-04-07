namespace EventForge.UpdateAgent.Services;

/// <summary>Detects the currently installed versions of Server and Client.</summary>
public class VersionDetectorService(AgentOptions options, ILogger<VersionDetectorService> logger)
{
    public string? GetServerVersion()
    {
        if (!options.Components.Server.Enabled) return null;
        var deployPath = options.Components.Server.DeployPath;
        if (string.IsNullOrWhiteSpace(deployPath) || !Directory.Exists(deployPath))
        {
            logger.LogDebug("Server deploy path not found or empty: '{Path}'", deployPath);
            return null;
        }
        try
        {
            // Look for version.txt written by the publish process
            var versionFile = Path.Combine(deployPath, "version.txt");
            if (File.Exists(versionFile))
            {
                var v = File.ReadAllText(versionFile).Trim();
                if (!string.IsNullOrEmpty(v)) return v;
                logger.LogDebug("version.txt at {Path} is empty, falling back to assembly", versionFile);
            }

            // Fallback: read from assembly in deploy path
            var exeFiles = Directory.GetFiles(deployPath, "EventForge.Server.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length > 0)
            {
                var fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(exeFiles[0]).FileVersion;
                if (!string.IsNullOrWhiteSpace(fileVersion)) return fileVersion;
                logger.LogDebug("FileVersion is empty for {Exe}", exeFiles[0]);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect server version from {Path}", deployPath);
        }
        return null;
    }

    public string? GetClientVersion()
    {
        if (!options.Components.Client.Enabled) return null;
        var deployPath = options.Components.Client.DeployPath;
        if (string.IsNullOrWhiteSpace(deployPath) || !Directory.Exists(deployPath))
        {
            logger.LogDebug("Client deploy path not found or empty: '{Path}'", deployPath);
            return null;
        }
        try
        {
            var versionFile = Path.Combine(deployPath, "version.txt");
            if (File.Exists(versionFile))
            {
                var v = File.ReadAllText(versionFile).Trim();
                if (!string.IsNullOrEmpty(v)) return v;
                logger.LogDebug("version.txt at {Path} is empty", versionFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect client version from {Path}", deployPath);
        }
        return null;
    }

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
        catch { return "unknown"; }
    }
}
