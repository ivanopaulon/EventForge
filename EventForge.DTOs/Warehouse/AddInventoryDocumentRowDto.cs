using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for adding a row to an inventory document.
    /// Represents a single product count in the inventory.
    /// </summary>
    public class AddInventoryDocumentRowDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Storage location identifier.
        /// </summary>
        [Required]
        public Guid LocationId { get; set; }

        /// <summary>
        /// Quantity counted during inventory.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Lot identifier (optional).
        /// </summary>
        public Guid? LotId { get; set; }

        /// <summary>
        /// Unit of measure identifier (optional).
        /// When provided, enables proper quantity conversion for alternative units.
        /// </summary>
        public Guid? UnitOfMeasureId { get; set; }

        /// <summary>
        /// Notes for this inventory row.
        /// </summary>
        [StringLength(200)]
        public string? Notes { get; set; }
    }
}
