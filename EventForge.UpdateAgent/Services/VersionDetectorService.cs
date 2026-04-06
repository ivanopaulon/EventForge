namespace EventForge.UpdateAgent.Services;

/// <summary>Detects the currently installed versions of Server and Client.</summary>
public class VersionDetectorService(AgentOptions options, ILogger<VersionDetectorService> logger)
{
    public string? GetServerVersion()
    {
        if (!options.Components.Server.Enabled) return null;
        try
        {
            // Look for version.txt written by the publish process
            var versionFile = Path.Combine(options.Components.Server.DeployPath, "version.txt");
            if (File.Exists(versionFile))
                return File.ReadAllText(versionFile).Trim();

            // Fallback: read from assembly in deploy path
            var exeFiles = Directory.GetFiles(options.Components.Server.DeployPath, "EventForge.Server.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length > 0)
            {
                var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(exeFiles[0]).FileVersion;
                return version;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect server version from {Path}", options.Components.Server.DeployPath);
        }
        return null;
    }

    public string? GetClientVersion()
    {
        if (!options.Components.Client.Enabled) return null;
        try
        {
            var versionFile = Path.Combine(options.Components.Client.DeployPath, "version.txt");
            if (File.Exists(versionFile))
                return File.ReadAllText(versionFile).Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect client version from {Path}", options.Components.Client.DeployPath);
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
