using System.ComponentModel.DataAnnotations;

namespace Prym.Server.Data.Entities.Events;

/// <summary>
/// Represents a daily time slot for an event (e.g. 08:00–12:00 or 14:00–18:00).
/// An event can have multiple time slots per day (e.g. morning + afternoon sessions).
/// The slot times apply to every day spanned by the parent event's date range.
/// </summary>
public class EventTimeSlot
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent event.</summary>
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    /// <summary>Daily start time (e.g. 08:00:00).</summary>
    [Required]
    public TimeSpan StartTime { get; set; }

    /// <summary>Daily end time (e.g. 12:00:00). Must be after StartTime.</summary>
    [Required]
    public TimeSpan EndTime { get; set; }

    /// <summary>Optional label for this slot (e.g. "Mattina", "Pomeriggio").</summary>
    [MaxLength(100)]
    public string? Label { get; set; }

    /// <summary>Sort order for display purposes.</summary>
    public int SortOrder { get; set; }
}
