using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Prym.Agent.Services;

/// <summary>
/// Collects static system information once at startup and exposes it
/// for inclusion in Hub registration and enrollment payloads.
/// <para>
/// <see cref="LocalIpAddress"/> is resolved synchronously at construction using
/// a UDP socket trick — no real traffic is sent.
/// </para>
/// <para>
/// <see cref="GetPublicIpAddressAsync"/> resolves the public (WAN) IP once from
/// <c>https://api.ipify.org</c> and caches the result for the lifetime of the
/// service. The task is started in the constructor so resolution is in-flight
/// as early as possible.
/// </para>
/// </summary>
public class SystemInfoService
{
    public string MachineName    { get; } = GetMachineName();
    public string OSVersion      { get; } = GetOSVersion();
    public string DotNetVersion  { get; } = GetDotNetVersion();

    /// <summary>
    /// The agent machine's LAN IP address (IPv4), resolved by asking the OS
    /// which local interface it would use to reach 8.8.8.8. No real traffic is sent.
    /// Returns <see langword="null"/> if the socket trick fails (e.g. no network).
    /// </summary>
    public string? LocalIpAddress { get; } = GetLocalIpAddress();

    // Public IP is resolved asynchronously and cached for the service lifetime.
    private readonly Task<string?> _publicIpTask;

    public SystemInfoService()
    {
        _publicIpTask = FetchPublicIpAsync();
    }

    /// <summary>
    /// Returns the public (WAN) IP address as reported by <c>https://api.ipify.org</c>.
    /// The task is started at construction; the first <c>await</c> will complete as soon
    /// as the external lookup finishes (typically &lt;1 s). Subsequent awaits return the
    /// cached result immediately.
    /// Returns <see langword="null"/> on any network error or timeout.
    /// </summary>
    public Task<string?> GetPublicIpAddressAsync() => _publicIpTask;

    // ── Private helpers ──────────────────────────────────────────────────────

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

    /// <summary>
    /// Retrieves the local IPv4 address by opening a UDP socket toward 8.8.8.8:65530.
    /// The OS fills in the source endpoint without sending any real traffic.
    /// This avoids the pitfall of <c>Dns.GetHostEntry</c> returning IPv6 or a VPN adapter.
    /// </summary>
    private static string? GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Fetches the public IP from ipify with a 5-second timeout.</summary>
    private static async Task<string?> FetchPublicIpAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetStringAsync("https://api.ipify.org").ConfigureAwait(false);
            var ip = response.Trim();
            return string.IsNullOrEmpty(ip) ? null : ip;
        }
        catch
        {
            return null;
        }
    }
}
