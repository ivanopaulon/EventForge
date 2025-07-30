using System;
using System.ComponentModel.DataAnnotations;

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
    }
}
