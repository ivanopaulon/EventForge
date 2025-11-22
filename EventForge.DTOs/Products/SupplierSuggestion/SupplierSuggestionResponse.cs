using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Response DTO containing ranked supplier suggestions for a product.
    /// </summary>
    public class SupplierSuggestionResponse
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code.
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// List of supplier suggestions ranked by score.
        /// </summary>
        public List<SupplierSuggestion> Suggestions { get; set; } = new();

        /// <summary>
        /// The top recommended supplier.
        /// </summary>
        public SupplierSuggestion? RecommendedSupplier { get; set; }

        /// <summary>
        /// Potential savings if switching to recommended supplier.
        /// </summary>
        public decimal PotentialSavings { get; set; }

        /// <summary>
        /// AI-generated explanation for the recommendation.
        /// </summary>
        public string? RecommendationExplanation { get; set; }
    }
}
