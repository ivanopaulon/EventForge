using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for creating or updating stock entries with validation rules.
    /// If StockId is provided, it's an update operation with restrictions.
    /// If StockId is null/empty, it's a creation operation.
    /// </summary>
    public class CreateOrUpdateStockDto
    {
        /// <summary>
        /// Stock ID for update operations. Null/Empty for new stock creation.
        /// </summary>
        public Guid? StockId { get; set; }

        [Required(ErrorMessage = "Product is required.")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Warehouse ID. Required for new stock. Cannot be changed for existing stock.
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Storage location ID. Required for new stock. Cannot be changed for existing stock.
        /// </summary>
        [Required(ErrorMessage = "Storage location is required.")]
        public Guid StorageLocationId { get; set; }

        public Guid? LotId { get; set; }

        /// <summary>
        /// New quantity for the stock.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
        public decimal NewQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reserved quantity must be non-negative.")]
        public decimal ReservedQuantity { get; set; } = 0;

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
