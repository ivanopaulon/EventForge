using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a reminder for document deadlines, renewals, or other time-based events
/// </summary>
public class DocumentReminder : AuditableEntity
{
    /// <summary>
    /// Reference to the document header
    /// </summary>
    [Required(ErrorMessage = "Document header is required.")]
    [Display(Name = "Document Header", Description = "Reference to the document header.")]
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Type of reminder
    /// </summary>
    [Required(ErrorMessage = "Reminder type is required.")]
    [Display(Name = "Reminder Type", Description = "Type of reminder.")]
    public ReminderType ReminderType { get; set; } = ReminderType.Deadline;

    /// <summary>
    /// Title or subject of the reminder
    /// </summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Title", Description = "Title or subject of the reminder.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the reminder
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of the reminder.")]
    public string? Description { get; set; }

    /// <summary>
    /// Target date/time for the reminder
    /// </summary>
    [Required(ErrorMessage = "Target date is required.")]
    [Display(Name = "Target Date", Description = "Target date/time for the reminder.")]
    public DateTime TargetDate { get; set; }

    /// <summary>
    /// Priority level of the reminder
    /// </summary>
    [Display(Name = "Priority", Description = "Priority level of the reminder.")]
    public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

    /// <summary>
    /// Status of the reminder
    /// </summary>
    [Display(Name = "Status", Description = "Status of the reminder.")]
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;

    /// <summary>
    /// Indicates if the reminder is recurring
    /// </summary>
    [Display(Name = "Is Recurring", Description = "Indicates if the reminder is recurring.")]
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Recurrence pattern for recurring reminders
    /// </summary>
    [Display(Name = "Recurrence Pattern", Description = "Recurrence pattern for recurring reminders.")]
    public RecurrencePattern? RecurrencePattern { get; set; }

