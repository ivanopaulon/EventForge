using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for DocumentTemplate output/display operations
    /// </summary>
    public class DocumentTemplateDto
    {
        /// <summary>
        /// Unique identifier for the template
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the template
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the template
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Document type this template is based on
        /// </summary>
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Document type name for display
        /// </summary>
        public string? DocumentTypeName { get; set; }

        /// <summary>
        /// Template category for organization
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Indicates if this template is available for all users
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// User or role that owns this template
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// JSON configuration for template fields and default values
        /// </summary>
        public string? TemplateConfiguration { get; set; }

        /// <summary>
        /// Default business party for documents created from this template
        /// </summary>
        public Guid? DefaultBusinessPartyId { get; set; }

        /// <summary>
        /// Default warehouse for documents created from this template
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }

        /// <summary>
        /// Default payment method for documents created from this template
        /// </summary>
        public string? DefaultPaymentMethod { get; set; }

        /// <summary>
        /// Default due date offset in days
        /// </summary>
        public int? DefaultDueDateDays { get; set; }

        /// <summary>
        /// Default notes to include in documents created from this template
        /// </summary>
        public string? DefaultNotes { get; set; }

        /// <summary>
        /// Usage count for analytics
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Last time this template was used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Date and time when the template was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the template
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the template was last modified (UTC)
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the template
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates whether the template is active
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document template
    /// </summary>
    public class CreateDocumentTemplateDto
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        [Required(ErrorMessage = "Template name is required.")]
        [StringLength(100, ErrorMessage = "Template name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the template
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Document type this template is based on
        /// </summary>
        [Required(ErrorMessage = "Document type is required.")]
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Template category for organization
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Indicates if this template is available for all users
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// User or role that owns this template
        /// </summary>
        [StringLength(100, ErrorMessage = "Owner cannot exceed 100 characters.")]
        public string? Owner { get; set; }

        /// <summary>
        /// JSON configuration for template fields and default values
        /// </summary>
        public string? TemplateConfiguration { get; set; }

        /// <summary>
        /// Default business party for documents created from this template
        /// </summary>
        public Guid? DefaultBusinessPartyId { get; set; }

        /// <summary>
        /// Default warehouse for documents created from this template
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }

        /// <summary>
        /// Default payment method for documents created from this template
        /// </summary>
        [StringLength(30, ErrorMessage = "Payment method cannot exceed 30 characters.")]
        public string? DefaultPaymentMethod { get; set; }

        /// <summary>
        /// Default due date offset in days
        /// </summary>
        public int? DefaultDueDateDays { get; set; }

        /// <summary>
        /// Default notes to include in documents created from this template
        /// </summary>
        [StringLength(500, ErrorMessage = "Default notes cannot exceed 500 characters.")]
        public string? DefaultNotes { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing document template
    /// </summary>
    public class UpdateDocumentTemplateDto
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        [Required(ErrorMessage = "Template name is required.")]
        [StringLength(100, ErrorMessage = "Template name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the template
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Template category for organization
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Indicates if this template is available for all users
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// User or role that owns this template
        /// </summary>
        [StringLength(100, ErrorMessage = "Owner cannot exceed 100 characters.")]
        public string? Owner { get; set; }

        /// <summary>
        /// JSON configuration for template fields and default values
        /// </summary>
        public string? TemplateConfiguration { get; set; }

        /// <summary>
        /// Default business party for documents created from this template
        /// </summary>
        public Guid? DefaultBusinessPartyId { get; set; }

        /// <summary>
        /// Default warehouse for documents created from this template
        /// </summary>
        public Guid? DefaultWarehouseId { get; set; }

        /// <summary>
        /// Default payment method for documents created from this template
        /// </summary>
        [StringLength(30, ErrorMessage = "Payment method cannot exceed 30 characters.")]
        public string? DefaultPaymentMethod { get; set; }

        /// <summary>
        /// Default due date offset in days
        /// </summary>
        public int? DefaultDueDateDays { get; set; }

        /// <summary>
        /// Default notes to include in documents created from this template
        /// </summary>
        [StringLength(500, ErrorMessage = "Default notes cannot exceed 500 characters.")]
        public string? DefaultNotes { get; set; }

        /// <summary>
        /// Indicates whether the template is active
        /// </summary>
        public bool IsActive { get; set; }
    }
}