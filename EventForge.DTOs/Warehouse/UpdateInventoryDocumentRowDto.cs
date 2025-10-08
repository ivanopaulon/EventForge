using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for updating an inventory document row.
    /// Allows modification of quantity and notes for a counted item.
    /// </summary>
    public class UpdateInventoryDocumentRowDto
    {
        /// <summary>
        /// Quantity counted during inventory.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Notes for this inventory row.
        /// </summary>
        [StringLength(200)]
        public string? Notes { get; set; }
    }
}
