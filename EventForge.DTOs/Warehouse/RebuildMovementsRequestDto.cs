namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Request parameters for rebuilding missing stock movements from documents.
/// </summary>
public class RebuildMovementsRequestDto
{
    /// <summary>
    /// Optional: filter by document date from (UTC).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Optional: filter by document date to (UTC).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Optional: filter by warehouse ID (SourceWarehouseId or DestinationWarehouseId on document header).
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Optional: filter by document type ID.
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// If true, only preview what would be created without actually creating movements.
    /// </summary>
    public bool DryRun { get; set; } = false;
}
