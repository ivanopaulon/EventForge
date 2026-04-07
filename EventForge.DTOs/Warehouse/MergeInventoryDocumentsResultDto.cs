namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Result of a merge operation between multiple inventory documents.
/// </summary>
public class MergeInventoryDocumentsResultDto
{
    /// <summary>
    /// The resulting merged inventory document ID.
    /// </summary>
    public Guid MergedDocumentId { get; set; }

    /// <summary>
    /// Number of the resulting merged document.
    /// </summary>
    public string MergedDocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// IDs of the source documents that were soft-deleted after merge.
    /// </summary>
    public List<Guid> SoftDeletedDocumentIds { get; set; } = new();

    /// <summary>
    /// Total rows in the resulting merged document.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of rows merged (quantities summed from different documents).
    /// </summary>
    public int MergedRows { get; set; }

    /// <summary>
    /// Number of rows simply copied (no duplicates found).
    /// </summary>
    public int CopiedRows { get; set; }

    /// <summary>
    /// Warnings generated during the merge (e.g. LotId conflicts).
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
