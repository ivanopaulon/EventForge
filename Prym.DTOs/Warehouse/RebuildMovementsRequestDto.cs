namespace Prym.DTOs.Warehouse;

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

    /// <summary>
    /// Document approval statuses to include. If null or empty, defaults to Approved only.
    /// Values map to Data.Entities.Documents.ApprovalStatus: None=0, Pending=1, Approved=2, Rejected=3.
    /// </summary>
    public List<int>? ApprovalStatuses { get; set; }

    /// <summary>
    /// Document statuses to include. If null or empty, defaults to Closed only.
    /// Values map to Prym.DTOs.Common.DocumentStatus: Draft=0, Open=1, Closed=2, Cancelled=3.
    /// </summary>
    public List<int>? DocumentStatuses { get; set; }

    /// <summary>
    /// If true, existing movements linked to a document row are updated (date, quantity, type, location)
    /// instead of skipped. Default: true.
    /// </summary>
    public bool UpdateExisting { get; set; } = true;
}
