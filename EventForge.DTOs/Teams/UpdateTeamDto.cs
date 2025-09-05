using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Teams
{

    /// <summary>
    /// DTO for Team update operations.
    /// </summary>
    public class UpdateTeamDto
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
        public TeamStatus Status { get; set; }

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
        /// Coach contact ID.
        /// </summary>
        [Display(Name = "Coach Contact", Description = "Coach contact information.")]
        public Guid? CoachContactId { get; set; }

        /// <summary>
        /// Team logo document ID.
        /// </summary>
        [Display(Name = "Team Logo Document", Description = "Team logo document.")]
        public Guid? TeamLogoDocumentId { get; set; }
    }
}
