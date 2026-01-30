using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for adjusting stock quantities.
    /// </summary>
    public class AdjustStockDto
    {
        [Required]
        public Guid StockId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid StorageLocationId { get; set; }

        [Required]
        [Range(0, 999999999)]
        public decimal NewQuantity { get; set; }

        [Required]
        [Range(0, 999999999)]
        public decimal PreviousQuantity { get; set; }

        [Required]
        public StockAdjustmentReason Reason { get; set; }

        public string? Notes { get; set; }

        /// <summary>
        /// Indicates if this adjustment requires full audit trail.
        /// </summary>
        public bool RequiresAudit { get; set; }
    }
}
