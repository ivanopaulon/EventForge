namespace EventForge.DTOs.Business
{
    /// <summary>
    /// DTO representing aggregated product analysis for a specific business party.
    /// Includes purchase and sale statistics, average prices, and last transaction dates.
    /// </summary>
    public class BusinessPartyProductAnalysisDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code (SKU).
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Total quantity purchased from/by this business party.
        /// </summary>
        public decimal QuantityPurchased { get; set; }

        /// <summary>
        /// Total net value of purchases (after discounts).
        /// </summary>
        public decimal ValuePurchased { get; set; }

        /// <summary>
        /// Total quantity sold to/by this business party.
        /// </summary>
        public decimal QuantitySold { get; set; }

        /// <summary>
        /// Total net value of sales (after discounts).
        /// </summary>
        public decimal ValueSold { get; set; }

        /// <summary>
        /// Date of last purchase transaction.
        /// </summary>
        public DateTime? LastPurchaseDate { get; set; }

        /// <summary>
        /// Date of last sale transaction.
        /// </summary>
        public DateTime? LastSaleDate { get; set; }

        /// <summary>
        /// Weighted average purchase price (considering quantities).
        /// </summary>
        public decimal AvgPurchasePrice { get; set; }

        /// <summary>
        /// Weighted average sale price (considering quantities).
        /// </summary>
        public decimal AvgSalePrice { get; set; }
    }
}
