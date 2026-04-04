using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Calendar;

/// <summary>
/// DTO for reading and displaying a calendar reminder.
/// </summary>
public class CalendarReminderDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsAllDay { get; set; }
    public CalendarItemType ItemType { get; set; }
    public ReminderPriority Priority { get; set; }
    public ReminderStatus Status { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? CompletionNotes { get; set; }
    public Guid? EventId { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public string? Color { get; set; }
    public string? AssignedToUserId { get; set; }
    public CalendarVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for creating a new calendar reminder or task.
/// </summary>
public class CreateCalendarReminderDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public bool IsAllDay { get; set; }

    public CalendarItemType ItemType { get; set; } = CalendarItemType.Reminder;

    public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

    public ReminderStatus Status { get; set; } = ReminderStatus.Active;

    public Guid? EventId { get; set; }

    public bool IsRecurring { get; set; }

    public RecurrencePattern? RecurrencePattern { get; set; }

    [Range(1, 365)]
    public int? RecurrenceInterval { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? AssignedToUserId { get; set; }

    public CalendarVisibility Visibility { get; set; } = CalendarVisibility.Public;
}

/// <summary>
/// DTO for updating an existing calendar reminder or task.
/// </summary>
public class UpdateCalendarReminderDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public bool IsAllDay { get; set; }

    public CalendarItemType ItemType { get; set; }

    public ReminderPriority Priority { get; set; }

    public ReminderStatus Status { get; set; }

    public Guid? EventId { get; set; }

    public bool IsRecurring { get; set; }

    public RecurrencePattern? RecurrencePattern { get; set; }

    [Range(1, 365)]
    public int? RecurrenceInterval { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }

    [MaxLength(500)]
    public string? CompletionNotes { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? AssignedToUserId { get; set; }

    public CalendarVisibility Visibility { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
