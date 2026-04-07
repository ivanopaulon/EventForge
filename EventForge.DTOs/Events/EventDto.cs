using EventForge.DTOs.Common;
namespace EventForge.DTOs.Events
{

    /// <summary>
    /// DTO for Event output/display operations.
    /// </summary>
    public class EventDto
    {
        /// <summary>
        /// Unique identifier for the event.
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
        public EventStatus Status { get; set; }

        /// <summary>
        /// Hex color code for scheduler display (e.g. "#4285F4"). Null means default color.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Username the event is assigned to.
        /// </summary>
        public string? AssignedToUserId { get; set; }

        /// <summary>
        /// Visibility of the event.
        /// </summary>
        public CalendarVisibility Visibility { get; set; }

        /// <summary>Daily time slots (e.g. 08:00–12:00 and 14:00–18:00).</summary>
        public List<EventTimeSlotDto> TimeSlots { get; set; } = new();

        /// <summary>
        /// Number of teams associated with the event.
        /// </summary>
        public int TeamCount { get; set; }

        /// <summary>
        /// Date and time when the event was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the event.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the event was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the event.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency. Returned by the server so clients can
        /// include it in update requests to detect concurrent modifications.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
