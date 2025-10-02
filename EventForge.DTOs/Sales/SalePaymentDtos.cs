using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{

/// <summary>
/// DTO for adding a payment to a sale session.
/// </summary>
public class AddSalePaymentDto
{
    /// <summary>
    /// Payment method identifier.
    /// </summary>
    [Required(ErrorMessage = "Payment method ID is required")]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Amount to pay.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction reference (optional).
    /// </summary>
    [MaxLength(200)]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Notes for this payment.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for a sale payment.
/// </summary>
public class SalePaymentDto
{
    /// <summary>
    /// Payment identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Payment method identifier.
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Payment method name.
    /// </summary>
    public string? PaymentMethodName { get; set; }

    /// <summary>
    /// Payment method code.
    /// </summary>
    public string? PaymentMethodCode { get; set; }

    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment status.
    /// </summary>
    public PaymentStatusDto Status { get; set; }

    /// <summary>
    /// Transaction reference.
    /// </summary>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Payment timestamp.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payment status DTO.
/// </summary>
public enum PaymentStatusDto
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}
}
