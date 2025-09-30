using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for creating a new inventory entry.
    /// </summary>
    public class CreateInventoryEntryDto
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
        /// Notes for the inventory entry.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
