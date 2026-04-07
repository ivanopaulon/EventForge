using System.Runtime.InteropServices;

namespace Prym.Agent.Services;

/// <summary>
/// Collects static system information once at startup and exposes it
/// for inclusion in Hub registration and enrollment payloads.
/// </summary>
public class SystemInfoService
{
    public string MachineName { get; } = GetMachineName();
    public string OSVersion { get; } = GetOSVersion();
    public string DotNetVersion { get; } = GetDotNetVersion();

    private static string GetMachineName()
    {
        try { return Environment.MachineName; }
        catch { return "UNKNOWN"; }
    }

    private static string GetOSVersion()
    {
        try
        {
            // On Windows returns e.g. "Microsoft Windows 11 Pro 10.0.22621"
            var desc = RuntimeInformation.OSDescription;
            var arch = RuntimeInformation.OSArchitecture;
            return $"{desc} ({arch})".Trim();
        }
        catch { return "Unknown OS"; }
    }

    private static string GetDotNetVersion()
    {
        try { return Environment.Version.ToString(); }
        catch { return "Unknown"; }
    }
}