    /// <summary>
    /// Recurrence interval (e.g., every 7 days)
    /// </summary>
    [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365.")]
    [Display(Name = "Recurrence Interval", Description = "Recurrence interval.")]
    public int? RecurrenceInterval { get; set; }

    /// <summary>
    /// End date for recurring reminders
    /// </summary>
    [Display(Name = "Recurrence End Date", Description = "End date for recurring reminders.")]
    public DateTime? RecurrenceEndDate { get; set; }

    /// <summary>
    /// User(s) to notify (JSON array of usernames)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notify users cannot exceed 1000 characters.")]
    [Display(Name = "Notify Users", Description = "Users to notify (JSON array).")]
    public string? NotifyUsers { get; set; }

    /// <summary>
    /// Role(s) to notify (JSON array of role names)
    /// </summary>
    [StringLength(500, ErrorMessage = "Notify roles cannot exceed 500 characters.")]
    [Display(Name = "Notify Roles", Description = "Roles to notify (JSON array).")]
    public string? NotifyRoles { get; set; }

    /// <summary>
    /// Email addresses to notify (JSON array)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notify emails cannot exceed 1000 characters.")]
    [Display(Name = "Notify Emails", Description = "Email addresses to notify (JSON array).")]
    public string? NotifyEmails { get; set; }

    /// <summary>
    /// Notification methods to use (JSON array: email, SMS, push, etc.)
    /// </summary>
    [StringLength(200, ErrorMessage = "Notification methods cannot exceed 200 characters.")]
    [Display(Name = "Notification Methods", Description = "Notification methods to use.")]
    public string? NotificationMethods { get; set; }

    /// <summary>
    /// Lead time in hours before target date to send notification
    /// </summary>
    [Range(0, 8760, ErrorMessage = "Lead time must be between 0 and 8760 hours.")]
    [Display(Name = "Lead Time Hours", Description = "Lead time in hours before target date.")]
    public int LeadTimeHours { get; set; } = 24;

    /// <summary>
    /// Indicates if escalation is enabled
    /// </summary>
    [Display(Name = "Escalation Enabled", Description = "Indicates if escalation is enabled.")]
    public bool EscalationEnabled { get; set; } = false;

    /// <summary>
    /// Escalation rules (JSON configuration)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Escalation rules cannot exceed 1000 characters.")]
    [Display(Name = "Escalation Rules", Description = "Escalation rules configuration.")]
    public string? EscalationRules { get; set; }

    /// <summary>
    /// Auto-snooze settings (JSON configuration)
    /// </summary>
    [StringLength(500, ErrorMessage = "Snooze settings cannot exceed 500 characters.")]
    [Display(Name = "Snooze Settings", Description = "Auto-snooze settings.")]
    public string? SnoozeSettings { get; set; }

    /// <summary>
    /// Custom data for the reminder (JSON)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Custom data cannot exceed 2000 characters.")]
    [Display(Name = "Custom Data", Description = "Custom data for the reminder.")]
    public string? CustomData { get; set; }

    /// <summary>
    /// Last time notification was sent
    /// </summary>
    [Display(Name = "Last Notified", Description = "Last time notification was sent.")]
    public DateTime? LastNotifiedAt { get; set; }

    /// <summary>
    /// Next scheduled notification time
    /// </summary>
    [Display(Name = "Next Notification", Description = "Next scheduled notification time.")]
    public DateTime? NextNotificationAt { get; set; }

    /// <summary>
    /// Number of notifications sent
    /// </summary>
    [Display(Name = "Notification Count", Description = "Number of notifications sent.")]
    public int NotificationCount { get; set; } = 0;

    /// <summary>
    /// Number of times reminder was snoozed
    /// </summary>
    [Display(Name = "Snooze Count", Description = "Number of times reminder was snoozed.")]
    public int SnoozeCount { get; set; } = 0;

    /// <summary>
    /// Date and time when reminder was completed/dismissed
    /// </summary>
    [Display(Name = "Completed At", Description = "Date when reminder was completed.")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who completed/dismissed the reminder
    /// </summary>
    [StringLength(100, ErrorMessage = "Completed by cannot exceed 100 characters.")]
    [Display(Name = "Completed By", Description = "User who completed the reminder.")]
    public string? CompletedBy { get; set; }

    /// <summary>
    /// Completion notes or reason
    /// </summary>
    [StringLength(500, ErrorMessage = "Completion notes cannot exceed 500 characters.")]
    [Display(Name = "Completion Notes", Description = "Completion notes or reason.")]
    public string? CompletionNotes { get; set; }
}

/// <summary>
/// Represents a schedule for document-related activities (renewals, reviews, etc.)
/// </summary>
public class DocumentSchedule : AuditableEntity
{
    /// <summary>
    /// Reference to the document header (optional for template-based schedules)
    /// </summary>
    [Display(Name = "Document Header", Description = "Reference to the document header.")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Reference to document type for type-based schedules
    /// </summary>
    [Display(Name = "Document Type", Description = "Reference to document type.")]
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Name of the schedule
    /// </summary>
    [Required(ErrorMessage = "Schedule name is required.")]
    [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
    [Display(Name = "Schedule Name", Description = "Name of the schedule.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the schedule
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the schedule.")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of scheduled activity
    /// </summary>
    [Required(ErrorMessage = "Schedule type is required.")]
    [Display(Name = "Schedule Type", Description = "Type of scheduled activity.")]
    public ScheduleType ScheduleType { get; set; } = ScheduleType.Renewal;

    /// <summary>
    /// Category for organizing schedules
    /// </summary>
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Category for organizing schedules.")]
    public string? Category { get; set; }

    /// <summary>
    /// Start date of the schedule
    /// </summary>
    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Start Date", Description = "Start date of the schedule.")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the schedule (optional)
    /// </summary>
    [Display(Name = "End Date", Description = "End date of the schedule.")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Frequency of the scheduled activity
    /// </summary>
    [Display(Name = "Frequency", Description = "Frequency of the scheduled activity.")]
    public ScheduleFrequency Frequency { get; set; } = ScheduleFrequency.Monthly;

    /// <summary>
    /// Interval for the frequency (e.g., every 2 months)
    /// </summary>
    [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
    [Display(Name = "Interval", Description = "Interval for the frequency.")]
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Specific days for weekly/monthly schedules (JSON array)
    /// </summary>
    [StringLength(100, ErrorMessage = "Specific days cannot exceed 100 characters.")]
    [Display(Name = "Specific Days", Description = "Specific days for schedules.")]
    public string? SpecificDays { get; set; }

    /// <summary>
    /// Time of day for the scheduled activity (stored as TimeSpan)
    /// </summary>
    [Display(Name = "Time Of Day", Description = "Time of day for the activity.")]
    public TimeSpan? TimeOfDay { get; set; }

    /// <summary>
    /// Timezone for the schedule
    /// </summary>
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters.")]
    [Display(Name = "Timezone", Description = "Timezone for the schedule.")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Priority of the schedule
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of the schedule.")]
    public SchedulePriority Priority { get; set; } = SchedulePriority.Normal;

    /// <summary>
    /// Status of the schedule
    /// </summary>
    [Display(Name = "Status", Description = "Status of the schedule.")]
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Active;

    /// <summary>
    /// Next execution date
    /// </summary>
    [Display(Name = "Next Execution", Description = "Next execution date.")]
    public DateTime? NextExecutionDate { get; set; }

    /// <summary>
    /// Last execution date
    /// </summary>
    [Display(Name = "Last Execution", Description = "Last execution date.")]
    public DateTime? LastExecutionDate { get; set; }

    /// <summary>
    /// Number of times executed
    /// </summary>
    [Display(Name = "Execution Count", Description = "Number of times executed.")]
    public int ExecutionCount { get; set; } = 0;

    /// <summary>
    /// Actions to perform (JSON configuration)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Actions cannot exceed 2000 characters.")]
    [Display(Name = "Actions", Description = "Actions to perform.")]
    public string? Actions { get; set; }

    /// <summary>
    /// Conditions for execution (JSON configuration)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Conditions cannot exceed 1000 characters.")]
    [Display(Name = "Conditions", Description = "Conditions for execution.")]
    public string? Conditions { get; set; }

    /// <summary>
    /// Notification settings (JSON configuration)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
    [Display(Name = "Notification Settings", Description = "Notification settings.")]
    public string? NotificationSettings { get; set; }

    /// <summary>
    /// Auto-renewal settings (JSON configuration)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Auto-renewal settings cannot exceed 1000 characters.")]
    [Display(Name = "Auto-Renewal Settings", Description = "Auto-renewal settings.")]
    public string? AutoRenewalSettings { get; set; }

    /// <summary>
    /// Integration settings for external systems (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Integration settings cannot exceed 1000 characters.")]
    [Display(Name = "Integration Settings", Description = "Integration settings.")]
    public string? IntegrationSettings { get; set; }

    /// <summary>
    /// Custom data for the schedule (JSON)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Custom data cannot exceed 2000 characters.")]
    [Display(Name = "Custom Data", Description = "Custom data for the schedule.")]
    public string? CustomData { get; set; }

    /// <summary>
    /// Reminders associated with this schedule
    /// </summary>
    [Display(Name = "Reminders", Description = "Reminders associated with this schedule.")]
    public ICollection<DocumentReminder> Reminders { get; set; } = new List<DocumentReminder>();
}