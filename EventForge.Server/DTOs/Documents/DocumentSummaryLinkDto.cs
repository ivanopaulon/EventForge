namespace EventForge.Server.DTOs.Documents;

/// <summary>
/// DTO for DocumentSummaryLink output/display operations.
/// </summary>
public class DocumentSummaryLinkDto
{
    /// <summary>
    /// Unique identifier for the document summary link.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the summary document (e.g., invoice).
    /// </summary>
    public Guid SummaryDocumentId { get; set; }

    /// <summary>
    /// Summary document number for display.
    /// </summary>
    public string? SummaryDocumentNumber { get; set; }

    /// <summary>
    /// Summary document date for display.
    /// </summary>
    public DateTime? SummaryDocumentDate { get; set; }

    /// <summary>
    /// ID of the detailed document.
    /// </summary>
    public Guid? DetailedDocumentId { get; set; }

    /// <summary>
    /// Detailed document number for display.
    /// </summary>
    public string? DetailedDocumentNumber { get; set; }

    /// <summary>
    /// Detailed document date for display.
    /// </summary>
    public DateTime? DetailedDocumentDate { get; set; }

    /// <summary>
    /// Date and time when the link was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the link.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the link was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the link.
    /// </summary>
    public string? ModifiedBy { get; set; }
}