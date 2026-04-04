using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Events
{

    /// <summary>
    /// DTO for Event update operations.
    /// </summary>
    public class UpdateEventDto
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
        public DateTime StartDate { get; set; }

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
        public int Capacity { get; set; }

        /// <summary>
        /// Event status.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Event status.")]
        public EventStatus Status { get; set; }

        /// <summary>
        /// Hex color code for scheduler display (e.g. "#4285F4"). Optional.
        /// </summary>
        [MaxLength(7, ErrorMessage = "Color code cannot exceed 7 characters.")]
        [Display(Name = "Color", Description = "Hex color for scheduler display.")]
        public string? Color { get; set; }

        /// <summary>
        /// Username the event is assigned to. Optional.
        /// </summary>
        [MaxLength(100, ErrorMessage = "AssignedToUserId cannot exceed 100 characters.")]
        [Display(Name = "Assigned To", Description = "Username this event is assigned to.")]
        public string? AssignedToUserId { get; set; }

        /// <summary>
        /// Visibility of the event (Public or Private).
        /// </summary>
        [Display(Name = "Visibility", Description = "Controls who can see this event.")]
        public CalendarVisibility Visibility { get; set; } = CalendarVisibility.Public;

        /// <summary>Daily time slots (e.g. 08:00–12:00 and 14:00–18:00). Replaces all existing slots.</summary>
        public List<CreateEventTimeSlotDto> TimeSlots { get; set; } = new();

        /// <summary>
        /// Row version for optimistic concurrency control. Send the value received from the
        /// GET response to detect concurrent modifications. Null disables the check.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
