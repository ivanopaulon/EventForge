namespace EventForge.DTOs.Warehouse;

/// <summary>
/// DTO for merging multiple inventory documents into one.
/// </summary>
public class MergeInventoryDocumentsDto
{
    /// <summary>
    /// List of source document IDs to merge (minimum 2 required).
    /// </summary>
    public List<Guid> SourceDocumentIds { get; set; } = new();

    /// <summary>
    /// Optional: ID of the document to use as the merge target (base document).
    /// If null, the first document in SourceDocumentIds is used as base.
    /// The target document becomes the resulting merged document (Finalized).
    /// All other source documents will be soft-deleted.
    /// </summary>
    public Guid? TargetDocumentId { get; set; }

    /// <summary>
    /// Optional notes for the merged document.
    /// </summary>
    public string? Notes { get; set; }
}
