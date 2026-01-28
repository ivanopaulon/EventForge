using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Request parameters for stock reconciliation calculation
    /// </summary>
    public class StockReconciliationRequestDto
    {
        /// <summary>
        /// Starting date for movement analysis (optional)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Ending date for movement analysis (optional)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by specific warehouse (optional)
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Filter by specific storage location (optional)
        /// </summary>
        public Guid? LocationId { get; set; }

        /// <summary>
        /// Filter by specific product (optional)
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Include document movements in calculation (default: true)
        /// </summary>
        public bool IncludeDocuments { get; set; } = true;

        /// <summary>
        /// Include inventory movements in calculation (default: true)
        /// </summary>
        public bool IncludeInventories { get; set; } = true;

        /// <summary>
        /// Only return items with discrepancies (default: false)
        /// </summary>
        public bool OnlyWithDiscrepancies { get; set; } = false;

        /// <summary>
        /// Starting quantity for calculation (optional, defaults to 0)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? StartingQuantity { get; set; }

        /// <summary>
        /// Threshold percentage for major discrepancies (default: 10%)
        /// </summary>
        [Range(0, 100)]
        public decimal DiscrepancyThreshold { get; set; } = 10m;
    }
}
