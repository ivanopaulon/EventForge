using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prym.ManagementHub.Data.Entities;
using Prym.ManagementHub.Services;
using System.IO.Compression;
using System.Text.Json;

namespace Prym.ManagementHub.Tests;

/// <summary>
/// Tests for <see cref="PackageWatcherService"/>.
/// Verifies the file-handling behavior when <see cref="IPackageService.CreateAsync"/> fails.
/// </summary>
public sealed class PackageWatcherServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _incomingPath;
    private readonly string _storePath;
    private readonly Mock<IPackageService> _packageServiceMock;

    public PackageWatcherServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _incomingPath = Path.Combine(_tempRoot, "incoming");
        _storePath = Path.Combine(_tempRoot, "store");
        Directory.CreateDirectory(_incomingPath);
        Directory.CreateDirectory(_storePath);

        _packageServiceMock = new Mock<IPackageService>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    // ── CreateAsync failure → file moves to failed/ ──────────────────────────────────────────

    [Fact]
    public async Task IngestFile_WhenCreateAsyncFails_FileMovedToFailedFolder_NotOrphanInStore()
    {
        // Arrange: create a valid package zip in incoming.
        var zipPath = CreateValidPackageZip("1.0.0", "Server");

        _packageServiceMock
            .Setup(x => x.ExistsByChecksumAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _packageServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<UpdatePackage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DB constraint violation"));

        var svc = BuildService();

        // Act: trigger ingestion of the zip.
        await svc.IngestFileForTestAsync(zipPath, CancellationToken.None);

        // Assert: file is NOT in PackageStorePath root (was moved on to failed/).
        var storeFiles = Directory.GetFiles(_storePath, "*.zip", SearchOption.TopDirectoryOnly);
        Assert.Empty(storeFiles);

        // Assert: file IS in the failed/ subdirectory.
        var failedDir = Path.Combine(_incomingPath, "failed");
        Assert.True(Directory.Exists(failedDir), "failed/ directory should have been created.");
        var failedFiles = Directory.GetFiles(failedDir, "*.zip");
        Assert.Single(failedFiles);
    }

    [Fact]
    public async Task IngestFile_WhenCreateAsyncSucceeds_FileInStore_NoFailedFolder()
    {
        // Arrange: create a valid package zip in incoming.
        var zipPath = CreateValidPackageZip("2.0.0", "Client");

        _packageServiceMock
            .Setup(x => x.ExistsByChecksumAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _packageServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<UpdatePackage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdatePackage p, CancellationToken _) => p);

        var svc = BuildService();

        await svc.IngestFileForTestAsync(zipPath, CancellationToken.None);

        // File should be in the store, not in failed/.
        var storeFiles = Directory.GetFiles(_storePath, "*.zip", SearchOption.TopDirectoryOnly);
        Assert.Single(storeFiles);

        var failedDir = Path.Combine(_incomingPath, "failed");
        Assert.False(Directory.Exists(failedDir) && Directory.GetFiles(failedDir, "*.zip").Length > 0,
            "No files should be in the failed/ folder on success.");
    }

    [Fact]
    public async Task IngestFile_DuplicateChecksum_FileDeletedFromIncoming_NothingInStore()
    {
        var zipPath = CreateValidPackageZip("3.0.0", "Agent");

        _packageServiceMock
            .Setup(x => x.ExistsByChecksumAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // already exists

        var svc = BuildService();

        await svc.IngestFileForTestAsync(zipPath, CancellationToken.None);

        Assert.False(File.Exists(zipPath), "Incoming duplicate should be deleted.");
        var storeFiles = Directory.GetFiles(_storePath, "*.zip", SearchOption.TopDirectoryOnly);
        Assert.Empty(storeFiles);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private string CreateValidPackageZip(string version, string component)
    {
        var fileName = $"test-{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(_incomingPath, fileName);

        var manifest = new
        {
            Version = version,
            Component = component,
            Checksum = (string?)null,
            ReleaseNotes = "Test",
            GitCommit = (string?)null
        };

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("manifest.json");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(JsonSerializer.Serialize(manifest));
        }

        return zipPath;
    }

    private TestablePackageWatcherService BuildService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ManagementHub:IncomingPackagesPath"] = _incomingPath,
                ["ManagementHub:PackageStorePath"] = _storePath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddScoped<IPackageService>(_ => _packageServiceMock.Object);

        return new TestablePackageWatcherService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<PackageWatcherService>.Instance);
    }
}

/// <summary>
/// Exposes <c>IngestFileAsync</c> as a public method for testing.
/// </summary>
internal sealed class TestablePackageWatcherService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PackageWatcherService> logger)
    : PackageWatcherService(scopeFactory, configuration, logger)
{
    public Task IngestFileForTestAsync(string zipPath, CancellationToken ct)
        => IngestFileAsync(zipPath, ct);
}
