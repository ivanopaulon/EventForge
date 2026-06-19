using Prym.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Documents
{

    /// <summary>
    /// DTO for creating a new document type
    /// </summary>
    public class CreateDocumentTypeDto
    {
        /// <summary>
        /// Name of the document type
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Code of the document type
        /// </summary>
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this document type increases warehouse stock
        /// </summary>
        public bool IsStockIncrease { get; set; }

        /// <summary>
        /// Indicates if this document type represents a physical inventory count.
        /// When true, CreatesStockMovements is forced to false.
        /// </summary>
        public bool IsInventoryDocument { get; set; }

        /// <summary>
        /// Indicates if approving/closing a document of this type should generate warehouse stock movements.
        /// Defaults to true; automatically set to false for inventory document types.
        /// </summary>
        public bool CreatesStockMovements { get; set; } = true;

        /// <summary>
        /// Indicates if a stock movement is created/updated/deleted immediately on every document row change,
        /// regardless of document status. When true, CreatesStockMovements is forced to false.
        /// Incompatible with IsInventoryDocument = true.
        /// </summary>
        public bool MovesStockOnRowChange { get; set; } = true;

        /// <summary>
        /// Default warehouse for this document type
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }

        /// <summary>
        /// Indicates if the document is fiscal
        /// </summary>
        public bool IsFiscal { get; set; }

        /// <summary>
        /// Required business party type for this document (Customer, Supplier, or Both)
        /// </summary>
        public BusinessPartyType RequiredPartyType { get; set; } = BusinessPartyType.Both;

        /// <summary>
        /// Additional notes or description
        /// </summary>
        [StringLength(200)]
        public string? Notes { get; set; }
    }
}
