using EventForge.DTOs.Analytics;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for consuming the analytics API endpoints.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<AnalyticsService> _logger;
    private const string BaseUrl = "api/v1/analytics";

    public AnalyticsService(IHttpClientService httpClientService, ILogger<AnalyticsService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PromotionAnalyticsDashboardDto?> GetPromotionAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/promotions", filter);
            return await _httpClientService.GetAsync<PromotionAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotion analytics");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PricingAnalyticsDashboardDto?> GetPricingAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/pricing", filter);
            return await _httpClientService.GetAsync<PricingAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pricing analytics");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<SalesAnalyticsDashboardDto?> GetSalesAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/sales", filter);
            return await _httpClientService.GetAsync<SalesAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales analytics");
            return null;
        }
    }

    private static string BuildUrl(string baseEndpoint, AnalyticsFilterDto? filter)
    {
        if (filter is null)
            return baseEndpoint;

        var queryParams = new List<string>();

        if (filter.DateFrom.HasValue)
            queryParams.Add($"dateFrom={filter.DateFrom.Value:yyyy-MM-dd}");

        if (filter.DateTo.HasValue)
            queryParams.Add($"dateTo={filter.DateTo.Value:yyyy-MM-dd}");

        if (filter.Top > 0)
            queryParams.Add($"top={filter.Top}");

        if (!string.IsNullOrWhiteSpace(filter.GroupBy))
            queryParams.Add($"groupBy={filter.GroupBy}");

        return queryParams.Count > 0
            ? $"{baseEndpoint}?{string.Join("&", queryParams)}"
            : baseEndpoint;
    }
}
