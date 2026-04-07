using Prym.DTOs.Analytics;
using Prym.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Prym.Server.Services.Analytics;

/// <summary>
/// Service implementation for retrieving aggregated analytics data.
/// </summary>
public class AnalyticsService(
    PrymDbContext context,
    ITenantContext tenantContext,
    ILogger<AnalyticsService> logger,
    IMemoryCache cache,
    IMonitoringMetricsService monitoringMetrics) : IAnalyticsService
{

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public async Task<PromotionAnalyticsDashboardDto> GetPromotionAnalyticsAsync(
        AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for analytics operations.");

            var cacheKey = $"analytics_promotions_{tenantId}_{filter.DateFrom}_{filter.DateTo}_{filter.GroupBy}";
            if (cache.TryGetValue(cacheKey, out PromotionAnalyticsDashboardDto? cached) && cached is not null)
            {
                monitoringMetrics.RecordCacheLookup(true);
                return cached;
            }
            monitoringMetrics.RecordCacheLookup(false);

            var now = DateTime.UtcNow;
            var top = filter.Top > 0 ? filter.Top : 10;

            // Top promotions by CurrentUses
            var topPromotions = await context.Promotions
                .Where(p => !p.IsDeleted && p.TenantId == tenantId.Value)
                .OrderByDescending(p => p.CurrentUses)
                .Take(top)
                .Select(p => new { p.Id, p.Name, p.CurrentUses })
                .ToListAsync(ct);

            // Usage trend from DocumentRows with AppliedPromotionsJSON set
            var dateFrom = filter.DateFrom ?? now.AddMonths(-12);
            var dateTo = filter.DateTo ?? now;

            var trendRaw = await context.DocumentRows
                .Where(r => !r.IsDeleted
                    && r.TenantId == tenantId.Value
                    && r.AppliedPromotionsJSON != null
                    && r.DocumentHeader != null
                    && r.DocumentHeader.Date >= dateFrom
                    && r.DocumentHeader.Date <= dateTo)
                .Select(r => new
                {
                    r.DocumentHeader!.Date,
                    DiscountValue = r.LineDiscountValue
                })
                .ToListAsync(ct);

            // Distribute total promotion savings proportionally by usage count
            var totalUsageCount = topPromotions.Sum(p => p.CurrentUses);
            var totalDiscountFromRows = trendRaw.Sum(r => r.DiscountValue);

            var topPromotionDtos = topPromotions
                .Select((p, idx) => new TopPromotionDto
                {
                    Rank = idx + 1,
                    PromotionId = p.Id,
                    PromotionName = p.Name,
                    UsageCount = p.CurrentUses,
                    TotalSavings = totalUsageCount > 0
                        ? Math.Round(totalDiscountFromRows * p.CurrentUses / totalUsageCount, 2)
                        : 0m
                })
                .ToList();

            var groupBy = filter.GroupBy?.ToLowerInvariant() ?? "month";
            var usageTrend = GroupTrend(trendRaw.Select(r => (r.Date, r.DiscountValue)), groupBy)
                .Select(g => new PromotionTrendDto
                {
                    Date = g.Label,
                    UsageCount = g.Count,
                    TotalDiscountAmount = g.TotalAmount
                })
                .ToList();

            // Active promotions
            var activeCount = await context.Promotions
                .CountAsync(p => !p.IsDeleted
                    && p.TenantId == tenantId.Value
                    && p.StartDate <= now
                    && p.EndDate >= now, ct);

            // This month stats
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var thisMonthRows = trendRaw.Where(r => r.Date >= monthStart).ToList();

            var result = new PromotionAnalyticsDashboardDto
            {
                TopPromotions = topPromotionDtos,
                UsageTrend = usageTrend,
                TotalActivePromotions = activeCount,
                TotalUsesThisMonth = thisMonthRows.Count,
                TotalDiscountThisMonth = thisMonthRows.Sum(r => r.DiscountValue)
            };

            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotion analytics for tenant {TenantId}", tenantContext.CurrentTenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PricingAnalyticsDashboardDto> GetPricingAnalyticsAsync(
        AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for analytics operations.");

            var cacheKey = $"analytics_pricing_{tenantId}_{filter.DateFrom}_{filter.DateTo}_{filter.GroupBy}";
            if (cache.TryGetValue(cacheKey, out PricingAnalyticsDashboardDto? cached) && cached is not null)
            {
                monitoringMetrics.RecordCacheLookup(true);
                return cached;
            }
            monitoringMetrics.RecordCacheLookup(false);

            var now = DateTime.UtcNow;
            var top = filter.Top > 0 ? filter.Top : 10;
            var dateFrom = filter.DateFrom ?? now.AddMonths(-12);
            var dateTo = filter.DateTo ?? now;

            // Top price lists by usage
            var priceListUsage = await context.DocumentRows
                .Where(r => !r.IsDeleted
                    && r.TenantId == tenantId.Value
                    && r.AppliedPriceListId.HasValue
                    && r.DocumentHeader != null
                    && r.DocumentHeader.Date >= dateFrom
                    && r.DocumentHeader.Date <= dateTo)
                .GroupBy(r => r.AppliedPriceListId!.Value)
                .Select(g => new
                {
                    PriceListId = g.Key,
                    TimesApplied = g.Count(),
                    DocumentCount = g.Select(r => r.DocumentHeaderId).Distinct().Count(),
                    AverageDiscount = g.Average(r => r.LineDiscount)
                })
                .OrderByDescending(x => x.TimesApplied)
                .Take(top)
                .ToListAsync(ct);

            var priceListIds = priceListUsage.Select(x => x.PriceListId).ToList();
            var priceListNames = await context.PriceLists
                .Where(pl => priceListIds.Contains(pl.Id))
                .Select(pl => new { pl.Id, pl.Name })
                .ToDictionaryAsync(pl => pl.Id, pl => pl.Name, ct);

            var topPriceLists = priceListUsage
                .Select(x => new PriceListUsageSummaryDto
                {
                    PriceListId = x.PriceListId,
                    PriceListName = priceListNames.TryGetValue(x.PriceListId, out var name) ? name : x.PriceListId.ToString(),
                    TimesApplied = x.TimesApplied,
                    DocumentCount = x.DocumentCount,
                    AverageDiscount = x.AverageDiscount
                })
                .ToList();

            // Manual price overrides trend
            var manualRaw = await context.DocumentRows
                .Where(r => !r.IsDeleted
                    && r.TenantId == tenantId.Value
                    && r.IsPriceManual
                    && r.DocumentHeader != null
                    && r.DocumentHeader.Date >= dateFrom
                    && r.DocumentHeader.Date <= dateTo)
                .Select(r => new
                {
                    r.DocumentHeader!.Date,
                    Amount = r.UnitPrice * r.Quantity
                })
                .ToListAsync(ct);

            var groupBy = filter.GroupBy?.ToLowerInvariant() ?? "month";
            var manualOverridesTrend = GroupTrend(manualRaw.Select(r => (r.Date, r.Amount)), groupBy)
                .Select(g => new ManualPriceOverrideDto
                {
                    Date = g.Label,
                    Count = g.Count,
                    TotalAmount = g.TotalAmount
                })
                .ToList();

            // Total rows vs manual rows for automatic pricing percentage
            var totalRows = await context.DocumentRows
                .CountAsync(r => !r.IsDeleted
                    && r.TenantId == tenantId.Value
                    && r.DocumentHeader != null
                    && r.DocumentHeader.Date >= dateFrom
                    && r.DocumentHeader.Date <= dateTo, ct);

            var totalManual = manualRaw.Count;
            var automaticPct = totalRows > 0
                ? Math.Round(100m - (totalManual * 100m / totalRows), 2)
                : 100m;

            var result = new PricingAnalyticsDashboardDto
            {
                TopPriceLists = topPriceLists,
                ManualOverridesTrend = manualOverridesTrend,
                TotalManualOverrides = totalManual,
                AutomaticPricingPercentage = automaticPct
            };

            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving pricing analytics for tenant {TenantId}", tenantContext.CurrentTenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SalesAnalyticsDashboardDto> GetSalesAnalyticsAsync(
        AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for analytics operations.");

            var cacheKey = $"analytics_sales_{tenantId}_{filter.DateFrom}_{filter.DateTo}_{filter.GroupBy}";
            if (cache.TryGetValue(cacheKey, out SalesAnalyticsDashboardDto? cached) && cached is not null)
            {
                monitoringMetrics.RecordCacheLookup(true);
                return cached;
            }
            monitoringMetrics.RecordCacheLookup(false);

            var now = DateTime.UtcNow;
            var top = filter.Top > 0 ? filter.Top : 10;
            var dateFrom = (filter.DateFrom ?? now.AddMonths(-12)).Date;
            var dateTo = (filter.DateTo ?? now).Date.AddDays(1).AddTicks(-1);

            // Sales trend from DocumentHeaders (exclude cancelled)
            var headersRaw = await context.DocumentHeaders
                .Where(h => !h.IsDeleted
                    && h.TenantId == tenantId.Value
                    && h.Status != DocumentStatus.Cancelled
                    && h.Date >= dateFrom
                    && h.Date <= dateTo)
                .Select(h => new
                {
                    h.Date,
                    Amount = h.TotalNetAmount
                })
                .ToListAsync(ct);

            // Debug: log before-filter count only when debug logging is enabled to avoid extra DB round-trip in production
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var totalDocsInRange = await context.DocumentHeaders
                    .CountAsync(h => !h.IsDeleted && h.TenantId == tenantId.Value && h.Date >= dateFrom && h.Date <= dateTo, ct);
                logger.LogDebug("Sales analytics: {TotalDocsInRange} documents in range [{DateFrom}, {DateTo}] (pre-filter); {HeadersCount} non-cancelled; TotalNetAmount={TotalAmount}",
                    totalDocsInRange, dateFrom, dateTo, headersRaw.Count, headersRaw.Sum(h => h.Amount));
            }

            if (!headersRaw.Any())
            {
                logger.LogInformation("Sales analytics: no documents found for tenant {TenantId} in range [{DateFrom}, {DateTo}]",
                    tenantId, dateFrom, dateTo);
                return new SalesAnalyticsDashboardDto
                {
                    SalesTrend = [],
                    TopProducts = [],
                    TotalRevenue = 0m,
                    TotalDocuments = 0,
                    AverageOrderValue = 0m,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                };
            }

            var groupBy = filter.GroupBy?.ToLowerInvariant() ?? "month";

            // If all headers have TotalNetAmount == 0, fallback to sum(UnitPrice * Quantity) from rows
            var totalRevenue = headersRaw.Sum(h => h.Amount);
            if (totalRevenue == 0m)
            {
                logger.LogInformation("Sales analytics: TotalNetAmount is 0 on all {Count} headers for tenant {TenantId}, falling back to DocumentRows sum",
                    headersRaw.Count, tenantId);
                var rowsRevenue = await context.DocumentRows
                    .Where(r => !r.IsDeleted
                        && r.TenantId == tenantId.Value
                        && r.DocumentHeader != null
                        && r.DocumentHeader.Status != DocumentStatus.Cancelled
                        && r.DocumentHeader.Date >= dateFrom
                        && r.DocumentHeader.Date <= dateTo)
                    .SumAsync(r => r.UnitPrice * r.Quantity, ct);
                totalRevenue = rowsRevenue;
            }

            var salesTrend = GroupTrend(headersRaw.Select(h => (h.Date, h.Amount)), groupBy)
                .Select(g => new SalesTrendDto
                {
                    Date = g.Label,
                    TotalAmount = g.TotalAmount,
                    DocumentCount = g.Count,
                    AverageOrderValue = g.Count > 0 ? Math.Round(g.TotalAmount / g.Count, 2) : 0m
                })
                .ToList();

            // Top products by revenue
            var productRows = await context.DocumentRows
                .Where(r => !r.IsDeleted
                    && r.TenantId == tenantId.Value
                    && r.ProductId.HasValue
                    && r.DocumentHeader != null
                    && r.DocumentHeader.Status != DocumentStatus.Cancelled
                    && r.DocumentHeader.Date >= dateFrom
                    && r.DocumentHeader.Date <= dateTo)
                .Select(r => new
                {
                    r.ProductId,
                    r.Description,
                    r.Quantity,
                    Revenue = r.UnitPrice * r.Quantity,
                    Discount = r.LineDiscountValue
                })
                .ToListAsync(ct);

            var topProductsRaw = productRows
                .GroupBy(r => r.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().Description,
                    TotalQuantity = g.Sum(r => r.Quantity),
                    TotalRevenue = g.Sum(r => r.Revenue),
                    TotalDiscount = g.Sum(r => r.Discount)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(top)
                .Select((x, idx) => new TopProductDto
                {
                    Rank = idx + 1,
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    TotalQuantity = x.TotalQuantity,
                    TotalRevenue = x.TotalRevenue,
                    TotalDiscount = x.TotalDiscount
                })
                .ToList();

            var totalDocs = headersRaw.Count;

            var result = new SalesAnalyticsDashboardDto
            {
                SalesTrend = salesTrend,
                TopProducts = topProductsRaw,
                TotalRevenue = totalRevenue,
                TotalDocuments = totalDocs,
                AverageOrderValue = totalDocs > 0 ? Math.Round(totalRevenue / totalDocs, 2) : 0m,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving sales analytics for tenant {TenantId}", tenantContext.CurrentTenantId);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private record TrendGroup(string Label, int Count, decimal TotalAmount);

    /// <summary>
    /// Groups a sequence of (date, amount) pairs into labelled buckets.
    /// </summary>
    private static IEnumerable<TrendGroup> GroupTrend(
        IEnumerable<(DateTime Date, decimal Amount)> items,
        string groupBy)
    {
        return groupBy switch
        {
            "day" => items
                .GroupBy(x => x.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new TrendGroup(
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Count(),
                    g.Sum(x => x.Amount))),

            "week" => items
                .GroupBy(x => GetIso8601WeekLabel(x.Date))
                .OrderBy(g => g.Key)
                .Select(g => new TrendGroup(
                    g.Key,
                    g.Count(),
                    g.Sum(x => x.Amount))),

            _ => items // default: month
                .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new TrendGroup(
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Count(),
                    g.Sum(x => x.Amount)))
        };
    }

    /// <summary>
    /// Returns a sortable ISO-8601 week label such as "2024-W03".
    /// </summary>
    private static string GetIso8601WeekLabel(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
        return $"{date.Year}-W{week:D2}";
    }

}
