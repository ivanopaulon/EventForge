namespace EventForge.DTOs.Analytics
{
    /// <summary>
    /// Dashboard data for promotion analytics.
    /// </summary>
    public class PromotionAnalyticsDashboardDto
    {
        /// <summary>
        /// Total number of currently active promotions.
        /// </summary>
        public int TotalActivePromotions { get; set; }

        /// <summary>
        /// Total number of promotion uses in the current month.
        /// </summary>
        public int TotalUsesThisMonth { get; set; }

        /// <summary>
        /// Total discount amount distributed this month.
        /// </summary>
        public decimal TotalDiscountThisMonth { get; set; }

        /// <summary>
        /// Top promotions by usage within the requested period.
        /// </summary>
        public List<PromotionUsageItemDto> TopPromotions { get; set; } = new();

        /// <summary>
        /// Monthly usage trend for promotions.
        /// </summary>
        public List<AnalyticsTrendPointDto> UsageTrend { get; set; } = new();
    }

    /// <summary>
    /// Usage statistics for a single promotion.
    /// </summary>
    public class PromotionUsageItemDto
    {
        /// <summary>Rank in the top-N list (1-based).</summary>
        public int Rank { get; set; }

        /// <summary>Promotion name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Total number of times the promotion was applied.</summary>
        public int TotalUses { get; set; }

        /// <summary>Total discount amount granted by this promotion.</summary>
        public decimal TotalSavings { get; set; }
    }

    /// <summary>
    /// A single data point in an analytics trend series.
    /// </summary>
    public class AnalyticsTrendPointDto
    {
        /// <summary>Period label (e.g. "Jan 2025").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Numeric value for the period.</summary>
        public double Value { get; set; }
    }
}
