using EventForge.Data.Entities.Business;

namespace EventForge.Models.Business;

/// <summary>
/// DTO for PaymentTerm output/display operations.
/// </summary>
public class PaymentTermDto
{
    /// <summary>
    /// Unique identifier for the payment term.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name or short description of the payment term.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the payment term.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of days for payment due.
    /// </summary>
    public int DueDays { get; set; }

    /// <summary>
    /// Preferred payment method.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Status of the payment term.
    /// </summary>
    public PaymentTermStatus Status { get; set; }

    /// <summary>
    /// Date and time when the payment term was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the payment term.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the payment term was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the payment term.
    /// </summary>
    public string? ModifiedBy { get; set; }
}