using System;

namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Request to apply a suggested supplier as preferred.
    /// </summary>
    public class ApplySuggestionRequest
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Supplier identifier to set as preferred.
        /// </summary>
        public Guid SupplierId { get; set; }

        /// <summary>
        /// Reason for applying this suggestion.
        /// </summary>
        public string? Reason { get; set; }
    }
}
