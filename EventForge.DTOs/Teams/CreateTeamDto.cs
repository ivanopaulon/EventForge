using System;
using System.ComponentModel.DataAnnotations;

using EventForge.DTOs.Common;
namespace EventForge.DTOs.Teams
{
    
    /// <summary>
    /// DTO for Team creation operations.
    /// </summary>
    public class CreateTeamDto
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
        /// Associated event ID.
        /// </summary>
        [Required]
        [Display(Name = "Event", Description = "Associated event.")]
        public Guid EventId { get; set; }
    }
}
