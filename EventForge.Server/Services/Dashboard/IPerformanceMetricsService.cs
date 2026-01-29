using EventForge.DTOs.Dashboard;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Service for retrieving performance metrics.
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Gets current performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance metrics information</returns>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
}
