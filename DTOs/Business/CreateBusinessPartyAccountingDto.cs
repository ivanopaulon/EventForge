using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Business;

/// <summary>
/// DTO for BusinessPartyAccounting creation operations.
/// </summary>
public class CreateBusinessPartyAccountingDto
{
    /// <summary>
    /// Foreign key to the related BusinessParty.
    /// </summary>
    [Required(ErrorMessage = "The business party ID is required.")]
    [Display(Name = "Business Party", Description = "Reference to the related business party.")]
    public Guid BusinessPartyId { get; set; }

    /// <summary>
    /// IBAN for payments.
    /// </summary>
    [MaxLength(34, ErrorMessage = "The IBAN cannot exceed 34 characters.")]
    [Display(Name = "IBAN", Description = "IBAN for payments.")]
    public string? Iban { get; set; }

    /// <summary>
    /// Foreign key to the related bank.
    /// </summary>
    [Display(Name = "Bank", Description = "Reference to the bank entity.")]
    public Guid? BankId { get; set; }

    /// <summary>
    /// Foreign key to the payment term.
    /// </summary>
    [Display(Name = "Payment Term", Description = "Reference to the payment term.")]
    public Guid? PaymentTermId { get; set; }

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
    public string? Notes { get; set; }
}