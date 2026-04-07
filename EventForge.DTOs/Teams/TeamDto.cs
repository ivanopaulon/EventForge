using EventForge.DTOs.Common;
namespace EventForge.DTOs.Teams
{

    /// <summary>
    /// DTO for Team output/display operations.
    /// </summary>
    public class TeamDto
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
        /// Club code or identifier (e.g., official club registration code).
        /// </summary>
        public string? ClubCode { get; set; }

        /// <summary>
        /// Federation code or identifier (e.g., national sports federation code).
        /// </summary>
        public string? FederationCode { get; set; }

        /// <summary>
        /// Team category (e.g., "Youth", "Senior", "Professional", "U18", "U21").
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Coach contact ID.
        /// </summary>
        public Guid? CoachContactId { get; set; }

        /// <summary>
        /// Coach contact name (for display purposes).
        /// </summary>
        public string? CoachContactName { get; set; }

        /// <summary>
        /// Team logo document ID.
        /// </summary>
        public Guid? TeamLogoDocumentId { get; set; }

        /// <summary>
        /// Team logo URL (for display purposes).
        /// </summary>
        public string? TeamLogoUrl { get; set; }

        /// <summary>
        /// Number of team members.
        /// </summary>
        public int MemberCount { get; set; }

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
}
