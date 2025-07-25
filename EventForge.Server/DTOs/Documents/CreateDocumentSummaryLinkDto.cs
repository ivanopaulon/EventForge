using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Documents;

/// <summary>
/// DTO for creating a new document summary link.
/// </summary>
public class CreateDocumentSummaryLinkDto
{
    /// <summary>
    /// ID of the summary document (e.g., invoice).
    /// </summary>
    [Required(ErrorMessage = "The summary document ID is required.")]
    public Guid SummaryDocumentId { get; set; }

    /// <summary>
    /// ID of the detailed document.
    /// </summary>
    public Guid? DetailedDocumentId { get; set; }
}