using System.ComponentModel.DataAnnotations;
using EventForge.Data.Entities.Audit;

namespace EventForge.Data.Entities.Teams;

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
