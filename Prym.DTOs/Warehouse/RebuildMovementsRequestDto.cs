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
    /// Document statuses to include. If null or empty, defaults to Archived.
    /// Values map to Prym.DTOs.Common.DocumentStatus: Active=1, Archived=4.
    /// </summary>
    public List<int>? DocumentStatuses { get; set; }

    /// <summary>
    /// If true, existing movements linked to a document row are updated (date, quantity, type, location)
    /// instead of skipped. Default: true.
    /// </summary>
    public bool UpdateExisting { get; set; } = true;

    /// <summary>
    /// When true, Phase 3 of the rebuild overwrites the quantity of every existing Stock row
    /// for affected product/location pairs by computing the net from the full movement history
    /// (inbound − outbound, using <see cref="Math.Abs"/> for legacy negative-quantity rows).
    /// Use this flag to correct stock balances that were already wrong before the rebuild.
    /// Default: false (safe mode — existing balances are only adjusted by the movement delta).
    /// </summary>
    public bool ForceRecalculateFromMovements { get; set; } = false;

    /// <summary>
    /// Opening-balance quantity added to the movement-net when
    /// <see cref="ForceRecalculateFromMovements"/> is <c>true</c>.
    /// Useful when the movement history does not start from zero (e.g. the system was migrated
    /// with an initial stock balance that has no corresponding movement).
    /// Applied uniformly to every affected product/location pair, so use only when all pairs
    /// share the same starting point (e.g. a single-product corrective run).
    /// Default: 0.
    /// </summary>
    public decimal StartingQuantity { get; set; } = 0m;

    /// <summary>
    /// Number of movements processed per inner save chunk in Phase 2b.
    /// Lower values reduce peak memory consumption and DB lock contention at the cost of
    /// more round-trips. Valid range: 10–500. Default: 100.
    /// </summary>
    public int BatchChunkSize { get; set; } = 100;
}
