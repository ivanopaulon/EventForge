using System;
using System.ComponentModel.DataAnnotations;

using EventForge.DTOs.Common;
namespace EventForge.DTOs.Documents
{
    
    /// <summary>
    /// DTO for updating an existing document type
    /// </summary>
    public class UpdateDocumentTypeDto
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
        /// Default warehouse for this document type
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }
    
        /// <summary>
        /// Indicates if the document is fiscal
        /// </summary>
        public bool IsFiscal { get; set; }
    
        /// <summary>
        /// Additional notes or description
        /// </summary>
        [StringLength(200)]
        public string? Notes { get; set; }
    }
}
