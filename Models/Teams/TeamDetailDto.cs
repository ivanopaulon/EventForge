namespace EventForge.Models.Teams;

/// <summary>
/// DTO for detailed Team information including members.
/// </summary>
public class TeamDetailDto
{
    /// <summary>
    /// Unique identifier for the team.
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
    public TeamStatus Status { get; set; }

    /// <summary>
    /// Associated event ID.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event name (for display purposes).
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Collection of team members.
    /// </summary>
    public ICollection<TeamMemberDto> Members { get; set; } = new List<TeamMemberDto>();

    /// <summary>
    /// Number of team members.
    /// </summary>
    public int MemberCount => Members.Count;

    /// <summary>
    /// Date and time when the team was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the team.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the team was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the team.
    /// </summary>
    public string? ModifiedBy { get; set; }
}