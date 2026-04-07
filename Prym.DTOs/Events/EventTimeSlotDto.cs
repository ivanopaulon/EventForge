using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Events;

/// <summary>
/// Represents a single daily time slot for reading/display.
/// </summary>
public class EventTimeSlotDto
{
    public Guid Id { get; set; }

    /// <summary>Daily start time (e.g. 08:00:00).</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Daily end time (e.g. 12:00:00).</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Optional descriptive label (e.g. "Mattina", "Pomeriggio").</summary>
    public string? Label { get; set; }

    /// <summary>Sort order for display.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Represents a single daily time slot for create/update operations.
/// </summary>
public class CreateEventTimeSlotDto
{
    /// <summary>Daily start time (e.g. 08:00:00). Required.</summary>
    [Required]
    public TimeSpan StartTime { get; set; }

    /// <summary>Daily end time (e.g. 12:00:00). Must be after StartTime.</summary>
    [Required]
    public TimeSpan EndTime { get; set; }

    /// <summary>Optional descriptive label (e.g. "Mattina", "Pomeriggio").</summary>
    [MaxLength(100)]
    public string? Label { get; set; }

    /// <summary>Sort order for display (0-based).</summary>
    public int SortOrder { get; set; }
}
