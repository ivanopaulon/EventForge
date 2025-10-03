using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for inventory document output/display operations.
    /// </summary>
    public class InventoryDocumentDto
    {
        /// <summary>
        /// Unique identifier for the inventory document.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Document number.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Document series.
        /// </summary>
        public string? Series { get; set; }

        /// <summary>
        /// Date of the inventory.
        /// </summary>
        public DateTime InventoryDate { get; set; }

        /// <summary>
        /// Warehouse ID for the inventory.
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Warehouse name for display.
        /// </summary>
        public string? WarehouseName { get; set; }

        /// <summary>
        /// Status of the document.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Notes for the inventory document.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the document was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the document.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the document was finalized (UTC).
        /// </summary>
        public DateTime? FinalizedAt { get; set; }

        /// <summary>
        /// User who finalized the document.
        /// </summary>
        public string? FinalizedBy { get; set; }

        /// <summary>
        /// List of inventory rows.
        /// </summary>
        public List<InventoryDocumentRowDto> Rows { get; set; } = new List<InventoryDocumentRowDto>();

        /// <summary>
        /// Total number of items in the inventory.
        /// </summary>
        public int TotalItems => Rows.Count;
    }
}
