using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Prym.ManagementHub.Services;

/// <summary>
/// Background service that monitors the <c>IncomingPackagesPath</c> folder for new
/// zip files produced by <c>New-UpdatePackage.ps1</c>.
///
/// On startup it also scans for any existing zips not yet recorded in the database.
///
/// When a valid package zip is found:
///   1. Reads manifest.json from inside the zip
///   2. Verifies the embedded checksum against the actual file
///   3. Moves the zip to <c>PackageStorePath</c>
///   4. Creates a <see cref="UpdatePackage"/> record with status <c>ReadyToDeploy</c>
/// </summary>
public class PackageWatcherService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PackageWatcherService> logger) : BackgroundService
{
    private string IncomingPath => configuration["ManagementHub:IncomingPackagesPath"] ?? "packages/incoming";
    private string StorePath => configuration["ManagementHub:PackageStorePath"] ?? "packages";

    // Debounce: wait this long after a file event before processing
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Directory.CreateDirectory(IncomingPath);
            Directory.CreateDirectory(StorePath);

            // ── Startup scan: pick up any zips dropped while Hub was offline ────────
            await ScanIncomingFolderAsync(stoppingToken);

            // ── FileSystemWatcher for ongoing monitoring ──────────────────────────
            using var watcher = new FileSystemWatcher(IncomingPath, "*.zip")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            var pendingFiles = new System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource>();

            watcher.Created += (_, e) => ScheduleIngestion(e.FullPath, pendingFiles, stoppingToken);
            watcher.Changed += (_, e) => ScheduleIngestion(e.FullPath, pendingFiles, stoppingToken);

            logger.LogInformation("PackageWatcherService watching: {Path}", Path.GetFullPath(IncomingPath));

            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            logger.LogInformation("PackageWatcherService stopped.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "PackageWatcherService encountered a fatal error and is stopping.");
        }
    }

    /// <summary>Debounced: schedule ingestion after a short delay to let the file finish copying.</summary>
    private void ScheduleIngestion(
        string filePath,
        System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> pending,
        CancellationToken stoppingToken)
    {
        // Cancel any existing debounce timer for this file
        if (pending.TryRemove(filePath, out var existingCts))
            existingCts.Cancel();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        pending[filePath] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cts.Token);
                pending.TryRemove(filePath, out _);
                await IngestFileAsync(filePath, stoppingToken);
            }
            catch (OperationCanceledException) { /* debounced — a newer event came in */ }
        }, cts.Token);
    }

    private async Task ScanIncomingFolderAsync(CancellationToken ct)
    {
        var zips = Directory.GetFiles(IncomingPath, "*.zip");
        if (zips.Length == 0) return;

        logger.LogInformation("Startup scan: found {Count} zip(s) in incoming folder.", zips.Length);
        foreach (var zip in zips)
        {
            if (ct.IsCancellationRequested) break;
            await IngestFileAsync(zip, ct);
        }
    }

    private async Task IngestFileAsync(string zipPath, CancellationToken ct)
    {
        if (!File.Exists(zipPath)) return;

        var fileName = Path.GetFileName(zipPath);
        logger.LogInformation("Ingesting package: {File}", fileName);

        try
        {
            // ── Read manifest ────────────────────────────────────────────────
            PackageManifestDto? manifest = null;
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                var manifestEntry = zip.GetEntry("manifest.json");
                if (manifestEntry is null)
                {
                    logger.LogWarning("Skipping {File}: no manifest.json found.", fileName);
                    return;
                }
                await using var stream = manifestEntry.Open();
                manifest = await JsonSerializer.DeserializeAsync<PackageManifestDto>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
            }

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.Component))
            {
                logger.LogWarning("Skipping {File}: manifest.json is invalid or missing required fields.", fileName);
                return;
            }

            if (!Enum.TryParse<PackageComponent>(manifest.Component, ignoreCase: true, out var component))
            {
                logger.LogWarning("Skipping {File}: unknown component '{Component}'.", fileName, manifest.Component);
                return;
            }

            // ── Verify checksum ──────────────────────────────────────────────
            string actualChecksum;
            await using (var stream = File.OpenRead(zipPath))
            {
                var hash = await SHA256.HashDataAsync(stream, ct);
                actualChecksum = Convert.ToHexStringLower(hash);
            }

            if (!string.IsNullOrWhiteSpace(manifest.Checksum) &&
                !string.Equals(manifest.Checksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(
                    "Checksum mismatch for {File}. Manifest: {Expected} | Actual: {Actual}. Package rejected.",
                    fileName, manifest.Checksum, actualChecksum);
                return;
            }

            // ── Check for duplicates ─────────────────────────────────────────
            using var scope = scopeFactory.CreateScope();
            var packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();

            if (await packageService.ExistsByChecksumAsync(actualChecksum, ct))
            {
                logger.LogInformation("Package {File} already in database (checksum match). Removing from incoming.", fileName);
                File.Delete(zipPath);
                return;
            }

            // ── Move zip to managed store ────────────────────────────────────
            var storedName = $"{component.ToString().ToLowerInvariant()}-{manifest.Version}-{Guid.NewGuid():N}.zip";
            var destPath = Path.Combine(StorePath, storedName);
            File.Move(zipPath, destPath, overwrite: true);

            // ── Persist record ───────────────────────────────────────────────
            var fileSize = new FileInfo(destPath).Length;
            var package = new UpdatePackage
            {
                Version = manifest.Version,
                Component = component,
                ReleaseNotes = manifest.ReleaseNotes,
                Checksum = actualChecksum,
                FilePath = storedName,
                FileSizeBytes = fileSize,
                GitCommit = manifest.GitCommit,
                Status = PackageStatus.ReadyToDeploy,
                UploadedBy = "watcher"
            };

            await packageService.CreateAsync(package, ct);
            logger.LogInformation(
                "Package ingested: {Component} {Version} ({Checksum}) — ReadyToDeploy",
                component, manifest.Version, actualChecksum[..8]);
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
        {
            // File still being written — the debounce will fire again on the next Changed event
            logger.LogDebug("File {File} is still being written. Will retry on next event.", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ingesting package {File}.", fileName);
        }
    }

    /// <summary>Minimal DTO for reading manifest.json from inside the zip.</summary>
    private sealed class PackageManifestDto
    {
        public string Version { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string? Checksum { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? GitCommit { get; set; }
    }
}
