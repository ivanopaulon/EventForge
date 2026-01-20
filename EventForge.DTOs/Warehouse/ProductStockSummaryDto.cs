using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Summary of stock information for a product across all locations.
    /// Used in the aggregated view of the stock overview.
    /// </summary>
    public class ProductStockSummaryDto
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }

        // Aggregate totals
        public decimal TotalStock { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalAvailable => TotalStock - TotalReserved;

        // Reorder parameters (from Product)
        public decimal? ReorderPoint { get; set; }
        public decimal? SafetyStock { get; set; }
        public decimal? TargetStockLevel { get; set; }

        // Calculated status
        public StockStatus Status { get; set; }

        // Count metrics
        public int WarehouseCount { get; set; }
        public int LocationCount { get; set; }
        public int LotCount { get; set; }

        // Detailed breakdown (only populated in detailed view)
        public List<StockLocationDetail>? LocationDetails { get; set; }
    }
}
