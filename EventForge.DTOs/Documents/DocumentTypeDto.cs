using System;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for DocumentType output/display operations.
    /// </summary>
    public class DocumentTypeDto
    {
        /// <summary>
        /// Unique identifier for the document type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the document type.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Code of the document type.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this document type increases warehouse stock.
        /// </summary>
        public bool IsStockIncrease { get; set; }

        /// <summary>
        /// Default warehouse for this document type.
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }

        /// <summary>
        /// Default warehouse name for display.
        /// </summary>
        public string? DefaultWarehouseName { get; set; }

        /// <summary>
        /// Indicates if the document is fiscal.
        /// </summary>
        public bool IsFiscal { get; set; }

        /// <summary>
        /// Required business party type for this document (Customer, Supplier, or Both).
        /// </summary>
        public BusinessPartyType RequiredPartyType { get; set; }

        /// <summary>
        /// Additional notes or description.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the document type was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the document type.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the document type was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the document type.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
