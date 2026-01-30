namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Response containing detailed reliability metrics for a supplier.
    /// </summary>
    public class SupplierReliabilityResponse
    {
        /// <summary>
        /// Supplier identifier.
        /// </summary>
        public Guid SupplierId { get; set; }

        /// <summary>
        /// Supplier name.
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// Overall reliability score (0-100).
        /// </summary>
        public decimal OverallReliabilityScore { get; set; }

        /// <summary>
        /// Detailed reliability metrics.
        /// </summary>
        public ReliabilityMetrics Metrics { get; set; } = new();
    }
}
