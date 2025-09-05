using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Teams;

/// <summary>
/// Represents an insurance policy for a team member.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class InsurancePolicy : AuditableEntity
{
    /// <summary>
    /// Foreign key to the associated team member.
    /// </summary>
    [Required]
    [Display(Name = "Team Member", Description = "Associated team member.")]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Navigation property for the associated team member.
    /// </summary>
    public TeamMember? TeamMember { get; set; }

    /// <summary>
    /// Insurance provider company name.
    /// </summary>
    [Required(ErrorMessage = "The provider is required.")]
    [MaxLength(100, ErrorMessage = "The provider cannot exceed 100 characters.")]
    [Display(Name = "Provider", Description = "Insurance provider company name.")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Insurance policy number.
    /// </summary>
    [Required(ErrorMessage = "The policy number is required.")]
    [MaxLength(50, ErrorMessage = "The policy number cannot exceed 50 characters.")]
    [Display(Name = "Policy Number", Description = "Insurance policy number.")]
    public string PolicyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date from which the insurance is valid.
    /// </summary>
    [Required]
    [Display(Name = "Valid From", Description = "Date from which the insurance is valid.")]
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Date until which the insurance is valid.
    /// </summary>
    [Required]
    [Display(Name = "Valid To", Description = "Date until which the insurance is valid.")]
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Foreign key to the associated document reference (if policy document is uploaded).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Associated document reference.")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// Navigation property for the associated document reference.
    /// </summary>
    public DocumentReference? DocumentReference { get; set; }

    /// <summary>
    /// Type or category of insurance coverage (e.g., "Sports Liability", "Medical", "Comprehensive").
    /// </summary>
    [MaxLength(100, ErrorMessage = "The coverage type cannot exceed 100 characters.")]
    [Display(Name = "Coverage Type", Description = "Type or category of insurance coverage.")]
    public string? CoverageType { get; set; }

    /// <summary>
    /// Maximum coverage amount (if applicable).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Coverage amount must be non-negative.")]
    [Display(Name = "Coverage Amount", Description = "Maximum coverage amount.")]
    public decimal? CoverageAmount { get; set; }

    /// <summary>
    /// Currency for the coverage amount.
    /// </summary>
    [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
    [Display(Name = "Currency", Description = "Currency for the coverage amount.")]
    public string? Currency { get; set; } = "EUR";

    /// <summary>
    /// Additional notes about the insurance policy.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the insurance policy.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if the insurance policy is currently valid.
    /// </summary>
    [Display(Name = "Is Valid", Description = "Indicates if the insurance policy is currently valid.")]
    public bool IsValid => DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;

    /// <summary>
    /// Days until expiration (calculated property).
    /// </summary>
    [Display(Name = "Days Until Expiration", Description = "Days until the insurance policy expires.")]
    public int DaysUntilExpiration => (ValidTo.Date - DateTime.UtcNow.Date).Days;
}