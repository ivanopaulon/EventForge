using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO representing price trend data for a product over a period.
    /// Tracks purchase and sale prices from document movements.
    /// </summary>
    public class PriceTrendDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Year for which the trend data is provided.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// List of data points representing purchase prices over time.
        /// </summary>
        public List<PriceTrendDataPoint> PurchasePrices { get; set; } = new List<PriceTrendDataPoint>();

        /// <summary>
        /// List of data points representing sale prices over time.
        /// </summary>
        public List<PriceTrendDataPoint> SalePrices { get; set; } = new List<PriceTrendDataPoint>();

        /// <summary>
        /// Current average purchase price.
        /// </summary>
        public decimal CurrentAveragePurchasePrice { get; set; }

        /// <summary>
        /// Current average sale price.
        /// </summary>
        public decimal CurrentAverageSalePrice { get; set; }

        /// <summary>
        /// Minimum purchase price during the period.
        /// </summary>
        public decimal MinPurchasePrice { get; set; }

        /// <summary>
        /// Maximum purchase price during the period.
        /// </summary>
        public decimal MaxPurchasePrice { get; set; }

        /// <summary>
        /// Minimum sale price during the period.
        /// </summary>
        public decimal MinSalePrice { get; set; }

        /// <summary>
        /// Maximum sale price during the period.
        /// </summary>
        public decimal MaxSalePrice { get; set; }

        /// <summary>
        /// Average purchase price during the period.
        /// </summary>
        public decimal AveragePurchasePrice { get; set; }

        /// <summary>
        /// Average sale price during the period.
        /// </summary>
        public decimal AverageSalePrice { get; set; }
    }

    /// <summary>
    /// Data point representing a price at a specific date.
    /// </summary>
    public class PriceTrendDataPoint
    {
        /// <summary>
        /// Date of the data point.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Price at this date.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity associated with this price point.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Document type that generated this price point (e.g., "Invoice", "Purchase Order").
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Business party name (customer or supplier).
        /// </summary>
        public string? BusinessPartyName { get; set; }
    }
}
