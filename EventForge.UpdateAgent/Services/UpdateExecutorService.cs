using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Orchestrates the full update sequence: Download → Backup → Stop → Migrate → Deploy → Start → Verify → Report.
/// </summary>
public class UpdateExecutorService(
    AgentOptions options,
    BackupService backupService,
    IisManagerService iisManagerService,
    MigrationRunnerService migrationRunner,
    VersionDetectorService versionDetector,
    ILogger<UpdateExecutorService> logger)
{
    private static readonly HttpClient _http = new();

    public event Func<UpdateProgressMessage, Task>? OnProgress;

    public async Task ExecuteAsync(StartUpdateCommand command, CancellationToken ct)
    {
        var isServer = command.Component.Equals("Server", StringComparison.OrdinalIgnoreCase);
        var deployPath = isServer ? options.Components.Server.DeployPath : options.Components.Client.DeployPath;
        var tempDir = Path.Combine(AppContext.BaseDirectory, "updates", $"ef-update-{command.PackageId:N}");
        string? backupPath = null;
        string? zipPath = null;

        try
        {
            // ── Phase 1: Download ──
            await ReportAsync(command, UpdatePhase.Downloading, false, true, null, ct);
            zipPath = await DownloadPackageAsync(command.DownloadUrl, ct);

            // ── Phase 2: Verify Checksum ──
            await ReportAsync(command, UpdatePhase.VerifyingChecksum, false, true, null, ct);
            await VerifyChecksumAsync(zipPath, command.Checksum, ct);

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
            await DeployBinariesAsync(tempDir, deployPath, ct);

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
            logger.LogError(ex, "Update failed at component={Component} version={Version}", command.Component, command.Version);

            // Rollback attempt
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
                        var manifest2 = await LoadManifestAsync(tempDir);
                        if (manifest2.RollbackScripts.Count > 0)
                            await migrationRunner.RunScriptsAsync(tempDir, manifest2.RollbackScripts, CancellationToken.None);
                    }
                }
                catch (Exception rollbackEx)
                {
                    logger.LogError(rollbackEx, "Rollback also failed!");
                }
            }

            await ReportAsync(command, UpdatePhase.Completed, true, false, ex.Message, CancellationToken.None);
        }
        finally
        {
            // Cleanup temp files
            if (zipPath is not null && File.Exists(zipPath))
                try { File.Delete(zipPath); } catch { /* best effort */ }
            if (Directory.Exists(tempDir))
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private async Task<string> DownloadPackageAsync(string downloadUrl, CancellationToken ct)
    {
        var downloadDir = Path.Combine(AppContext.BaseDirectory, "updates");
        Directory.CreateDirectory(downloadDir);
        var zipPath = Path.Combine(downloadDir, $"ef-pkg-{Guid.NewGuid():N}.zip");

        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        _http.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);

        logger.LogInformation("Downloading package from {Url}", downloadUrl);
        await using var response = await _http.GetStreamAsync(downloadUrl, ct);
        await using var file = File.Create(zipPath);
        await response.CopyToAsync(file, ct);
        return zipPath;
    }

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

    private static async Task DeployBinariesAsync(string extractedPath, string deployPath, CancellationToken ct)
    {
        var binariesPath = Path.Combine(extractedPath, "binaries");
        if (!Directory.Exists(binariesPath)) binariesPath = extractedPath;

        Directory.CreateDirectory(deployPath);
        foreach (var file in Directory.GetFiles(binariesPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(binariesPath, file);
            var dest = Path.Combine(deployPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private async Task VerifyHealthAsync(string healthCheckUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(healthCheckUrl)) return;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(healthCheckUrl, ct);
                if (response.IsSuccessStatusCode) return;
                logger.LogWarning("Health check attempt {Attempt}/5 failed: {Status}", attempt, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Health check attempt {Attempt}/5 exception", attempt);
            }
            if (attempt < 5) await Task.Delay(5000, ct);
        }
        throw new InvalidOperationException("Health check failed after 5 attempts. Triggering rollback.");
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
