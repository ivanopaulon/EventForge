namespace EventForge.DTOs.Analytics
{
    /// <summary>
    /// Dashboard data for sales analytics.
    /// </summary>
    public class SalesAnalyticsDashboardDto
    {
        /// <summary>
        /// Total revenue in the requested period.
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total number of sales documents in the requested period.
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Average order value in the requested period.
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Top products by revenue within the requested period.
        /// </summary>
        public List<TopProductItemDto> TopProducts { get; set; } = new();

        /// <summary>
        /// Monthly revenue trend.
        /// </summary>
        public List<AnalyticsTrendPointDto> SalesTrend { get; set; } = new();
    }

    /// <summary>
    /// Sales statistics for a single product.
    /// </summary>
    public class TopProductItemDto
    {
        /// <summary>Rank in the top-N list (1-based).</summary>
        public int Rank { get; set; }

        /// <summary>Product name.</summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Total quantity sold.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Total revenue from this product.</summary>
        public decimal Revenue { get; set; }

        /// <summary>Total discount applied to this product.</summary>
        public decimal TotalDiscount { get; set; }
    }
}
