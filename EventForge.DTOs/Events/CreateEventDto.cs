using System;
using System.ComponentModel.DataAnnotations;

using EventForge.DTOs.Common;
namespace EventForge.DTOs.Events
{
    
    /// <summary>
    /// DTO for Event creation operations.
    /// </summary>
    public class CreateEventDto
    {
        /// <summary>
        /// Event name.
        /// </summary>
        [Required(ErrorMessage = "The event name is required.")]
        [MaxLength(100, ErrorMessage = "The event name cannot exceed 100 characters.")]
        [Display(Name = "Name", Description = "Unique event name.")]
        public string Name { get; set; } = string.Empty;
    
        /// <summary>
        /// Short event description.
        /// </summary>
        [Required(ErrorMessage = "The short description is required.")]
        [MaxLength(200, ErrorMessage = "The short description cannot exceed 200 characters.")]
        [Display(Name = "Short Description", Description = "Short event description.")]
        public string ShortDescription { get; set; } = string.Empty;
    
        /// <summary>
        /// Detailed event description.
        /// </summary>
        [Display(Name = "Long Description", Description = "Detailed event description.")]
        public string LongDescription { get; set; } = string.Empty;
    
        /// <summary>
        /// Event location.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The location cannot exceed 200 characters.")]
        [Display(Name = "Location", Description = "Event location.")]
        public string Location { get; set; } = string.Empty;
    
        /// <summary>
        /// Event start date and time (UTC).
        /// </summary>
        [Required(ErrorMessage = "The start date is required.")]
        [Display(Name = "Start Date", Description = "Event start date and time (UTC).")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
        /// <summary>
        /// Event end date and time (UTC).
        /// </summary>
        [Display(Name = "End Date", Description = "Event end date and time (UTC).")]
        public DateTime? EndDate { get; set; }
    
        /// <summary>
        /// Maximum event capacity.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
        [Display(Name = "Capacity", Description = "Maximum event capacity.")]
        public int Capacity { get; set; } = 1;
    
        /// <summary>
        /// Event status.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Event status.")]
        public EventStatus Status { get; set; } = EventStatus.Planned;
    }
}
