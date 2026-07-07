using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prym.ManagementHub.Configuration;
using Prym.ManagementHub.Data;
using Prym.ManagementHub.Data.Entities;
using Prym.ManagementHub.Services;

namespace Prym.ManagementHub.Tests;

/// <summary>
/// Tests for <see cref="OrphanedUpdateReconciliationService"/>.
/// Uses SQLite in-memory (shared-cache, keep-alive connection) so that EF Core's
/// <c>ExecuteUpdateAsync</c> / <c>ExecuteDeleteAsync</c> bulk operations work correctly.
/// </summary>
public sealed class OrphanedUpdateReconciliationServiceTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly ManagementHubDbContext _db;
    private readonly UpdateThrottleService _throttle;
    private readonly Mock<IPackageService> _packageServiceMock;
    private readonly InstallationService _installationService;
    private readonly ManagementHubOptions _options;

    public OrphanedUpdateReconciliationServiceTests()
    {
        // Keep the in-memory SQLite database alive for the duration of the test.
        _keepAlive = new SqliteConnection("Data Source=:memory:");
        _keepAlive.Open();

        var dbOptions = new DbContextOptionsBuilder<ManagementHubDbContext>()
            .UseSqlite(_keepAlive)
            .Options;

        _db = new ManagementHubDbContext(dbOptions);
        _db.Database.EnsureCreated();

        var connectionTrackerMock = new Mock<IConnectionTracker>();
        connectionTrackerMock.Setup(x => x.GetOnlineInstallationIds()).Returns([]);

        _installationService = new InstallationService(_db, connectionTrackerMock.Object);
        _packageServiceMock = new Mock<IPackageService>();

        _options = new ManagementHubOptions
        {
            OrphanedUpdateGraceMinutes = 15,
            OrphanedUpdateCheckIntervalSeconds = 300
        };

        _throttle = new UpdateThrottleService(_options);
        // Fill the throttle slot so we can verify Release is called.
        _throttle.AcquireAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _db.Dispose();
        _keepAlive.Dispose();
        _throttle.Dispose();
    }

    // ── Case (a): orphaned record — Offline + older than grace → Failed + throttle released ──

    [Fact]
    public async Task OrphanedHistory_OfflineInstallation_BeyondGrace_IsMarkedFailed_ThrottleReleased()
    {
        var (installation, package, history) = await SeedOrphanedHistoryAsync(
            status: InstallationStatus.Offline,
            startedAt: DateTime.UtcNow.AddMinutes(-30), // well beyond the 15-min grace
            phase: "Extracting");

        _packageServiceMock.Setup(x => x.GetByIdAsync(package.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        await RunReconciliationAsync();

        await _db.Entry(history).ReloadAsync();
        Assert.Equal(UpdateHistoryStatus.Failed, history.Status);
        Assert.NotNull(history.CompletedAt);

        // Throttle slot should now be re-acquirable (released during reconciliation).
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await _throttle.AcquireAsync(cts.Token); // would block if slot was not released
    }

    // ── Case (b): AwaitingMaintenanceWindow phase is never touched ──────────────────────────

    [Fact]
    public async Task InProgress_AwaitingMaintenanceWindow_NeverTouched_EvenBeyondGrace()
    {
        var (_, _, history) = await SeedOrphanedHistoryAsync(
            status: InstallationStatus.Offline,
            startedAt: DateTime.UtcNow.AddDays(-2), // very old
            phase: "AwaitingMaintenanceWindow");

        await RunReconciliationAsync();

        await _db.Entry(history).ReloadAsync();
        // Must remain InProgress — this is a legitimate long-running wait.
        Assert.Equal(UpdateHistoryStatus.InProgress, history.Status);
        Assert.Equal("AwaitingMaintenanceWindow", history.PhaseDescription);
    }

    // ── Case (c): Online installation — not touched ──────────────────────────────────────────

    [Fact]
    public async Task InProgress_OnlineInstallation_NeverTouched()
    {
        var (_, _, history) = await SeedOrphanedHistoryAsync(
            status: InstallationStatus.Online,
            startedAt: DateTime.UtcNow.AddMinutes(-30),
            phase: "Installing");

        await RunReconciliationAsync();

        await _db.Entry(history).ReloadAsync();
        Assert.Equal(UpdateHistoryStatus.InProgress, history.Status);
    }

    // ── Case (d): within grace period — not touched ──────────────────────────────────────────

    [Fact]
    public async Task InProgress_OfflineInstallation_WithinGrace_NeverTouched()
    {
        var (_, _, history) = await SeedOrphanedHistoryAsync(
            status: InstallationStatus.Offline,
            startedAt: DateTime.UtcNow.AddMinutes(-5), // within the 15-min grace
            phase: "Downloading");

        await RunReconciliationAsync();

        await _db.Entry(history).ReloadAsync();
        Assert.Equal(UpdateHistoryStatus.InProgress, history.Status);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private async Task<(Installation Installation, UpdatePackage Package, UpdateHistory History)>
        SeedOrphanedHistoryAsync(InstallationStatus status, DateTime startedAt, string phase)
    {
        var installation = new Installation
        {
            Name = "Test Install",
            ApiKey = Guid.NewGuid().ToString("N"),
            Status = status
        };
        _db.Installations.Add(installation);

        var package = new UpdatePackage
        {
            Version = "1.0.0",
            Component = PackageComponent.Server,
            Checksum = Guid.NewGuid().ToString("N"),
            FilePath = "test.zip",
            Status = PackageStatus.Deploying
        };
        _db.UpdatePackages.Add(package);

        var history = new UpdateHistory
        {
            InstallationId = installation.Id,
            PackageId = package.Id,
            Status = UpdateHistoryStatus.InProgress,
            StartedAt = startedAt,
            PhaseDescription = phase
        };
        _db.UpdateHistories.Add(history);

        await _db.SaveChangesAsync();
        return (installation, package, history);
    }

    private async Task RunReconciliationAsync()
    {
        var scopeFactory = BuildScopeFactory();
        var svc = new OrphanedUpdateReconciliationService(
            scopeFactory,
            _throttle,
            _options,
            NullLogger<OrphanedUpdateReconciliationService>.Instance);

        await svc.ReconcileAsync(CancellationToken.None);
    }

    private IServiceScopeFactory BuildScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddSingleton<IConnectionTracker>(_ =>
        {
            var m = new Mock<IConnectionTracker>();
            m.Setup(x => x.GetOnlineInstallationIds()).Returns([]);
            return m.Object;
        });
        services.AddScoped<IInstallationService>(_ => _installationService);
        services.AddScoped<IPackageService>(_ => _packageServiceMock.Object);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}
