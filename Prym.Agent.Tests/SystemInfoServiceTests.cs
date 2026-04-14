using System.Net;
using Prym.Agent.Services;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="SystemInfoService"/> network properties.
/// <para>
/// NOTE: <see cref="SystemInfoService.GetPublicIpAddressAsync"/> is NOT tested here
/// because it requires an outbound HTTP call to ipify.org — a network dependency
/// that would make these tests brittle in CI/CD environments without internet access.
/// </para>
/// </summary>
public class SystemInfoServiceTests
{
    private readonly SystemInfoService _svc = new();

    // ── MachineName ──────────────────────────────────────────────────────────

    [Fact]
    public void MachineName_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_svc.MachineName));
    }

    // ── OSVersion ────────────────────────────────────────────────────────────

    [Fact]
    public void OSVersion_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_svc.OSVersion));
    }

    // ── DotNetVersion ────────────────────────────────────────────────────────

    [Fact]
    public void DotNetVersion_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_svc.DotNetVersion));
    }

    [Fact]
    public void DotNetVersion_ParsesAsVersion()
    {
        Assert.True(Version.TryParse(_svc.DotNetVersion, out _),
            $"DotNetVersion '{_svc.DotNetVersion}' is not a valid Version string.");
    }

    // ── LocalIpAddress ───────────────────────────────────────────────────────

    [Fact]
    public void LocalIpAddress_WhenNotNull_IsValidIPv4()
    {
        // LocalIpAddress can be null if no network is available (e.g. offline CI runner),
        // so we only assert the format when a value is actually returned.
        if (_svc.LocalIpAddress is null) return;

        Assert.True(
            IPAddress.TryParse(_svc.LocalIpAddress, out var addr) &&
            addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork,
            $"LocalIpAddress '{_svc.LocalIpAddress}' is not a valid IPv4 address.");
    }

    [Fact]
    public void LocalIpAddress_WhenNotNull_IsNotLoopback()
    {
        // The Socket.Connect trick should return the actual LAN IP, not 127.0.0.1,
        // unless the machine has no outbound interface at all.
        if (_svc.LocalIpAddress is null) return;
        if (!IPAddress.TryParse(_svc.LocalIpAddress, out var addr)) return;

        Assert.False(IPAddress.IsLoopback(addr),
            $"LocalIpAddress '{_svc.LocalIpAddress}' unexpectedly resolved to loopback.");
    }

    // ── GetPublicIpAddressAsync ──────────────────────────────────────────────

    [Fact]
    public void GetPublicIpAddressAsync_ReturnsTask()
    {
        // The service must expose a non-null Task — we don't await it to avoid
        // network dependencies in unit tests.
        Assert.NotNull(_svc.GetPublicIpAddressAsync());
    }
}
