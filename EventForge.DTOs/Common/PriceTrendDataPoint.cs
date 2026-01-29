using System;

namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Unified DTO representing a single data point in a price trend chart.
    /// Supports both supplier price history tracking and document-based price analysis.
    /// </summary>
    public class PriceTrendDataPoint
    {
        /// <summary>
        /// Date of the price data point.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Price at this date.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity associated with this price point (for document-based analysis).
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Document type that generated this price point (e.g., "Invoice", "Purchase Order").
        /// Used in document-based price trend analysis.
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Business party name (customer or supplier) associated with this price.
        /// Used in document-based price trend analysis.
        /// </summary>
        public string? BusinessPartyName { get; set; }

        /// <summary>
        /// Source of the price change (Manual, BulkEdit, CSVImport, AutoUpdate).
        /// Used in supplier price history tracking.
        /// </summary>
        public string? ChangeSource { get; set; }

        /// <summary>
        /// Currency code for the price.
        /// </summary>
        public string? Currency { get; set; }
    }
}
