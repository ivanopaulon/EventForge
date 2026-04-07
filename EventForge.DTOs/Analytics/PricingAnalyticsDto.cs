namespace EventForge.DTOs.Analytics;

/// <summary>
/// Usage summary for a single price list.
/// </summary>
public class PriceListUsageSummaryDto
{
    /// <summary>Price list unique identifier.</summary>
    public Guid PriceListId { get; set; }

    /// <summary>Price list name.</summary>
    public string PriceListName { get; set; } = string.Empty;

    /// <summary>Number of times this price list was applied to a document row.</summary>
    public int TimesApplied { get; set; }

    /// <summary>Number of distinct documents that used this price list.</summary>
    public int DocumentCount { get; set; }

    /// <summary>Average discount percentage associated with this price list.</summary>
    public decimal AverageDiscount { get; set; }
}

/// <summary>
/// Manual price override count for a single time period.
/// </summary>
public class ManualPriceOverrideDto
{
    /// <summary>Period label (yyyy-MM-dd).</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Number of manual price overrides in this period.</summary>
    public int Count { get; set; }

    /// <summary>Total amount of manually-priced rows in this period.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Aggregate dashboard data for pricing analytics.
/// </summary>
public class PricingAnalyticsDashboardDto
{
    /// <summary>Top price lists ordered by usage.</summary>
    public List<PriceListUsageSummaryDto> TopPriceLists { get; set; } = new();

    /// <summary>Manual price override trend over the requested period.</summary>
    public List<ManualPriceOverrideDto> ManualOverridesTrend { get; set; } = new();

    /// <summary>Total number of manual price overrides in the period.</summary>
    public int TotalManualOverrides { get; set; }

    /// <summary>Percentage of rows priced automatically (0–100).</summary>
    public decimal AutomaticPricingPercentage { get; set; }
}
