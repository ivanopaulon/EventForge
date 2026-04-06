using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Orchestrates the full update sequence: Download → Verify → Backup → Stop → Migrate → Deploy → Start → Verify → Report.
///
/// The sequence is split into two independently callable phases:
///   1. <see cref="DownloadAndVerifyAsync"/> — resilient download + checksum gate (runs immediately on command).
///   2. <see cref="InstallFromZipAsync"/>    — everything from Extract onward (deferred to a maintenance window).
/// </summary>
public class UpdateExecutorService(
    AgentOptions options,
    BackupService backupService,
    IisManagerService iisManagerService,
    MigrationRunnerService migrationRunner,
    DownloadProgressService downloadProgress,
    ILogger<UpdateExecutorService> logger)
{
    // Single long-lived HttpClient; per-request timeout is enforced via CancellationTokenSource.
    private readonly HttpClient _http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(30)
    })
    { Timeout = Timeout.InfiniteTimeSpan };

    /// <summary>Fired after each phase to push progress to the Hub via SignalR.</summary>
    public event Func<UpdateProgressMessage, Task>? OnProgress;

    // Exponential backoff delays (seconds) between successive download attempts.
    private static readonly int[] DownloadDelays = [5, 15, 30, 60, 120];

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 1 + 2: resilient download with retry/resume, followed by SHA-256 checksum verification.
    /// Returns the local path to the verified zip file.
    /// </summary>
    public async Task<string> DownloadAndVerifyAsync(StartUpdateCommand command, CancellationToken ct)
    {
        await ReportAsync(command, UpdatePhase.Downloading, false, true, null, ct);
        var zipPath = await DownloadPackageAsync(command.DownloadUrl, command.PackageId.ToString("N"), command.Component, command.Version, ct);

        await ReportAsync(command, UpdatePhase.VerifyingChecksum, false, true, null, ct);
        await VerifyChecksumAsync(zipPath, command.Checksum, ct);

        return zipPath;
    }

    /// <summary>
    /// Phase 3–11: extract, backup, migrate, stop IIS, deploy, start IIS, post-migrate, health-check.
    /// Performs rollback on failure. The <paramref name="zipPath"/> is deleted in the finally block.
    /// </summary>
    public async Task InstallFromZipAsync(StartUpdateCommand command, string zipPath, CancellationToken ct)
    {
        var isServer = command.Component.Equals("Server", StringComparison.OrdinalIgnoreCase);
        var deployPath = isServer ? options.Components.Server.DeployPath : options.Components.Client.DeployPath;
        var tempDir = Path.Combine(AppContext.BaseDirectory, "updates", $"ef-update-{command.PackageId:N}");
        string? backupPath = null;

        try
        {
            // ── Phase 3: Extract ──
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
            var manifest = await LoadManifestAsync(tempDir);

            // ── Phase 4: Backup ──
            await ReportAsync(command, UpdatePhase.BackingUp, false, true, null, ct);
            backupPath = await backupService.CreateBackupAsync(deployPath, command.Component, command.Version, ct);

            // ── Phase 5: Pre-migrations ──
            if (isServer && manifest.PreMigrationScripts.Count > 0)
            {
                await ReportAsync(command, UpdatePhase.RunningPreMigrations, false, true, null, ct);
                await migrationRunner.RunScriptsAsync(tempDir, manifest.PreMigrationScripts, ct);
            }

            // ── Phase 6: Stop IIS ──
            if (isServer)
            {
                await ReportAsync(command, UpdatePhase.StoppingService, false, true, null, ct);
                await iisManagerService.StopSiteAsync(ct);
            }

            // ── Phase 7: Deploy ──
            await ReportAsync(command, UpdatePhase.DeployingBinaries, false, true, null, ct);
            await DeployBinariesAsync(tempDir, deployPath, manifest, ct);

            // ── Phase 8: Write version.txt ──
            await File.WriteAllTextAsync(Path.Combine(deployPath, "version.txt"), command.Version, ct);

            // ── Phase 9: Start IIS ──
            if (isServer)
            {
                await ReportAsync(command, UpdatePhase.StartingService, false, true, null, ct);
                await iisManagerService.StartSiteAsync(ct);
            }

            // ── Phase 10: Post-migrations ──
            if (isServer && manifest.PostMigrationScripts.Count > 0)
            {
                await ReportAsync(command, UpdatePhase.RunningPostMigrations, false, true, null, ct);
                await migrationRunner.RunScriptsAsync(tempDir, manifest.PostMigrationScripts, ct);
            }

            // ── Phase 11: Health check ──
            if (isServer)
            {
                await ReportAsync(command, UpdatePhase.VerifyingHealth, false, true, null, ct);
                await VerifyHealthAsync(options.Components.Server.HealthCheckUrl, ct);
            }

            // ── Complete ──
            await ReportAsync(command, UpdatePhase.Completed, true, true, null, ct);
            logger.LogInformation("Update completed successfully: {Component} {Version}", command.Component, command.Version);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update failed: component={Component} version={Version}", command.Component, command.Version);

            var rollbackSucceeded = false;
            if (backupPath is not null)
            {
                try
                {
                    await ReportAsync(command, UpdatePhase.Rollback, false, false, ex.Message, CancellationToken.None);
                    if (isServer) await iisManagerService.StopSiteAsync(CancellationToken.None);
                    await backupService.RestoreBackupAsync(backupPath, deployPath, CancellationToken.None);
                    if (isServer)
                    {
                        await iisManagerService.StartSiteAsync(CancellationToken.None);
                        if (Directory.Exists(tempDir))
                        {
                            var manifest2 = await LoadManifestAsync(tempDir);
                            if (manifest2.RollbackScripts.Count > 0)
                                await migrationRunner.RunScriptsAsync(tempDir, manifest2.RollbackScripts, CancellationToken.None);
                        }
                    }
                    rollbackSucceeded = true;
                }
                catch (Exception rollbackEx)
                {
                    logger.LogError(rollbackEx, "Rollback also failed!");
                }
            }

            var finalPhase = rollbackSucceeded ? UpdatePhase.Rollback : UpdatePhase.Completed;
            await ReportAsync(command, finalPhase, true, false, ex.Message, CancellationToken.None);

            // Re-throw so the caller (AgentWorker / ScheduledInstallWorker) can block the queue.
            throw;
        }
        finally
        {
            if (zipPath is not null && File.Exists(zipPath))
                try { File.Delete(zipPath); } catch { /* best effort */ }
            if (Directory.Exists(tempDir))
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    // ── Download (resilient) ─────────────────────────────────────────────────

    private async Task<string> DownloadPackageAsync(string downloadUrl, string packageIdHex, string component, string version, CancellationToken ct)
    {
        var packageId = Guid.TryParseExact(packageIdHex, "N", out var g) ? g : Guid.NewGuid();

        var downloadDir = Path.Combine(AppContext.BaseDirectory, "updates");
        Directory.CreateDirectory(downloadDir);

        var zipPath = Path.Combine(downloadDir, $"ef-pkg-{packageIdHex}.zip");
        var tmpPath = zipPath + ".tmp";

        var maxAttempts = options.DownloadMaxRetries + 1;

        downloadProgress.Start(packageId, component, version);
        try
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    var delaySec = DownloadDelays[Math.Min(attempt - 1, DownloadDelays.Length - 1)];
                    logger.LogWarning("Download retry {Attempt}/{Max} in {Delay}s...", attempt, maxAttempts - 1, delaySec);
                    await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
                }

                try
                {
                    long resumeFrom = File.Exists(tmpPath) ? new FileInfo(tmpPath).Length : 0;

                    using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                    request.Headers.Add("X-Api-Key", options.ApiKey);
                    if (resumeFrom > 0)
                        request.Headers.Range = new RangeHeaderValue(resumeFrom, null);

                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(options.DownloadTimeoutMinutes));
                    using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);

                    if (resumeFrom > 0 && response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                    {
                        logger.LogWarning("Server returned 416 — restarting download from byte 0.");
                        try { File.Delete(tmpPath); } catch { /* best effort */ }
                        resumeFrom = 0;

                        using var request2 = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                        request2.Headers.Add("X-Api-Key", options.ApiKey);
                        using var response2 = await _http.SendAsync(request2, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                        response2.EnsureSuccessStatusCode();
                        await WriteResponseToFileAsync(response2, tmpPath, append: false, packageId, linkedCts.Token);
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        await WriteResponseToFileAsync(response, tmpPath, append: resumeFrom > 0, packageId, linkedCts.Token);
                    }

                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    File.Move(tmpPath, zipPath);

                    logger.LogInformation("Package downloaded to {Path}", zipPath);
                    return zipPath;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < maxAttempts - 1)
                {
                    logger.LogWarning(ex, "Download attempt {Attempt}/{Max} failed", attempt + 1, maxAttempts);
                }
            }

            throw new InvalidOperationException($"Package download failed after {maxAttempts} attempt(s). URL: {downloadUrl}");
        }
        finally
        {
            downloadProgress.Complete(packageId);
        }
    }

    private async Task WriteResponseToFileAsync(
        HttpResponseMessage response, string tmpPath, bool append, Guid packageId, CancellationToken ct)
    {
        var totalBytes = response.Content.Headers.ContentLength;
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);

        var fileMode = append ? FileMode.Append : FileMode.Create;
        await using var fileStream = new FileStream(tmpPath, fileMode, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer    = new byte[81920];
        long written  = append && File.Exists(tmpPath) ? new FileInfo(tmpPath).Length : 0;
        var lastReport = DateTime.UtcNow;
        int bytesRead;

        while ((bytesRead = await responseStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            written += bytesRead;

            if ((DateTime.UtcNow - lastReport).TotalSeconds >= 1)
            {
                downloadProgress.Update(packageId, written, totalBytes);
                lastReport = DateTime.UtcNow;
            }
        }

        // Final update
        downloadProgress.Update(packageId, written, totalBytes);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task VerifyChecksumAsync(string zipPath, string expectedChecksum, CancellationToken ct)
    {
        await using var stream = File.OpenRead(zipPath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        var actual = Convert.ToHexStringLower(hash);
        if (!actual.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Checksum mismatch. Expected: {expectedChecksum} Actual: {actual}");
    }

    private static async Task<UpdateManifest> LoadManifestAsync(string extractedPath)
    {
        var manifestPath = Path.Combine(extractedPath, "manifest.json");
        if (!File.Exists(manifestPath)) return new UpdateManifest();
        var json = await File.ReadAllTextAsync(manifestPath);
        return JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new UpdateManifest();
    }

    private static async Task DeployBinariesAsync(string extractedPath, string deployPath, UpdateManifest manifest, CancellationToken ct)
    {
        var binariesPath = Path.Combine(extractedPath, "binaries");
        if (!Directory.Exists(binariesPath)) binariesPath = extractedPath;

        Directory.CreateDirectory(deployPath);

        var preserveSet = manifest.PreserveFiles
            .Select(f => f.Replace('/', Path.DirectorySeparatorChar).ToLowerInvariant())
            .ToHashSet();

        foreach (var file in Directory.GetFiles(binariesPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(binariesPath, file);
            var dest = Path.Combine(deployPath, relative);

            if (preserveSet.Count > 0 && File.Exists(dest) &&
                preserveSet.Contains(relative.ToLowerInvariant()))
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private async Task VerifyHealthAsync(string healthCheckUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(healthCheckUrl)) return;

        var maxAttempts = options.Install.HealthCheckMaxAttempts;
        var delayMs     = options.Install.HealthCheckDelaySeconds * 1_000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(healthCheckUrl, ct);
                if (response.IsSuccessStatusCode) return;
                logger.LogWarning("Health check attempt {Attempt}/{Max} failed: {Status}", attempt, maxAttempts, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Health check attempt {Attempt}/{Max} exception", attempt, maxAttempts);
            }
            if (attempt < maxAttempts) await Task.Delay(delayMs, ct);
        }
        throw new InvalidOperationException($"Health check failed after {maxAttempts} attempts. Triggering rollback.");
    }

    private async Task ReportAsync(StartUpdateCommand command, UpdatePhase phase, bool isCompleted, bool isSuccess, string? errorMessage, CancellationToken ct)
    {
        var msg = new UpdateProgressMessage(
            options.InstallationId,
            command.UpdateHistoryId,
            phase.ToString(),
            isCompleted,
            isSuccess,
            errorMessage);

        if (OnProgress is not null)
            await OnProgress(msg);
    }
}
