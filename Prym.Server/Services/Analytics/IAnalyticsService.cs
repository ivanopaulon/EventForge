using Prym.DTOs.Analytics;

namespace Prym.Server.Services.Analytics;

/// <summary>
/// Service for retrieving aggregated analytics data for promotions, pricing and sales.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Returns promotion analytics dashboard data for the given filter.
    /// </summary>
    Task<PromotionAnalyticsDashboardDto> GetPromotionAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Returns pricing analytics dashboard data for the given filter.
    /// </summary>
    Task<PricingAnalyticsDashboardDto> GetPricingAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Returns sales analytics dashboard data for the given filter.
    /// </summary>
    Task<SalesAnalyticsDashboardDto> GetSalesAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);
}
