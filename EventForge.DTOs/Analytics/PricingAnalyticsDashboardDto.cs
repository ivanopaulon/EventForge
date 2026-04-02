namespace EventForge.DTOs.Analytics
{
    /// <summary>
    /// Dashboard data for pricing analytics.
    /// </summary>
    public class PricingAnalyticsDashboardDto
    {
        /// <summary>
        /// Total number of manual price overrides in the requested period.
        /// </summary>
        public int TotalManualOverrides { get; set; }

        /// <summary>
        /// Percentage of transactions resolved via automatic pricing (0-100).
        /// </summary>
        public double AutomaticPricingPercentage { get; set; }

        /// <summary>
        /// Top price lists by application frequency.
        /// </summary>
        public List<PriceListUsageItemDto> TopPriceLists { get; set; } = new();

        /// <summary>
        /// Monthly trend of manual price overrides.
        /// </summary>
        public List<AnalyticsTrendPointDto> ManualOverridesTrend { get; set; } = new();
    }

    /// <summary>
    /// Usage statistics for a single price list.
    /// </summary>
    public class PriceListUsageItemDto
    {
        /// <summary>Price list name.</summary>
        public string PriceListName { get; set; } = string.Empty;

        /// <summary>Number of times this price list was applied.</summary>
        public int TimesApplied { get; set; }

        /// <summary>Number of distinct documents that used this price list.</summary>
        public int DocumentCount { get; set; }

        /// <summary>Average discount percentage applied via this price list.</summary>
        public double AverageDiscount { get; set; }
    }
}
