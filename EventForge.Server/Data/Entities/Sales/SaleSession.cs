using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a complete sales session with customer, items, payments, and state management.
/// Supports bar/restaurant (tables) and retail scenarios.
/// </summary>
public class SaleSession : AuditableEntity
{
    /// <summary>
    /// Reference to the store user (operator/cashier) who created the session.
    /// </summary>
    [Required]
    public Guid OperatorId { get; set; }

    /// <summary>
    /// Reference to the POS terminal where the session was created.
    /// </summary>
    [Required]
    public Guid PosId { get; set; }

    /// <summary>
    /// Customer identifier (optional for quick sales).
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Sale type identifier (configurable from backend).
    /// </summary>
    [MaxLength(50)]
    public string? SaleType { get; set; }

    /// <summary>
    /// Current session status.
    /// </summary>
    [Required]
    public SaleSessionStatus Status { get; set; } = SaleSessionStatus.Open;

    /// <summary>
    /// Total amount before discounts and promotions.
    /// </summary>
    public decimal OriginalTotal { get; set; }

    /// <summary>
    /// Total discount amount applied.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Final total after all discounts and promotions.
    /// </summary>
    public decimal FinalTotal { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Session items (products/services).
    /// </summary>
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    /// <summary>
    /// Session payments (supports multi-payment).
    /// </summary>
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();

    /// <summary>
    /// Session notes.
    /// </summary>
    public ICollection<SessionNote> Notes { get; set; } = new List<SessionNote>();

    /// <summary>
    /// Table identifier (for bar/restaurant scenarios).
    /// </summary>
    public Guid? TableId { get; set; }

    /// <summary>
    /// Table session reference (for table management).
    /// </summary>
    public TableSession? TableSession { get; set; }

    /// <summary>
    /// Document identifier (invoice, receipt) if created.
    /// </summary>
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Session closed timestamp.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Applied coupon codes.
    /// </summary>
    [MaxLength(500)]
    public string? CouponCodes { get; set; }
}

/// <summary>
/// Sale session status enumeration.
/// </summary>
public enum SaleSessionStatus
{
    /// <summary>
    /// Session is open and active.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Session is suspended (operator stepped away).
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// Session is closed and payment completed.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// Session was cancelled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Session is being split (intermediate state).
    /// </summary>
    Splitting = 4,

    /// <summary>
    /// Session is being merged (intermediate state).
    /// </summary>
    Merging = 5
}
