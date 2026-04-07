using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Teams;

/// <summary>
/// Represents a membership card for a team member.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class MembershipCard : AuditableEntity
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
    /// Membership card number.
    /// </summary>
    [Required(ErrorMessage = "The card number is required.")]
    [MaxLength(50, ErrorMessage = "The card number cannot exceed 50 characters.")]
    [Display(Name = "Card Number", Description = "Membership card number.")]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Federation or organization that issued the card.
    /// </summary>
    [Required(ErrorMessage = "The federation is required.")]
    [MaxLength(100, ErrorMessage = "The federation cannot exceed 100 characters.")]
    [Display(Name = "Federation", Description = "Federation or organization that issued the card.")]
    public string Federation { get; set; } = string.Empty;

    /// <summary>
    /// Date from which the membership is valid.
    /// </summary>
    [Required]
    [Display(Name = "Valid From", Description = "Date from which the membership is valid.")]
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Date until which the membership is valid.
    /// </summary>
    [Required]
    [Display(Name = "Valid To", Description = "Date until which the membership is valid.")]
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Foreign key to the associated document reference (if card document is uploaded).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Associated document reference.")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// Navigation property for the associated document reference.
    /// </summary>
    public DocumentReference? DocumentReference { get; set; }

    /// <summary>
    /// Category or type of membership (e.g., "Youth", "Senior", "Professional").
    /// </summary>
    [MaxLength(50, ErrorMessage = "The category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Category or type of membership.")]
    public string? Category { get; set; }

    /// <summary>
    /// Additional notes about the membership card.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the membership card.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if the membership is currently valid.
    /// </summary>
    [Display(Name = "Is Valid", Description = "Indicates if the membership is currently valid.")]
    public bool IsValid => DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;

    /// <summary>
    /// Days until expiration (calculated property).
    /// </summary>
    [Display(Name = "Days Until Expiration", Description = "Days until the membership expires.")]
    public int DaysUntilExpiration => (ValidTo.Date - DateTime.UtcNow.Date).Days;
}