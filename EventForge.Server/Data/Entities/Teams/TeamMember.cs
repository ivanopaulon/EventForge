using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Teams;

/// <summary>
/// Represents a member of a team.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class TeamMember : AuditableEntity
{
    /// <summary>
    /// First name of the team member.
    /// </summary>
    [Required(ErrorMessage = "The first name is required.")]
    [MaxLength(100, ErrorMessage = "The first name cannot exceed 100 characters.")]
    [Display(Name = "First Name", Description = "First name of the team member.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the team member.
    /// </summary>
    [Required(ErrorMessage = "The last name is required.")]
    [MaxLength(100, ErrorMessage = "The last name cannot exceed 100 characters.")]
    [Display(Name = "Last Name", Description = "Last name of the team member.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the team member.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [Display(Name = "Email", Description = "Email address of the team member.")]
    public string? Email { get; set; }

    /// <summary>
    /// Role of the team member within the team.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The role cannot exceed 50 characters.")]
    [Display(Name = "Role", Description = "Role of the team member within the team.")]
    public string? Role { get; set; }

    /// <summary>
    /// Date of birth of the team member.
    /// </summary>
    [Display(Name = "Date of Birth", Description = "Date of birth of the team member.")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Status of the team member.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Status of the team member.")]
    public TeamMemberStatus Status { get; set; } = TeamMemberStatus.Active;

    /// <summary>
    /// Foreign key to the owning team.
    /// </summary>
    [Required]
    [Display(Name = "Team", Description = "Owning team.")]
    public Guid TeamId { get; set; }

    /// <summary>
    /// Navigation property for the owning team.
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// Position or playing position of the team member.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The position cannot exceed 50 characters.")]
    [Display(Name = "Position", Description = "Position or playing position of the team member.")]
    public string? Position { get; set; }

    /// <summary>
    /// Jersey number for the team member (must be unique within the team).
    /// </summary>
    [Range(1, 999, ErrorMessage = "Jersey number must be between 1 and 999.")]
    [Display(Name = "Jersey Number", Description = "Jersey number for the team member.")]
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Eligibility status for participation.
    /// </summary>
    [Required]
    [Display(Name = "Eligibility Status", Description = "Eligibility status for participation.")]
    public EligibilityStatus EligibilityStatus { get; set; } = EligibilityStatus.Eligible;

    /// <summary>
    /// Foreign key to the profile photo document.
    /// </summary>
    [Display(Name = "Photo Document", Description = "Profile photo document.")]
    public Guid? PhotoDocumentId { get; set; }

    /// <summary>
    /// Navigation property for the profile photo document.
    /// </summary>
    public DocumentReference? PhotoDocument { get; set; }

    /// <summary>
    /// Indicates if photo consent has been given.
    /// </summary>
    [Display(Name = "Photo Consent", Description = "Indicates if photo consent has been given.")]
    public bool PhotoConsent { get; set; } = false;

    /// <summary>
    /// Date and time when photo consent was given.
    /// </summary>
    [Display(Name = "Photo Consent At", Description = "Date and time when photo consent was given.")]
    public DateTime? PhotoConsentAt { get; set; }

    /// <summary>
    /// Computed full name property.
    /// </summary>
    [Display(Name = "Full Name", Description = "Full name of the team member.")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Computed age property (if DateOfBirth is available).
    /// </summary>
    [Display(Name = "Age", Description = "Age of the team member.")]
    public int? Age => DateOfBirth.HasValue ? (int?)((DateTime.UtcNow - DateOfBirth.Value).Days / 365.25) : null;

    /// <summary>
    /// Indicates if the team member is a minor (under 18).
    /// </summary>
    [Display(Name = "Is Minor", Description = "Indicates if the team member is a minor.")]
    public bool IsMinor => Age.HasValue && Age.Value < 18;

    /// <summary>
    /// Collection of membership cards for this team member.
    /// </summary>
    [Display(Name = "Membership Cards", Description = "Collection of membership cards for this team member.")]
    public ICollection<MembershipCard> MembershipCards { get; set; } = new List<MembershipCard>();

    /// <summary>
    /// Collection of insurance policies for this team member.
    /// </summary>
    [Display(Name = "Insurance Policies", Description = "Collection of insurance policies for this team member.")]
    public ICollection<InsurancePolicy> InsurancePolicies { get; set; } = new List<InsurancePolicy>();
}

/// <summary>
/// Status for the team member.
/// </summary>
public enum TeamMemberStatus
{
    Active,     // The member is actively participating
    Suspended,  // The member is temporarily suspended
    Retired,    // The member has retired from the team
    Excluded    // The member has been excluded from the team
}
