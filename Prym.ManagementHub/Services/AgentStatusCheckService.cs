namespace Prym.ManagementHub.Services;

/// <summary>
/// Background service that periodically marks stale installations as <see cref="InstallationStatus.Offline"/>.
/// An installation is considered stale when <c>LastSeen</c> is older than
/// <see cref="ManagementHubOptions.HeartbeatTimeoutSeconds"/> seconds from now.
/// Runs every <see cref="ManagementHubOptions.AgentStatusCheckIntervalSeconds"/> seconds.
/// </summary>
public class AgentStatusCheckService(
    IServiceScopeFactory scopeFactory,
    ManagementHubOptions hubOptions,
    ILogger<AgentStatusCheckService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "AgentStatusCheckService started. Interval={Interval}s Timeout={Timeout}s",
            hubOptions.AgentStatusCheckIntervalSeconds,
            hubOptions.HeartbeatTimeoutSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromSeconds(
                Math.Max(10, hubOptions.AgentStatusCheckIntervalSeconds));

            await Task.Delay(interval, stoppingToken);

            try
            {
                await CheckStaleAgentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AgentStatusCheckService encountered an error during stale-agent check.");
            }
        }

        logger.LogInformation("AgentStatusCheckService stopping.");
    }

    private async Task CheckStaleAgentsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(
            -Math.Max(1, hubOptions.HeartbeatTimeoutSeconds));

        using var scope = scopeFactory.CreateScope();
        var installationService = scope.ServiceProvider.GetRequiredService<IInstallationService>();

        var stale = await installationService.GetStaleInstallationsAsync(cutoff, ct);
        if (stale.Count == 0) return;

        foreach (var id in stale)
        {
            await installationService.UpdateLastSeenAsync(
                id, null, null, InstallationStatus.Offline, ct);
        }

        logger.LogInformation(
            "AgentStatusCheckService: marked {Count} installation(s) offline (LastSeen < {Cutoff:u}).",
            stale.Count, cutoff);
    }
}
