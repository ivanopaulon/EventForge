using Prym.DTOs.Analytics;

namespace Prym.Client.Services;

/// <summary>
/// Client service for consuming the analytics API endpoints.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Retrieves promotion analytics data for the given filter.
    /// </summary>
    Task<PromotionAnalyticsDashboardDto?> GetPromotionAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves pricing analytics data for the given filter.
    /// </summary>
    Task<PricingAnalyticsDashboardDto?> GetPricingAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves sales analytics data for the given filter.
    /// </summary>
    Task<SalesAnalyticsDashboardDto?> GetSalesAnalyticsAsync(AnalyticsFilterDto? filter = null, CancellationToken ct = default);
}
