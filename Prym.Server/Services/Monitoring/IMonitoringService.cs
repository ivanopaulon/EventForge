using Prym.DTOs.Monitoring;

namespace Prym.Server.Services.Monitoring;

/// <summary>
/// Service for assembling the monitoring dashboard snapshot.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Returns a full monitoring dashboard snapshot for the current tenant.
    /// </summary>
    /// <param name="topN">Number of top promotions to return (default 10).</param>
    /// <param name="recentErrorCount">Number of recent error log entries to return (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<MonitoringDashboardDto> GetDashboardAsync(int topN = 10, int recentErrorCount = 20, CancellationToken ct = default);
}
