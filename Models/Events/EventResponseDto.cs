namespace EventForge.Models.Events;

/// <summary>
/// DTO for Event response, including associated teams and members.
/// </summary>
public class EventResponseDto
{
    /// <summary>
    /// Event unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short event description.
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed event description.
    /// </summary>
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>
    /// Event location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Event start date and time (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date and time (UTC).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum event capacity.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Event status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Status description.
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Teams associated with the event.
    /// </summary>
    public List<TeamResponseDto> Teams { get; set; } = new List<TeamResponseDto>();

    /// <summary>
    /// Number of teams registered.
    /// </summary>
    public int TeamCount { get; set; }

    /// <summary>
    /// Total number of team members.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Date and time when the entity was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the entity was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the entity.
    /// </summary>
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for Team response within an Event.
/// </summary>
public class TeamResponseDto
{
    /// <summary>
    /// Team unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Team name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the team.
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the team.
    /// </summary>
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>
    /// Contact email for the team.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Team status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Status description.
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Collection of team members.
    /// </summary>
    public List<TeamMemberResponseDto> Members { get; set; } = new List<TeamMemberResponseDto>();

    /// <summary>
    /// Number of members in the team.
    /// </summary>
    public int MemberCount { get; set; }
}

/// <summary>
/// DTO for TeamMember response within a Team.
/// </summary>
public class TeamMemberResponseDto
{
    /// <summary>
    /// Member unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// First name of the team member.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the team member.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the team member.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the team member.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Role of the team member within the team.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Date of birth of the team member.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Status of the team member.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Status description.
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;
}