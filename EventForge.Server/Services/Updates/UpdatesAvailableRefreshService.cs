using EventForge.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Services.Updates;

/// <summary>
/// BackgroundService that periodically queries the UpdateHub for the count of packages
/// with status <c>ReadyToDeploy</c> and broadcasts the result to all SuperAdmin clients
/// via the <see cref="AppHub"/> ("superadmin" group, event "UpdatesAvailable").
///
/// This keeps the FAB badge on the client up-to-date without requiring the SuperAdmin to
/// manually open the updates dialog.
/// </summary>
public sealed class UpdatesAvailableRefreshService(
    IUpdateHubProxyService hubProxy,
    IHubContext<AppHub> hubContext,
    IConfiguration configuration,
    ILogger<UpdatesAvailableRefreshService> logger) : BackgroundService
{
    private const int DefaultIntervalSeconds = 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = configuration["UpdateHub:BaseUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogInformation("UpdateHub:BaseUrl not configured — UpdatesAvailableRefreshService idle.");
            return;
        }

        var intervalSeconds = configuration.GetValue<int>("UpdateHub:AvailableRefreshIntervalSeconds", DefaultIntervalSeconds);
        logger.LogInformation("UpdatesAvailableRefreshService started (interval: {Interval}s).", intervalSeconds);

        // Small initial delay so the server is fully started before the first call.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await BroadcastCountAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Fetches the ReadyToDeploy count and broadcasts it to the "superadmin" SignalR group.
    /// Called by <see cref="UpdateNotificationHub.OnConnectedAsync"/> on SuperAdmin connect as well.
    /// </summary>
    public async Task BroadcastCountAsync(CancellationToken ct = default)
    {
        try
        {
            var packages = await hubProxy.GetPackagesAsync(ct);
            var count = packages.Count(p =>
                p.Status.Equals("ReadyToDeploy", StringComparison.OrdinalIgnoreCase));

            await hubContext.Clients.Group("superadmin").SendAsync(
                "UpdatesAvailable", new { Count = count }, ct);

            logger.LogDebug("Broadcasted UpdatesAvailable count={Count} to superadmin group.", count);
        }
        catch (UpdateHubNotConfiguredException)
        {
            // Hub not configured — count stays at 0, no broadcast needed.
        }
        catch (HttpRequestException ex) when (!ct.IsCancellationRequested && IsConnectionRefused(ex))
        {
            // ManagementHub is not reachable (connection refused / service not started).
            // Log at Debug to avoid filling the logs during development when Hub is not running.
            logger.LogDebug(
                "UpdatesAvailableRefreshService: ManagementHub not reachable at configured URL ({Message}). " +
                "Start Prym.ManagementHub or clear UpdateHub:BaseUrl to suppress this message.",
                ex.Message);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex,
                "UpdatesAvailableRefreshService: failed to fetch package count. {ExceptionType}: {ErrorMessage}",
                ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// Returns true when <paramref name="ex"/> (or any inner exception) represents a
    /// TCP connection-refused / host-unreachable error — i.e. the remote service is simply not running.
    /// </summary>
    private static bool IsConnectionRefused(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is System.Net.Sockets.SocketException se &&
                (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.NetworkUnreachable ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut))
                return true;
            ex = ex.InnerException;
        }
        return false;
    }
}
