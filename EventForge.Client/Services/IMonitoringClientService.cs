using Prym.DTOs.Monitoring;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service interface for consuming the monitoring API endpoint.
/// </summary>
public interface IMonitoringClientService
{
    /// <summary>
    /// Retrieves the monitoring dashboard snapshot.
    /// </summary>
    /// <param name="topN">Number of top promotions to include (default 10).</param>
    /// <param name="recentErrorCount">Number of recent error entries to include (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<MonitoringDashboardDto?> GetDashboardAsync(int topN = 10, int recentErrorCount = 20, CancellationToken ct = default);
}
