namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// DTO representing a single price history entry.
/// </summary>
public class PriceHistoryItem
{
    /// <summary>
    /// Price history entry identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product code.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Supplier name.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Old unit cost before the change.
    /// </summary>
    public decimal OldPrice { get; set; }

    /// <summary>
    /// New unit cost after the change.
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Absolute price change (NewPrice - OldPrice).
    /// </summary>
    public decimal PriceChange { get; set; }

    /// <summary>
    /// Percentage price change.
    /// </summary>
    public decimal PriceChangePercentage { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Date and time when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Name of the user who made the change.
    /// </summary>
    public string ChangedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Source of the change (Manual, BulkEdit, CSVImport, AutoUpdate).
    /// </summary>
    public string ChangeSource { get; set; } = string.Empty;

    /// <summary>
    /// User-provided reason for the change.
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Old lead time in days.
    /// </summary>
    public int? OldLeadTimeDays { get; set; }

    /// <summary>
    /// New lead time in days.
    /// </summary>
    public int? NewLeadTimeDays { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    public string? Notes { get; set; }
}
