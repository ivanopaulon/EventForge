using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// Request DTO for logging a price change.
/// </summary>
public class PriceChangeLogRequest
{
    /// <summary>
    /// ProductSupplier relationship identifier.
    /// </summary>
    [Required]
    public Guid ProductSupplierId { get; set; }

    /// <summary>
    /// Supplier identifier.
    /// </summary>
    [Required]
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Product identifier.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Previous unit cost.
    /// </summary>
    public decimal OldPrice { get; set; }

    /// <summary>
    /// New unit cost.
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    [MaxLength(10)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Previous lead time in days.
    /// </summary>
    public int? OldLeadTimeDays { get; set; }

    /// <summary>
    /// New lead time in days.
    /// </summary>
    public int? NewLeadTimeDays { get; set; }

    /// <summary>
    /// Source of the change (Manual, BulkEdit, CSVImport, AutoUpdate).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ChangeSource { get; set; } = "Manual";

    /// <summary>
    /// Optional user-provided reason for the change.
    /// </summary>
    [MaxLength(500)]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// User identifier who made the change.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
