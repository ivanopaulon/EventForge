using System;

namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Represents a single supplier suggestion with scoring details.
    /// </summary>
    public class SupplierSuggestion
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
        /// Indicates if this is the currently preferred supplier.
        /// </summary>
        public bool IsCurrentPreferred { get; set; }

        /// <summary>
        /// Total weighted score (0-100).
        /// </summary>
        public decimal TotalScore { get; set; }

        /// <summary>
        /// Detailed breakdown of score components.
        /// </summary>
        public ScoreBreakdown ScoreBreakdown { get; set; } = new();

        /// <summary>
        /// Current unit cost from this supplier.
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Currency for the unit cost.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Lead time in days.
        /// </summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// Confidence level for this recommendation.
        /// </summary>
        public ConfidenceLevel Confidence { get; set; }

        /// <summary>
        /// Short explanation of why this supplier is recommended.
        /// </summary>
        public string? RecommendationReason { get; set; }
    }
}
