namespace Prym.DTOs.Warehouse;

/// <summary>
/// Preview of a merge operation: what would happen if the selected documents were merged.
/// Used for user confirmation before executing the actual merge.
/// </summary>
public class MergeInventoryDocumentsPreviewDto
{
    /// <summary>
    /// List of source documents selected for merge.
    /// </summary>
    public List<MergeSourceDocumentSummaryDto> SourceDocuments { get; set; } = new();

    /// <summary>
    /// Total number of input rows across all source documents.
    /// </summary>
    public int TotalInputRows { get; set; }

    /// <summary>
    /// Estimated total rows in the resulting document (after merge).
    /// </summary>
    public int EstimatedOutputRows { get; set; }

    /// <summary>
    /// Number of rows that will be merged (quantities summed).
    /// </summary>
    public int RowsToMerge { get; set; }

    /// <summary>
    /// Number of rows that will be simply copied (no duplicates).
    /// </summary>
    public int RowsToCopy { get; set; }

    /// <summary>
    /// Warnings about potential issues (e.g. rows with different warehouses).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Whether all source documents belong to the same warehouse.
    /// </summary>
    public bool SameWarehouse { get; set; }

    /// <summary>
    /// Warehouse IDs found across all selected documents.
    /// </summary>
    public List<Guid?> WarehouseIds { get; set; } = new();
}

/// <summary>
/// Summary of a single source document in a merge preview.
/// </summary>
public class MergeSourceDocumentSummaryDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime InventoryDate { get; set; }
}
