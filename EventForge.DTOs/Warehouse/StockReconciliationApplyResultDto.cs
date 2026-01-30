namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Result of applying stock reconciliation
    /// </summary>
    public class StockReconciliationApplyResultDto
    {
        /// <summary>
        /// Number of stock records successfully updated
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Number of adjustment movements created
        /// </summary>
        public int MovementsCreated { get; set; }

        /// <summary>
        /// Total absolute value of adjustments made
        /// </summary>
        public decimal TotalAdjustmentValue { get; set; }

        /// <summary>
        /// List of Stock IDs that were updated
        /// </summary>
        public List<Guid> UpdatedStockIds { get; set; } = new();

        /// <summary>
        /// Success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if unsuccessful
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
