using System;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO representing a product's movement within a document.
    /// Contains document information plus the specific quantity and details for the product.
    /// </summary>
    public class ProductDocumentMovementDto
    {
        /// <summary>
        /// Document header identifier.
        /// </summary>
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Document number.
        /// </summary>
        public string DocumentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Document date.
        /// </summary>
        public DateTime DocumentDate { get; set; }

        /// <summary>
        /// Type of document (e.g., "Invoice", "Delivery Note").
        /// </summary>
        public string DocumentTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Business party name (customer or supplier).
        /// </summary>
        public string? BusinessPartyName { get; set; }

        /// <summary>
        /// Document status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product moved in this document.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit of measure for the quantity.
        /// </summary>
        public string? UnitOfMeasure { get; set; }

        /// <summary>
        /// Unit price of the product in this document.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total value for this product line in the document.
        /// </summary>
        public decimal LineTotal { get; set; }

        /// <summary>
        /// Indicates if this movement increases stock (true) or decreases stock (false).
        /// </summary>
        public bool IsStockIncrease { get; set; }

        /// <summary>
        /// Warehouse identifier (source or destination depending on movement type).
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Warehouse name for display.
        /// </summary>
        public string? WarehouseName { get; set; }
    }
}
