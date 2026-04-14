namespace Prym.ManagementHub.Services;

/// <summary>
/// Background service that periodically removes old archived/deployed packages from the
/// database and from disk according to <see cref="ManagementHubOptions.PackageRetentionDays"/>.
/// Runs every <see cref="ManagementHubOptions.PackageCleanupIntervalHours"/> hours.
/// Disabled when either setting is 0.
/// </summary>
public class PackageCleanupService(
    IServiceScopeFactory scopeFactory,
    ManagementHubOptions hubOptions,
    ILogger<PackageCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (hubOptions.PackageCleanupIntervalHours <= 0 || hubOptions.PackageRetentionDays <= 0)
        {
            logger.LogInformation(
                "PackageCleanupService disabled (IntervalHours={Interval}, RetentionDays={Retention}).",
                hubOptions.PackageCleanupIntervalHours, hubOptions.PackageRetentionDays);
            return;
        }

        logger.LogInformation(
            "PackageCleanupService started. Interval={Interval}h Retention={Retention}d",
            hubOptions.PackageCleanupIntervalHours, hubOptions.PackageRetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Run the first pass after a short initial delay so startup isn't affected.
            await Task.Delay(TimeSpan.FromHours(hubOptions.PackageCleanupIntervalHours), stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PackageCleanupService encountered an error during cleanup.");
            }
        }

        logger.LogInformation("PackageCleanupService stopping.");
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-hubOptions.PackageRetentionDays);

        using var scope = scopeFactory.CreateScope();
        var packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();

        var expired = await packageService.GetExpiredPackagesAsync(cutoff, ct);
        if (expired.Count == 0)
        {
            logger.LogDebug("PackageCleanupService: no expired packages found (cutoff={Cutoff:u}).", cutoff);
            return;
        }

        var deleted = 0;
        foreach (var pkg in expired)
        {
            try
            {
                var filePath = await packageService.GetDownloadPathAsync(pkg.Id, ct);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                await packageService.DeleteAsync(pkg.Id, ct);
                deleted++;

                logger.LogInformation(
                    "PackageCleanupService: deleted expired package Id={Id} Version={Version} UploadedAt={UploadedAt:u}.",
                    pkg.Id, pkg.Version, pkg.UploadedAt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "PackageCleanupService: failed to delete package Id={Id} Version={Version}.",
                    pkg.Id, pkg.Version);
            }
        }

        logger.LogInformation(
            "PackageCleanupService: cleanup complete. Deleted={Deleted}/{Total} packages older than {Cutoff:u}.",
            deleted, expired.Count, cutoff);
    }
}
