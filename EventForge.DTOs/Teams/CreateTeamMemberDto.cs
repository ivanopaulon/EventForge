using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Teams
{

    /// <summary>
    /// DTO for TeamMember creation operations.
    /// </summary>
    public class CreateTeamMemberDto
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
        /// Owning team ID.
        /// </summary>
        [Required]
        [Display(Name = "Team", Description = "Owning team.")]
        public Guid TeamId { get; set; }

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
        /// Profile photo document ID.
        /// </summary>
        [Display(Name = "Photo Document", Description = "Profile photo document.")]
        public Guid? PhotoDocumentId { get; set; }

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
    }
}
