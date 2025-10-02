using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a payment transaction in a sale session.
/// Supports multi-payment scenarios.
/// </summary>
public class SalePayment : AuditableEntity
{
    /// <summary>
    /// Payment unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the sale session.
    /// </summary>
    [Required]
    public Guid SaleSessionId { get; set; }

    /// <summary>
    /// Sale session navigation property.
    /// </summary>
    public SaleSession? SaleSession { get; set; }

    /// <summary>
    /// Payment method identifier (configurable from backend).
    /// </summary>
    [Required]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Payment method navigation property.
    /// </summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Amount paid with this payment method.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment status.
    /// </summary>
    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Transaction reference (from payment gateway, if applicable).
    /// </summary>
    [MaxLength(200)]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Payment timestamp.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Notes for this payment.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment completed successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Payment was cancelled.
    /// </summary>
    Cancelled = 4
}
