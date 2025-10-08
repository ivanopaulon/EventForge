using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for creating a new document row.
    /// </summary>
    public class CreateDocumentRowDto
    {
        /// <summary>
        /// Reference to the document header.
        /// </summary>
        [Required(ErrorMessage = "The document header ID is required.")]
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Row type (Product, Discount, Service, Bundle, etc.).
        /// </summary>
        public DocumentRowType RowType { get; set; } = DocumentRowType.Product;

        /// <summary>
        /// Parent row ID (for bundles or grouping).
        /// </summary>
        public Guid? ParentRowId { get; set; }

        /// <summary>
        /// Product code (SKU, barcode, etc.).
        /// </summary>
        [StringLength(50, ErrorMessage = "Product code cannot exceed 50 characters.")]
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product identifier (for traceability and inventory operations).
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Storage location identifier (for inventory operations).
        /// </summary>
        public Guid? LocationId { get; set; }

        /// <summary>
        /// Product or service description.
        /// </summary>
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Unit of measure.
        /// </summary>
        [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters.")]
        public string? UnitOfMeasure { get; set; }

        /// <summary>
        /// Unit price.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
        public decimal UnitPrice { get; set; } = 0m;

        /// <summary>
        /// Quantity.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Line discount in percentage.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Line discount must be between 0 and 100.")]
        public decimal LineDiscount { get; set; } = 0m;

        /// <summary>
        /// VAT rate applied to the line (percentage).
        /// </summary>
        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100.")]
        public decimal VatRate { get; set; } = 0m;

        /// <summary>
        /// VAT description.
        /// </summary>
        [StringLength(30, ErrorMessage = "VAT description cannot exceed 30 characters.")]
        public string? VatDescription { get; set; }

        /// <summary>
        /// Indicates if the row is a gift.
        /// </summary>
        public bool IsGift { get; set; } = false;

        /// <summary>
        /// Indicates if the row was manually entered.
        /// </summary>
        public bool IsManual { get; set; } = false;

        /// <summary>
        /// Source warehouse for this row.
        /// </summary>
        public Guid? SourceWarehouseId { get; set; }

        /// <summary>
        /// Destination warehouse for this row.
        /// </summary>
        public Guid? DestinationWarehouseId { get; set; }

        /// <summary>
        /// Additional notes for the row.
        /// </summary>
        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Sort order for the row in the document.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Related station (optional, for logistics/traceability).
        /// </summary>
        public Guid? StationId { get; set; }
    }
}
