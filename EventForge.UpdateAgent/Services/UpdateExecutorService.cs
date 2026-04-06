using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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

    // ── Maintenance notification helpers ────────────────────────────────────

    /// <summary>
    /// Returns the <c>(notificationBaseUrl, maintenanceSecret)</c> pair for the given component name.
    /// </summary>
    private (string Url, string Secret) GetNotifConfig(string component)
    {
        var isServer = component.Equals("Server", StringComparison.OrdinalIgnoreCase);
        return isServer
            ? (options.Components.Server.NotificationBaseUrl, options.Components.Server.MaintenanceSecret)
            : (options.Components.Client.NotificationBaseUrl, options.Components.Client.MaintenanceSecret);
    }

    /// <summary>
    /// Sends a best-effort "progress" notification to the Server so connected clients can see
    /// the current phase + optional download stats in the <c>DownloadProgressSnackbar</c>.
    /// Never throws — failures are logged as warnings.
    /// </summary>
    private Task NotifyPhaseAsync(StartUpdateCommand command, string currentPhase,
        int? percentComplete = null,
        string? formattedDownloaded = null,
        string? formattedTotal = null,
        string? formattedSpeed = null,
        string? eta = null)
    {
        var (url, secret) = GetNotifConfig(command.Component);
        return NotifyServerAsync(url, secret, new
        {
            Phase = "progress",
            CurrentPhase = currentPhase,
            command.Component,
            command.Version,
            PercentComplete = percentComplete,
            FormattedDownloaded = formattedDownloaded,
            FormattedTotal = formattedTotal,
            FormattedSpeed = formattedSpeed,
            Eta = eta
        });
    }

    /// <summary>
    /// Fires a best-effort POST to EventForge.Server's maintenance endpoint so connected clients
    /// are notified before/after an update. Never throws — failures are logged as warnings.
    /// </summary>
    private async Task NotifyServerAsync(string notificationBaseUrl, string maintenanceSecret, object payload)
    {
        if (string.IsNullOrWhiteSpace(notificationBaseUrl)) return;
        try
        {
            var url = notificationBaseUrl.TrimEnd('/') + "/api/v1/system/maintenance";
            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Maintenance-Secret", maintenanceSecret);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resp = await _http.SendAsync(request, cts.Token);
            if (!resp.IsSuccessStatusCode)
                logger.LogWarning("Maintenance notification returned {StatusCode}", resp.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Maintenance notification failed (best-effort, update continues)");
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a best-effort "AwaitingMaintenanceWindow" progress notification so connected clients
    /// know a manual (or out-of-window) package has been downloaded and is queued for installation.
    /// </summary>
    public Task NotifyAwaitingInstallAsync(StartUpdateCommand command)
        => NotifyPhaseAsync(command, UpdatePhase.AwaitingMaintenanceWindow.ToString());

    /// <summary>
    /// Phase 1 + 2: resilient download with retry/resume, followed by SHA-256 checksum verification.
    /// Returns the local path to the verified zip file.
    /// </summary>
    public async Task<string> DownloadAndVerifyAsync(StartUpdateCommand command, CancellationToken ct)
    {
        await ReportAsync(command, UpdatePhase.Downloading, false, true, null, ct);
        // Notify connected clients immediately so the snackbar appears.
        await NotifyPhaseAsync(command, UpdatePhase.Downloading.ToString(), percentComplete: 0);

        var zipPath = await DownloadPackageAsync(command, ct);

        await ReportAsync(command, UpdatePhase.VerifyingChecksum, false, true, null, ct);
        await NotifyPhaseAsync(command, UpdatePhase.VerifyingChecksum.ToString());
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
            await NotifyPhaseAsync(command, UpdatePhase.BackingUp.ToString());
            backupPath = await backupService.CreateBackupAsync(deployPath, command.Component, command.Version, ct);

            // ── Phase 5: Pre-migrations ──
            if (isServer && manifest.PreMigrationScripts.Count > 0)
            {
                await ReportAsync(command, UpdatePhase.RunningPreMigrations, false, true, null, ct);
                await NotifyPhaseAsync(command, UpdatePhase.RunningPreMigrations.ToString());
                await migrationRunner.RunScriptsAsync(tempDir, manifest.PreMigrationScripts, ct);
            }

            // ── Phase 6: Stop IIS ──
            if (isServer)
            {
                // Notify connected clients that the Server is going into maintenance before stopping IIS.
                await NotifyServerAsync(
                    options.Components.Server.NotificationBaseUrl,
                    options.Components.Server.MaintenanceSecret,
                    new { Phase = "Starting", Component = command.Component, Version = command.Version });

                await ReportAsync(command, UpdatePhase.StoppingService, false, true, null, ct);
                await NotifyPhaseAsync(command, UpdatePhase.StoppingService.ToString());
                await iisManagerService.StopSiteAsync(ct);
            }

            // ── Phase 7: Deploy ──
            await ReportAsync(command, UpdatePhase.DeployingBinaries, false, true, null, ct);
            await NotifyPhaseAsync(command, UpdatePhase.DeployingBinaries.ToString());
            await DeployBinariesAsync(tempDir, deployPath, manifest, ct);

            // ── Phase 8: Write version.txt ──
            await File.WriteAllTextAsync(Path.Combine(deployPath, "version.txt"), command.Version, ct);

            // ── Phase 9: Start IIS ──
            if (isServer)
            {
                await ReportAsync(command, UpdatePhase.StartingService, false, true, null, ct);
                await NotifyPhaseAsync(command, UpdatePhase.StartingService.ToString());
                await iisManagerService.StartSiteAsync(ct);
            }

            // ── Phase 10: Post-migrations ──
            if (isServer && manifest.PostMigrationScripts.Count > 0)
            {
                await ReportAsync(command, UpdatePhase.RunningPostMigrations, false, true, null, ct);
                await NotifyPhaseAsync(command, UpdatePhase.RunningPostMigrations.ToString());
                await migrationRunner.RunScriptsAsync(tempDir, manifest.PostMigrationScripts, ct);
            }

            // ── Phase 11: Health check ──
            if (isServer)
            {
                await ReportAsync(command, UpdatePhase.VerifyingHealth, false, true, null, ct);
                await NotifyPhaseAsync(command, UpdatePhase.VerifyingHealth.ToString());
                await VerifyHealthAsync(options.Components.Server.HealthCheckUrl, ct);
            }

            // ── Complete ──
            await ReportAsync(command, UpdatePhase.Completed, true, true, null, ct);

            // Notify connected clients with 100 % so the snackbar briefly shows "Completato" before clearing.
            await NotifyPhaseAsync(command, UpdatePhase.Completed.ToString(), percentComplete: 100);

            // Notify clients that maintenance is over (Server back online).
            if (isServer)
            {
                await NotifyServerAsync(
                    options.Components.Server.NotificationBaseUrl,
                    options.Components.Server.MaintenanceSecret,
                    new { Phase = "Started", Component = command.Component, Version = command.Version });
            }
            else
            {
                // Client component deployed — tell the Server so browsers can refresh.
                await NotifyServerAsync(
                    options.Components.Client.NotificationBaseUrl,
                    options.Components.Client.MaintenanceSecret,
                    new { Phase = "ClientDeployed", Component = command.Component, Version = command.Version });
            }

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

    private async Task<string> DownloadPackageAsync(StartUpdateCommand command, CancellationToken ct)
    {
        var downloadUrl = command.DownloadUrl;
        var packageIdHex = command.PackageId.ToString("N");
        var component = command.Component;
        var version = command.Version;

        // Defensive fix: if Hub sent a relative URL, prepend the hub base URL
        if (downloadUrl.StartsWith('/'))
        {
            var hubBase = string.IsNullOrWhiteSpace(options.HubBaseUrl)
                ? options.HubUrl.Replace("/hubs/update", "").TrimEnd('/')
                : options.HubBaseUrl.TrimEnd('/');
            downloadUrl = hubBase + downloadUrl;
        }

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
                        await WriteResponseToFileAsync(response2, tmpPath, append: false, packageId, command, linkedCts.Token);
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        await WriteResponseToFileAsync(response, tmpPath, append: resumeFrom > 0, packageId, command, linkedCts.Token);
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
        HttpResponseMessage response, string tmpPath, bool append, Guid packageId,
        StartUpdateCommand command, CancellationToken ct)
    {
        var totalBytes = response.Content.Headers.ContentLength;
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);

        var fileMode = append ? FileMode.Append : FileMode.Create;
        await using var fileStream = new FileStream(tmpPath, fileMode, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer    = new byte[81920];
        long written  = append && File.Exists(tmpPath) ? new FileInfo(tmpPath).Length : 0;
        var lastLocalReport  = DateTime.UtcNow;
        var lastServerNotify = DateTime.UtcNow;
        int bytesRead;

        while ((bytesRead = await responseStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            written += bytesRead;

            var now = DateTime.UtcNow;

            if ((now - lastLocalReport).TotalSeconds >= 1)
            {
                downloadProgress.Update(packageId, written, totalBytes);
                lastLocalReport = now;
            }

            // Forward progress to the Server every 2 s so the client snackbar stays updated.
            if ((now - lastServerNotify).TotalSeconds >= 2)
            {
                lastServerNotify = now;
                var snap = downloadProgress.Current;
                if (snap is not null)
                    await NotifyPhaseAsync(command, UpdatePhase.Downloading.ToString(),
                        percentComplete: snap.PercentComplete,
                        formattedDownloaded: snap.FormattedDownloaded,
                        formattedTotal: snap.TotalBytes.HasValue ? snap.FormattedTotal : null,
                        formattedSpeed: snap.FormattedSpeed,
                        eta: snap.Eta?.ToString(@"mm\:ss"));
            }
        }

        // Final local + server update
        downloadProgress.Update(packageId, written, totalBytes);
        var finalSnap = downloadProgress.Current;
        if (finalSnap is not null)
            await NotifyPhaseAsync(command, UpdatePhase.Downloading.ToString(),
                percentComplete: 100,
                formattedDownloaded: finalSnap.FormattedDownloaded,
                formattedTotal: finalSnap.TotalBytes.HasValue ? finalSnap.FormattedTotal : null);
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

        var maxAttempts   = options.Install.HealthCheckMaxAttempts;
        var delayMs       = options.Install.HealthCheckDelaySeconds * 1_000;
        // Cap individual health-check requests to the configured delay + 5 s to avoid hanging forever.
        var requestTimeout = TimeSpan.FromMilliseconds(delayMs + 5_000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(requestTimeout);
                var response = await _http.GetAsync(healthCheckUrl, cts.Token);
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
