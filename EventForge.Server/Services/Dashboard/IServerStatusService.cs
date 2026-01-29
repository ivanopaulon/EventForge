using EventForge.DTOs.Dashboard;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Service for retrieving server status information.
/// </summary>
public interface IServerStatusService
{
    /// <summary>
    /// Gets current server status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server status information</returns>
    Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default);
}
