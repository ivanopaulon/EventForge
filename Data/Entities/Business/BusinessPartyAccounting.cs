using System.ComponentModel.DataAnnotations;

/// <summary>
/// Accounting and banking data associated with a BusinessParty.
/// </summary>
public class BusinessPartyAccounting : AuditableEntity
{
    /// <summary>
    /// Foreign key to the related BusinessParty.
    /// </summary>
    [Required(ErrorMessage = "The business party ID is required.")]
    [Display(Name = "Business Party", Description = "Reference to the related business party.")]
    public Guid BusinessPartyId { get; set; } = Guid.Empty;

    /// <summary>
    /// IBAN for payments.
    /// </summary>
    [MaxLength(34, ErrorMessage = "The IBAN cannot exceed 34 characters.")]
    [Display(Name = "IBAN", Description = "IBAN for payments.")]
    public string? Iban { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the related bank.
    /// </summary>
    [Display(Name = "Bank", Description = "Reference to the bank entity.")]
    public Guid? BankId { get; set; } = null;

    /// <summary>
    /// Navigation property for the bank.
    /// </summary>
    [Display(Name = "Bank", Description = "Navigation property for the bank.")]
    public Bank? Bank { get; set; } = null;

    /// <summary>
    /// Foreign key to the payment term.
    /// </summary>
    [Display(Name = "Payment Term", Description = "Reference to the payment term.")]
    public Guid? PaymentTermId { get; set; } = null;

    /// <summary>
    /// Navigation property for the payment term.
    /// </summary>
    [Display(Name = "Payment Term", Description = "Navigation property for the payment term.")]
    public PaymentTerm? PaymentTerm { get; set; } = null;

    /// <summary>
    /// Assigned credit limit.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "The credit limit must be a positive value.")]
    [Display(Name = "Credit Limit", Description = "Assigned credit limit.")]
    public decimal? CreditLimit { get; set; } = 0m;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The notes cannot exceed 100 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;
}