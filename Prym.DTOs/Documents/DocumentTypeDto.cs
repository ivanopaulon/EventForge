using Prym.DTOs.Common;

namespace Prym.DTOs.Documents
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
        /// Indicates if this document type represents a physical inventory count
        /// </summary>
        public bool IsInventoryDocument { get; set; }

        /// <summary>
        /// Indicates if approving/closing a document of this type should generate warehouse stock movements.
        /// Always false for inventory documents.
        /// </summary>
        public bool CreatesStockMovements { get; set; } = true;

        /// <summary>
        /// Indicates if a stock movement is created/updated/deleted immediately on every document row change,
        /// regardless of document status. When true, CreatesStockMovements is forced to false.
        /// </summary>
        public bool MovesStockOnRowChange { get; set; } = true;

        /// <summary>
        /// When true, this document type represents a stock transfer between two warehouses.
        /// The rebuild procedure generates both Outbound (source) and Inbound (destination) movements.
        /// </summary>
        public bool IsTransferDocument { get; set; }

        /// <summary>
        /// Default movement reason used during the rebuild procedure.
        /// Null = legacy heuristic (Purchase for inbound, Sale for outbound).
        /// </summary>
        public string? DefaultMovementReason { get; set; }

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
