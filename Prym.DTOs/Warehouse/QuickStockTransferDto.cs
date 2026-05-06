using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Warehouse
{
    /// <summary>
    /// Request DTO for a quick stock transfer between two locations.
    /// </summary>
    public class QuickStockTransferDto
    {
        /// <summary>
        /// Product to transfer.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Source storage location.
        /// </summary>
        [Required]
        public Guid FromLocationId { get; set; }

        /// <summary>
        /// Destination storage location.
        /// </summary>
        [Required]
        public Guid ToLocationId { get; set; }

        /// <summary>
        /// Quantity to transfer (must be > 0).
        /// </summary>
        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Optional lot ID.
        /// </summary>
        public Guid? LotId { get; set; }

        /// <summary>
        /// Optional notes for the movement.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
