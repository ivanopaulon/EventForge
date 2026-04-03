using EventForge.DTOs.Monitoring;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service implementation for the monitoring dashboard API.
/// </summary>
public class MonitoringClientService : IMonitoringClientService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<MonitoringClientService> _logger;
    private const string BaseUrl = "api/v1/monitoring";

    public MonitoringClientService(IHttpClientService httpClientService, ILogger<MonitoringClientService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MonitoringDashboardDto?> GetDashboardAsync(int topN = 10, int recentErrorCount = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/dashboard?topN={topN}&recentErrorCount={recentErrorCount}";
            return await _httpClientService.GetAsync<MonitoringDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring dashboard");
            return null;
        }
    }
}
