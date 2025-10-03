using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for creating a new inventory document.
    /// Simplified version for starting an inventory session.
    /// </summary>
    public class CreateInventoryDocumentDto
    {
        /// <summary>
        /// Optional warehouse ID for the inventory.
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Date of the inventory.
        /// </summary>
        [Required]
        public DateTime InventoryDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Notes for the inventory document.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Document series for progressive numbering.
        /// </summary>
        [StringLength(10)]
        public string? Series { get; set; }

        /// <summary>
        /// Document number (optional - can be auto-generated).
        /// </summary>
        [StringLength(30)]
        public string? Number { get; set; }
    }
}
