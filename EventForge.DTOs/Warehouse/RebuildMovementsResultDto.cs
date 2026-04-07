namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Result of the rebuild missing stock movements operation.
/// </summary>
public class RebuildMovementsResultDto
{
    /// <summary>
    /// Whether this was a dry-run (preview only, no movements created).
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Total number of approved/closed documents scanned.
    /// </summary>
    public int DocumentsScanned { get; set; }

    /// <summary>
    /// Total number of document rows scanned (non-deleted, with ProductId).
    /// </summary>
    public int RowsScanned { get; set; }

    /// <summary>
    /// Number of rows that already had a movement (skipped).
    /// </summary>
    public int RowsAlreadyHadMovement { get; set; }

    /// <summary>
    /// Number of rows skipped because no warehouse location could be determined.
    /// </summary>
    public int RowsSkippedNoLocation { get; set; }

    /// <summary>
    /// Number of rows for which a movement was created (or would be created in dry-run).
    /// </summary>
    public int MovementsCreated { get; set; }

    /// <summary>
    /// Number of rows for which an existing movement was updated (or would be updated in dry-run).
    /// </summary>
    public int MovementsUpdated { get; set; }

    /// <summary>
    /// Number of rows that encountered an error during movement creation.
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Detailed line items for the operation result.
    /// </summary>
    public List<RebuildMovementsRowResultDto> Items { get; set; } = new();
}

/// <summary>
/// Result for a single document row in the rebuild operation.
/// </summary>
public class RebuildMovementsRowResultDto
{
    public Guid DocumentHeaderId { get; set; }
    public string? DocumentNumber { get; set; }
    public Guid DocumentRowId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>
    /// Status of this row: "Created", "Updated", "AlreadyExists", "SkippedNoLocation", "Error".
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Direction of the movement: "Inbound" or "Outbound".
    /// </summary>
    public string? MovementType { get; set; }
}
