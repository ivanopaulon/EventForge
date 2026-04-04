using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;
using EventForge.Server.Data.Entities.Events;

namespace EventForge.Server.Data.Entities.Calendar;

/// <summary>
/// Represents a calendar reminder or task with optional recurrence and event linkage.
/// </summary>
public class CalendarReminder : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public bool IsAllDay { get; set; } = false;

    public CalendarItemType ItemType { get; set; } = CalendarItemType.Reminder;

    public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

    public ReminderStatus Status { get; set; } = ReminderStatus.Active;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    public string? CompletedBy { get; set; }

    [MaxLength(500)]
    public string? CompletionNotes { get; set; }

    public Guid? EventId { get; set; }

    public Event? Event { get; set; }

    public bool IsRecurring { get; set; } = false;

    public RecurrencePattern? RecurrencePattern { get; set; }

    public int? RecurrenceInterval { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }
}
