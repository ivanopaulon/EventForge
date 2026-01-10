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
    /// Optional notes for the merged document.
    /// </summary>
    public string? Notes { get; set; }
}
