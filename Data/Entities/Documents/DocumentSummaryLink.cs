using System.ComponentModel.DataAnnotations;

/// <summary>
/// Link between a summary document (e.g., invoice) and a detailed document (e.g., delivery note).
/// </summary>
public class DocumentSummaryLink : AuditableEntity
{
    /// <summary>
    /// ID of the summary document (e.g., invoice).
    /// </summary>
    [Required(ErrorMessage = "The summary document ID is required.")]
    [Display(Name = "Summary Document", Description = "ID of the summary document (e.g., invoice).")]
    public Guid SummaryDocumentId { get; set; }

    /// <summary>
    /// Navigation property for the summary document.
    /// </summary>
    [Display(Name = "Summary Document", Description = "Navigation property for the summary document.")]
    public DocumentHeader? SummaryDocument { get; set; }

    /// <summary>
    /// Navigation property for the detailed document.
    /// </summary>
    [Display(Name = "Detailed Document", Description = "Navigation property for the detailed document.")]
    public DocumentHeader? DetailedDocument { get; set; }
}