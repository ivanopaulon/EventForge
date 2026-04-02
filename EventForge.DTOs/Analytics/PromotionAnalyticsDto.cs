namespace EventForge.DTOs.Analytics;

/// <summary>
/// Summary of promotion usage for a single promotion.
/// </summary>
public class PromotionUsageSummaryDto
{
    /// <summary>Promotion unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Promotion name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Total number of times the promotion has been used.</summary>
    public int TotalUses { get; set; }

    /// <summary>Total discount amount granted by this promotion.</summary>
    public decimal TotalDiscountAmount { get; set; }

    /// <summary>Average discount percentage applied.</summary>
    public decimal AverageDiscountPercentage { get; set; }

    /// <summary>Date and time when the promotion was last used.</summary>
    public DateTime? LastUsedDate { get; set; }
}

/// <summary>
/// A promotion ranked by usage within the query period.
/// </summary>
public class TopPromotionDto
{
    /// <summary>Rank position (1 = most used).</summary>
    public int Rank { get; set; }

    /// <summary>Promotion unique identifier.</summary>
    public Guid PromotionId { get; set; }

    /// <summary>Promotion name.</summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>Number of times the promotion was used.</summary>
    public int UsageCount { get; set; }

    /// <summary>Total savings delivered by this promotion.</summary>
    public decimal TotalSavings { get; set; }
}

/// <summary>
/// Promotion usage aggregated for a single time period.
/// </summary>
public class PromotionTrendDto
{
    /// <summary>Period label in yyyy-MM-dd format.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Number of promotion usages in this period.</summary>
    public int UsageCount { get; set; }

    /// <summary>Total discount amount applied in this period.</summary>
    public decimal TotalDiscountAmount { get; set; }
}

/// <summary>
/// Aggregate dashboard data for promotion analytics.
/// </summary>
public class PromotionAnalyticsDashboardDto
{
    /// <summary>Top promotions ordered by usage count.</summary>
    public List<TopPromotionDto> TopPromotions { get; set; } = new();

    /// <summary>Promotion usage trend over the requested period.</summary>
    public List<PromotionTrendDto> UsageTrend { get; set; } = new();

    /// <summary>Number of currently active promotions.</summary>
    public int TotalActivePromotions { get; set; }

    /// <summary>Total promotion uses in the current calendar month.</summary>
    public int TotalUsesThisMonth { get; set; }

    /// <summary>Total discount amount granted in the current calendar month.</summary>
    public decimal TotalDiscountThisMonth { get; set; }
}
