using Microsoft.Extensions.Logging.Abstractions;
using Prym.Agent.Configuration;
using Prym.Agent.Services;
using Prym.DTOs.Agent;
using static Prym.Agent.Configuration.AgentOptions;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="PendingInstallService"/>.
/// Covers FIFO ordering, block/unblock, and server-before-client priority.
/// Each test uses a fresh in-memory service instance with no disk persistence.
/// </summary>
public class PendingInstallServiceTests : IDisposable
{
    // Use a temp directory so pending.json is written somewhere safe (and cleaned up)
    private readonly string _baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly PendingInstallService _svc;

    public PendingInstallServiceTests()
    {
        Directory.CreateDirectory(_baseDir);

        // Point AppContext.BaseDirectory override via AppDomain data — not practical here.
        // Instead we verify behaviour purely via the public API (queue logic is in-memory).
        _svc = BuildService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_baseDir))
            Directory.Delete(_baseDir, recursive: true);
    }

    // ── FIFO ordering ───────────────────────────────────────────────────────

    [Fact]
    public void Enqueue_GetNext_ReturnsFIFO()
    {
        var id1 = Enqueue(_svc, "Server", "1.0.0");
        var id2 = Enqueue(_svc, "Server", "1.1.0");

        var next = _svc.GetNext();
        Assert.NotNull(next);
        Assert.Equal(id1, next.PackageId);
    }

    [Fact]
    public void GetAll_ReturnsAllQueuedEntries()
    {
        Enqueue(_svc, "Server", "1.0.0");
        Enqueue(_svc, "Client", "1.0.0");

        var all = _svc.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Remove_ReducesQueue()
    {
        var id = Enqueue(_svc, "Server", "1.0.0");
        Enqueue(_svc, "Server", "1.1.0");

        _svc.Remove(id);

        var all = _svc.GetAll();
        Assert.Single(all);
        Assert.DoesNotContain(all, e => e.PackageId == id);
    }

    [Fact]
    public void GetNext_EmptyQueue_ReturnsNull()
    {
        Assert.Null(_svc.GetNext());
    }

    // ── Block / Unblock ─────────────────────────────────────────────────────

    [Fact]
    public void Block_SetsIsBlocked_GetNextReturnsNull()
    {
        var id = Enqueue(_svc, "Server", "1.0.0");

        _svc.Block(id, "test failure");

        Assert.True(_svc.IsBlocked);
        Assert.Equal(id, _svc.BlockedByPackageId);
        Assert.Null(_svc.GetNext());
    }

    [Fact]
    public void Unblock_WithoutSkip_ClearsBlockButKeepsEntry()
    {
        var id = Enqueue(_svc, "Server", "1.0.0");
        _svc.Block(id, "reason");

        _svc.Unblock(skipAndRemove: false);

        Assert.False(_svc.IsBlocked);
        // Entry is still in queue
        var all = _svc.GetAll();
        Assert.Contains(all, e => e.PackageId == id);
        Assert.NotNull(_svc.GetNext());
    }

    [Fact]
    public void Unblock_WithSkip_RemovesFailedEntry()
    {
        var id = Enqueue(_svc, "Server", "1.0.0");
        Enqueue(_svc, "Server", "1.1.0");
        _svc.Block(id, "failure");

        _svc.Unblock(skipAndRemove: true);

        Assert.False(_svc.IsBlocked);
        Assert.DoesNotContain(_svc.GetAll(), e => e.PackageId == id);
        // Second entry is now head
        var next = _svc.GetNext();
        Assert.NotNull(next);
    }

    [Fact]
    public void Block_NonExistentPackage_StillSetsIsBlocked()
    {
        // Block() always sets IsBlocked=true regardless of whether the package exists in queue.
        // It returns false (not downgraded to manual) when the package is not found.
        var result = _svc.Block(Guid.NewGuid(), "reason");
        Assert.False(result);   // no manual downgrade (package not found)
        Assert.True(_svc.IsBlocked); // queue is still blocked
    }

    [Fact]
    public void Block_ReturnsTrue_WhenPackageReachesMaxRetries()
    {
        // MaxAutoRetries = 3 → third failure should downgrade to manual (return true)
        var options = new AgentOptions { Install = new InstallOptions { MaxAutoRetries = 3 } };
        var svc = new PendingInstallService(options, NullLogger<PendingInstallService>.Instance);

        var id = Enqueue(svc, "Server", "1.0.0");

        // Fail twice (no downgrade yet)
        var r1 = svc.Block(id, "fail 1"); svc.Unblock(skipAndRemove: false);
        var r2 = svc.Block(id, "fail 2"); svc.Unblock(skipAndRemove: false);
        // Third failure → downgrade
        var r3 = svc.Block(id, "fail 3");

        Assert.False(r1);
        Assert.False(r2);
        Assert.True(r3); // downgraded to manual
    }

    // ── Server-before-Client priority ───────────────────────────────────────

    [Fact]
    public void GetNext_ClientHead_ServerSameVersion_ReturnsServerFirst()
    {
        // Client arrives first and gets lower QueuePosition, but Server for same version
        // should be installed first
        var clientId = Enqueue(_svc, "Client", "2.0.0");
        var serverId = Enqueue(_svc, "Server", "2.0.0");

        // Create a fake zip file for the server entry so File.Exists returns true
        var serverEntry = _svc.GetByPackageId(serverId)!;
        File.WriteAllText(serverEntry.LocalZipPath, "fake");

        var next = _svc.GetNext();

        Assert.NotNull(next);
        Assert.Equal(serverId, next.PackageId);

        // Cleanup fake file
        File.Delete(serverEntry.LocalZipPath);
    }

    [Fact]
    public void GetNext_ClientHead_ServerDifferentVersion_ReturnsClientFirst()
    {
        // Different versions → no server-first rule applies
        var clientId = Enqueue(_svc, "Client", "2.0.0");
        Enqueue(_svc, "Server", "3.0.0");

        var next = _svc.GetNext();

        Assert.NotNull(next);
        Assert.Equal(clientId, next.PackageId);
    }

    // ── GetByPackageId ───────────────────────────────────────────────────────

    [Fact]
    public void GetByPackageId_ExistingEntry_ReturnsIt()
    {
        var id = Enqueue(_svc, "Server", "1.0.0");
        var found = _svc.GetByPackageId(id);
        Assert.NotNull(found);
        Assert.Equal(id, found.PackageId);
    }

    [Fact]
    public void GetByPackageId_UnknownId_ReturnsNull()
    {
        Assert.Null(_svc.GetByPackageId(Guid.NewGuid()));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PendingInstallService BuildService()
    {
        var options = new AgentOptions
        {
            Install = new InstallOptions
            {
                MaxAutoRetries = 3
            }
        };
        var logger = NullLogger<PendingInstallService>.Instance;
        return new PendingInstallService(options, logger);
    }

    private static Guid Enqueue(PendingInstallService svc, string component, string version)
    {
        var packageId = Guid.NewGuid();
        var zipPath = Path.Combine(Path.GetTempPath(), $"{packageId}.zip");

        svc.Enqueue(new StartUpdateCommand(
            UpdateHistoryId: Guid.NewGuid(),
            PackageId: packageId,
            Version: version,
            Component: component,
            DownloadUrl: "https://example.com/pkg.zip",
            Checksum: $"sha256-{packageId:N}",
            IsManualInstall: false),
            zipPath);

        return packageId;
    }
}
