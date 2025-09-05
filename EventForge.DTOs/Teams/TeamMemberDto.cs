using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Teams
{

    /// <summary>
    /// DTO for TeamMember output/display operations.
    /// </summary>
    public class TeamMemberDto
    {
        /// <summary>
        /// Unique identifier for the team member.
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
        /// Full name of the team member (computed).
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

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
        public TeamMemberStatus Status { get; set; }

        /// <summary>
        /// Owning team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Team name (for display purposes).
        /// </summary>
        public string? TeamName { get; set; }

        /// <summary>
        /// Position or playing position of the team member.
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// Jersey number for the team member.
        /// </summary>
        public int? JerseyNumber { get; set; }

        /// <summary>
        /// Eligibility status for participation.
        /// </summary>
        public EligibilityStatus EligibilityStatus { get; set; }

        /// <summary>
        /// Profile photo document ID.
        /// </summary>
        public Guid? PhotoDocumentId { get; set; }

        /// <summary>
        /// Profile photo URL (for display purposes).
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Indicates if photo consent has been given.
        /// </summary>
        public bool PhotoConsent { get; set; }

        /// <summary>
        /// Date and time when photo consent was given.
        /// </summary>
        public DateTime? PhotoConsentAt { get; set; }

        /// <summary>
        /// Age of the team member (computed from DateOfBirth).
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// Indicates if the team member is a minor (under 18).
        /// </summary>
        public bool IsMinor { get; set; }

        /// <summary>
        /// Date and time when the team member was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the team member.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the team member was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the team member.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
