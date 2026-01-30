namespace EventForge.DTOs.Products
{
    /// <summary>
    /// Request DTO for bulk updating supplier products.
    /// </summary>
    public class BulkUpdateSupplierProductsRequest
    {
        /// <summary>
        /// List of product IDs to update.
        /// </summary>
        public List<System.Guid> ProductIds { get; set; } = new();

        /// <summary>
        /// Mode for updating unit cost (Set, Increase, Decrease, PercentageIncrease, PercentageDecrease).
        /// </summary>
        public UpdateMode? UpdateMode { get; set; }

        /// <summary>
        /// Value to use for the unit cost update (depends on UpdateMode).
        /// </summary>
        public decimal? UnitCostValue { get; set; }

        /// <summary>
        /// Lead time in days (null to leave unchanged).
        /// </summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// Currency code (null to leave unchanged).
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Minimum order quantity (null to leave unchanged).
        /// </summary>
        public int? MinOrderQuantity { get; set; }

        /// <summary>
        /// Set as preferred supplier (null to leave unchanged).
        /// </summary>
        public bool? IsPreferred { get; set; }
    }
}
