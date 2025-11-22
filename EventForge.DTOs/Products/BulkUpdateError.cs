using System;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// Represents an error that occurred during bulk update for a specific product.
    /// </summary>
    public class BulkUpdateError
    {
        /// <summary>
        /// Product ID that failed.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name for display.
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Error message describing what went wrong.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
