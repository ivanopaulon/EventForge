using System.Collections.Generic;

namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Detailed breakdown of supplier scoring factors.
    /// </summary>
    public class ScoreBreakdown
    {
        /// <summary>
        /// Price score (0-100).
        /// </summary>
        public decimal PriceScore { get; set; }

        /// <summary>
        /// Lead time score (0-100).
        /// </summary>
        public decimal LeadTimeScore { get; set; }

        /// <summary>
        /// Reliability score (0-100).
        /// </summary>
        public decimal ReliabilityScore { get; set; }

        /// <summary>
        /// Price trend score (0-100).
        /// </summary>
        public decimal TrendScore { get; set; }

        /// <summary>
        /// Explanations for each scoring factor.
        /// </summary>
        public Dictionary<string, string> Explanations { get; set; } = new();
    }
}
