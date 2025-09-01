using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a document template for creating standardized documents
/// </summary>
public class DocumentTemplate : AuditableEntity
{
    /// <summary>
    /// Name of the template
    /// </summary>
    [Required(ErrorMessage = "Template name is required.")]
    [StringLength(100, ErrorMessage = "Template name cannot exceed 100 characters.")]
    [Display(Name = "Template Name", Description = "Name of the template.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the template
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the template.")]
    public string? Description { get; set; }

    /// <summary>
    /// Document type this template is based on
    /// </summary>
    [Required(ErrorMessage = "Document type is required.")]
    [Display(Name = "Document Type", Description = "Document type this template is based on.")]
    public Guid DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Template category for organization
    /// </summary>
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Template category for organization.")]
    public string? Category { get; set; }

    /// <summary>
    /// Indicates if this template is available for all users or restricted
    /// </summary>
    [Display(Name = "Is Public", Description = "Indicates if this template is available for all users.")]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// User or role that owns this template (if not public)
    /// </summary>
    [StringLength(100, ErrorMessage = "Owner cannot exceed 100 characters.")]
    [Display(Name = "Owner", Description = "User or role that owns this template.")]
    public string? Owner { get; set; }

    /// <summary>
    /// JSON configuration for template fields and default values
    /// </summary>
    [Display(Name = "Template Configuration", Description = "JSON configuration for template fields and default values.")]
    public string? TemplateConfiguration { get; set; }

    /// <summary>
    /// Default business party for documents created from this template
    /// </summary>
    [Display(Name = "Default Business Party", Description = "Default business party for documents created from this template.")]
    public Guid? DefaultBusinessPartyId { get; set; }

    /// <summary>
    /// Default warehouse for documents created from this template
    /// </summary>
    [Display(Name = "Default Warehouse", Description = "Default warehouse for documents created from this template.")]
    public Guid? DefaultWarehouseId { get; set; }

    /// <summary>
    /// Default payment method for documents created from this template
    /// </summary>
    [StringLength(30, ErrorMessage = "Payment method cannot exceed 30 characters.")]
    [Display(Name = "Default Payment Method", Description = "Default payment method for documents created from this template.")]
    public string? DefaultPaymentMethod { get; set; }

    /// <summary>
    /// Default due date offset in days
    /// </summary>
    [Display(Name = "Default Due Date Days", Description = "Default due date offset in days.")]
    public int? DefaultDueDateDays { get; set; }

    /// <summary>
    /// Default notes to include in documents created from this template
    /// </summary>
    [StringLength(500, ErrorMessage = "Default notes cannot exceed 500 characters.")]
    [Display(Name = "Default Notes", Description = "Default notes to include in documents.")]
    public string? DefaultNotes { get; set; }

    /// <summary>
    /// Usage count for analytics
    /// </summary>
    [Display(Name = "Usage Count", Description = "Number of times this template has been used.")]
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Last time this template was used
    /// </summary>
    [Display(Name = "Last Used", Description = "Last time this template was used.")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Documents created from this template
    /// </summary>
    [Display(Name = "Created Documents", Description = "Documents created from this template.")]
    public ICollection<DocumentHeader> CreatedDocuments { get; set; } = new List<DocumentHeader>();

    /// <summary>
    /// Recurring document schedules based on this template
    /// </summary>
    [Display(Name = "Recurring Schedules", Description = "Recurring document schedules based on this template.")]
    public ICollection<DocumentRecurrence> RecurringSchedules { get; set; } = new List<DocumentRecurrence>();
}