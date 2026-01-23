using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Represents a single stock item in the reconciliation result
    /// </summary>
    public class StockReconciliationItemDto
    {
        /// <summary>
        /// Stock ID
        /// </summary>
        public Guid StockId { get; set; }

        /// <summary>
        /// Product ID
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Warehouse name
        /// </summary>
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// Storage location code
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// Current quantity in stock table
        /// </summary>
        public decimal CurrentQuantity { get; set; }

        /// <summary>
        /// Calculated quantity based on movements
        /// </summary>
        public decimal CalculatedQuantity { get; set; }

        /// <summary>
        /// Difference between calculated and current (CalculatedQuantity - CurrentQuantity)
        /// </summary>
        public decimal Difference { get; set; }

        /// <summary>
        /// Difference percentage relative to calculated quantity
        /// </summary>
        public decimal DifferencePercentage { get; set; }

        /// <summary>
        /// Severity level of the discrepancy
        /// </summary>
        public ReconciliationSeverity Severity { get; set; }

        /// <summary>
        /// List of source movements that contributed to the calculated quantity
        /// </summary>
        public List<StockMovementSourceDto> SourceMovements { get; set; } = new();

        /// <summary>
        /// Total number of document movements
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Total number of inventory movements
        /// </summary>
        public int TotalInventories { get; set; }

        /// <summary>
        /// Total number of manual movements
        /// </summary>
        public int TotalManualMovements { get; set; }
    }
}
