using Prym.DTOs.Monitoring;

namespace Prym.Client.Services;

/// <summary>
/// Client-side service implementation for the monitoring dashboard API.
/// </summary>
public class MonitoringClientService(
    IHttpClientService httpClientService,
    ILogger<MonitoringClientService> logger) : IMonitoringClientService
{
    private const string BaseUrl = "api/v1/monitoring";

    /// <inheritdoc />
    public async Task<MonitoringDashboardDto?> GetDashboardAsync(int topN = 10, int recentErrorCount = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/dashboard?topN={topN}&recentErrorCount={recentErrorCount}";
            return await httpClientService.GetAsync<MonitoringDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving monitoring dashboard");
            return null;
        }
    }
}
