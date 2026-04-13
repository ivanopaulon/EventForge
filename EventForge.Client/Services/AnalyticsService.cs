using Prym.DTOs.Analytics;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for consuming the analytics API endpoints.
/// </summary>
public class AnalyticsService(
    IHttpClientService httpClientService,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private const string BaseUrl = "api/v1/analytics";

    /// <inheritdoc />
    public async Task<PromotionAnalyticsDashboardDto?> GetPromotionAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/promotions", filter);
            return await httpClientService.GetAsync<PromotionAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotion analytics");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PricingAnalyticsDashboardDto?> GetPricingAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/pricing", filter);
            return await httpClientService.GetAsync<PricingAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving pricing analytics");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<SalesAnalyticsDashboardDto?> GetSalesAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildUrl($"{BaseUrl}/sales", filter);
            return await httpClientService.GetAsync<SalesAnalyticsDashboardDto>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving sales analytics");
            return null;
        }
    }

    private static string BuildUrl(string baseEndpoint, AnalyticsFilterDto? filter)
    {
        if (filter is null)
            return baseEndpoint;

        List<string> queryParams = [];

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
