using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for document reminder information
    /// </summary>
    public class DocumentReminderDto
    {
        /// <summary>
        /// Unique identifier for the reminder
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the document header
        /// </summary>
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Type of reminder
        /// </summary>
        public ReminderType ReminderType { get; set; }

        /// <summary>
        /// Title or subject of the reminder
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the reminder
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Target date/time for the reminder
        /// </summary>
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// Priority level of the reminder
        /// </summary>
        public ReminderPriority Priority { get; set; }

        /// <summary>
        /// Status of the reminder
        /// </summary>
        public ReminderStatus Status { get; set; }

        /// <summary>
        /// Indicates if the reminder is recurring
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Recurrence pattern for recurring reminders
        /// </summary>
        public RecurrencePattern? RecurrencePattern { get; set; }

        /// <summary>
        /// Recurrence interval (e.g., every 7 days)
        /// </summary>
        public int? RecurrenceInterval { get; set; }

        /// <summary>
        /// End date for recurring reminders
        /// </summary>
        public DateTime? RecurrenceEndDate { get; set; }

        /// <summary>
        /// Lead time in hours before target date to send notification
        /// </summary>
        public int LeadTimeHours { get; set; }

        /// <summary>
        /// Indicates if escalation is enabled
        /// </summary>
        public bool EscalationEnabled { get; set; }

        /// <summary>
        /// Last time notification was sent
        /// </summary>
        public DateTime? LastNotifiedAt { get; set; }

        /// <summary>
        /// Next scheduled notification time
        /// </summary>
        public DateTime? NextNotificationAt { get; set; }

        /// <summary>
        /// Number of notifications sent
        /// </summary>
        public int NotificationCount { get; set; }

        /// <summary>
        /// Number of times reminder was snoozed
        /// </summary>
        public int SnoozeCount { get; set; }

        /// <summary>
        /// Date and time when reminder was completed/dismissed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// User who completed/dismissed the reminder
        /// </summary>
        public string? CompletedBy { get; set; }

        /// <summary>
        /// Completion notes or reason
        /// </summary>
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the reminder
        /// </summary>
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document reminder
    /// </summary>
    public class CreateDocumentReminderDto
    {
        /// <summary>
        /// Reference to the document header
        /// </summary>
        [Required(ErrorMessage = "Document header is required.")]
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Type of reminder
        /// </summary>
        [Required(ErrorMessage = "Reminder type is required.")]
        public ReminderType ReminderType { get; set; } = ReminderType.Deadline;

        /// <summary>
        /// Title or subject of the reminder
        /// </summary>
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the reminder
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Target date/time for the reminder
        /// </summary>
        [Required(ErrorMessage = "Target date is required.")]
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// Priority level of the reminder
        /// </summary>
        public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

        /// <summary>
        /// Indicates if the reminder is recurring
        /// </summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>
        /// Recurrence pattern for recurring reminders
        /// </summary>
        public RecurrencePattern? RecurrencePattern { get; set; }

        /// <summary>
        /// Recurrence interval (e.g., every 7 days)
        /// </summary>
        [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365.")]
        public int? RecurrenceInterval { get; set; }

        /// <summary>
        /// End date for recurring reminders
        /// </summary>
        public DateTime? RecurrenceEndDate { get; set; }

        /// <summary>
        /// Lead time in hours before target date to send notification
        /// </summary>
        [Range(0, 8760, ErrorMessage = "Lead time must be between 0 and 8760 hours.")]
        public int LeadTimeHours { get; set; } = 24;

        /// <summary>
        /// Indicates if escalation is enabled
        /// </summary>
        public bool EscalationEnabled { get; set; } = false;

        /// <summary>
        /// User(s) to notify (JSON array of usernames)
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notify users cannot exceed 1000 characters.")]
        public string? NotifyUsers { get; set; }

        /// <summary>
        /// Role(s) to notify (JSON array of role names)
        /// </summary>
        [StringLength(500, ErrorMessage = "Notify roles cannot exceed 500 characters.")]
        public string? NotifyRoles { get; set; }

        /// <summary>
        /// Notification methods to use (JSON array: email, SMS, push, etc.)
        /// </summary>
        [StringLength(200, ErrorMessage = "Notification methods cannot exceed 200 characters.")]
        public string? NotificationMethods { get; set; }
    }

    /// <summary>
    /// DTO for updating document reminder information
    /// </summary>
    public class UpdateDocumentReminderDto
    {
        /// <summary>
        /// Title or subject of the reminder
        /// </summary>
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }

        /// <summary>
        /// Description of the reminder
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Target date/time for the reminder
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// Priority level of the reminder
        /// </summary>
        public ReminderPriority? Priority { get; set; }

        /// <summary>
        /// Status of the reminder
        /// </summary>
        public ReminderStatus? Status { get; set; }

        /// <summary>
        /// Lead time in hours before target date to send notification
        /// </summary>
        [Range(0, 8760, ErrorMessage = "Lead time must be between 0 and 8760 hours.")]
        public int? LeadTimeHours { get; set; }

        /// <summary>
        /// Completion notes or reason
        /// </summary>
        [StringLength(500, ErrorMessage = "Completion notes cannot exceed 500 characters.")]
        public string? CompletionNotes { get; set; }
    }
}