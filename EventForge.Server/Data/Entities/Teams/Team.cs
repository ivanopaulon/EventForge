using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Teams;

/// <summary>
/// Represents a team participating in an event.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class Team : AuditableEntity
{
    /// <summary>
    /// Team name.
    /// </summary>
    [Required(ErrorMessage = "The team name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Team name.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the team.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The short description cannot exceed 200 characters.")]
    [Display(Name = "Short Description", Description = "Short description of the team.")]
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the team.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The long description cannot exceed 1000 characters.")]
    [Display(Name = "Long Description", Description = "Detailed description of the team.")]
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>
    /// Contact email for the team.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [Display(Name = "Email", Description = "Contact email for the team.")]
    public string? Email { get; set; }

    /// <summary>
    /// Team status.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Team status.")]
    public TeamStatus Status { get; set; } = TeamStatus.Active;

    /// <summary>
    /// Foreign key to the associated event.
    /// </summary>
    [Required]
    [Display(Name = "Event", Description = "Associated event.")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Navigation property for the associated event.
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Club code or identifier (e.g., official club registration code).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The club code cannot exceed 50 characters.")]
    [Display(Name = "Club Code", Description = "Club code or identifier.")]
    public string? ClubCode { get; set; }

    /// <summary>
    /// Federation code or identifier (e.g., national sports federation code).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The federation code cannot exceed 50 characters.")]
    [Display(Name = "Federation Code", Description = "Federation code or identifier.")]
    public string? FederationCode { get; set; }

    /// <summary>
    /// Team category (e.g., "Youth", "Senior", "Professional", "U18", "U21").
    /// </summary>
    [MaxLength(50, ErrorMessage = "The category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Team category.")]
    public string? Category { get; set; }

    /// <summary>
    /// Foreign key to the coach contact information.
    /// </summary>
    [Display(Name = "Coach Contact", Description = "Coach contact information.")]
    public Guid? CoachContactId { get; set; }

    /// <summary>
    /// Navigation property for the coach contact.
    /// </summary>
    public Contact? CoachContact { get; set; }

    /// <summary>
    /// Foreign key to the team logo document.
    /// </summary>
    [Display(Name = "Team Logo Document", Description = "Team logo document.")]
    public Guid? TeamLogoDocumentId { get; set; }

    /// <summary>
    /// Navigation property for the team logo document.
    /// </summary>
    public DocumentReference? TeamLogoDocument { get; set; }

    /// <summary>
    /// Collection of team members.
    /// </summary>
    [Display(Name = "Members", Description = "Collection of team members.")]
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}

/// <summary>
/// Status for the team.
/// </summary>
public enum TeamStatus
{
    Active,     // The team is active and participating
    Suspended,  // The team is temporarily suspended
    Retired,    // The team has withdrawn from the event
    Deleted     // The team has been eliminated
}

