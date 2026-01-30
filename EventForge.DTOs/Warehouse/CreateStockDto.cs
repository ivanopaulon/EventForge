using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for creating a new stock entry.
    /// </summary>
    public class CreateStockDto
    {
        [Required(ErrorMessage = "Product is required.")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Storage location is required.")]
        public Guid StorageLocationId { get; set; }

        public Guid? LotId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
        public decimal Quantity { get; set; }

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