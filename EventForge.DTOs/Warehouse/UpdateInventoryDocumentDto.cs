using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for updating an inventory document header (metadata only).
    /// Can only update Draft documents.
    /// </summary>
    public class UpdateInventoryDocumentDto
    {
        /// <summary>
        /// Date of the inventory.
        /// </summary>
        [Required]
        public DateTime InventoryDate { get; set; }

        /// <summary>
        /// Optional warehouse ID for the inventory.
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Notes for the inventory document.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
