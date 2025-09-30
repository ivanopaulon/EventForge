using System;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for inventory entry output/display operations.
    /// </summary>
    public class InventoryEntryDto
    {
        /// <summary>
        /// Unique identifier for the inventory entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Product code.
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Storage location identifier.
        /// </summary>
        public Guid LocationId { get; set; }

        /// <summary>
        /// Storage location name.
        /// </summary>
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity counted during inventory.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Lot identifier (if applicable).
        /// </summary>
        public Guid? LotId { get; set; }

        /// <summary>
        /// Lot code (if applicable).
        /// </summary>
        public string? LotCode { get; set; }

        /// <summary>
        /// Notes for the inventory entry.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the entry was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the entry.
        /// </summary>
        public string? CreatedBy { get; set; }
    }
}
