using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for updating an existing stock entry.
    /// </summary>
    public class UpdateStockDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
        public decimal? Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reserved quantity must be non-negative.")]
        public decimal? ReservedQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum level must be non-negative.")]
        public decimal? MinimumLevel { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum level must be non-negative.")]
        public decimal? MaximumLevel { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reorder point must be non-negative.")]
        public decimal? ReorderPoint { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reorder quantity must be non-negative.")]
        public decimal? ReorderQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative.")]
        public decimal? UnitCost { get; set; }

        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }
    }
}