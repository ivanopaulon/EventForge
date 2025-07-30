using System;
using System.ComponentModel.DataAnnotations;

using EventForge.DTOs.Common;
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
