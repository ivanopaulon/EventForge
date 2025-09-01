using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a recurring document schedule for automatic document generation
/// </summary>
public class DocumentRecurrence : AuditableEntity
{
    /// <summary>
    /// Name of the recurring schedule
    /// </summary>
    [Required(ErrorMessage = "Schedule name is required.")]
    [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
    [Display(Name = "Schedule Name", Description = "Name of the recurring schedule.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the recurring schedule
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the recurring schedule.")]
    public string? Description { get; set; }

    /// <summary>
    /// Template to use for creating recurring documents
    /// </summary>
    [Required(ErrorMessage = "Template is required.")]
    [Display(Name = "Template", Description = "Template to use for creating recurring documents.")]
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Navigation property for the template
    /// </summary>
    public DocumentTemplate? Template { get; set; }

    /// <summary>
    /// Recurrence pattern (daily, weekly, monthly, yearly, custom)
    /// </summary>
    [Required(ErrorMessage = "Recurrence pattern is required.")]
    [Display(Name = "Recurrence Pattern", Description = "Recurrence pattern for document generation.")]
    public RecurrencePattern Pattern { get; set; } = RecurrencePattern.Monthly;

    /// <summary>
    /// Interval for recurrence (e.g., every 2 weeks, every 3 months)
    /// </summary>
    [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
    [Display(Name = "Interval", Description = "Interval for recurrence.")]
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Days of week for weekly recurrence (JSON array)
    /// </summary>
    [StringLength(50, ErrorMessage = "Days of week cannot exceed 50 characters.")]
    [Display(Name = "Days Of Week", Description = "Days of week for weekly recurrence.")]
    public string? DaysOfWeek { get; set; }

    /// <summary>
    /// Day of month for monthly recurrence (1-31)
    /// </summary>
    [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31.")]
    [Display(Name = "Day Of Month", Description = "Day of month for monthly recurrence.")]
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Start date for the recurring schedule
    /// </summary>
    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Start Date", Description = "Start date for the recurring schedule.")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End date for the recurring schedule (optional)
    /// </summary>
    [Display(Name = "End Date", Description = "End date for the recurring schedule.")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum number of occurrences (optional alternative to end date)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Max occurrences must be between 1 and 1000.")]
    [Display(Name = "Max Occurrences", Description = "Maximum number of occurrences.")]
    public int? MaxOccurrences { get; set; }

    /// <summary>
    /// Next scheduled execution date
    /// </summary>
    [Display(Name = "Next Execution", Description = "Next scheduled execution date.")]
    public DateTime? NextExecutionDate { get; set; }

    /// <summary>
    /// Last execution date
    /// </summary>
    [Display(Name = "Last Execution", Description = "Last execution date.")]
    public DateTime? LastExecutionDate { get; set; }

    /// <summary>
    /// Number of documents generated so far
    /// </summary>
    [Display(Name = "Execution Count", Description = "Number of documents generated so far.")]
    public int ExecutionCount { get; set; } = 0;

    /// <summary>
    /// Indicates if the schedule is currently active
    /// </summary>
    [Display(Name = "Is Enabled", Description = "Indicates if the schedule is currently active.")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Status of the recurring schedule
    /// </summary>
    [Display(Name = "Status", Description = "Status of the recurring schedule.")]
    public RecurrenceStatus Status { get; set; } = RecurrenceStatus.Active;

    /// <summary>
    /// Business party to use for generated documents (overrides template default)
    /// </summary>
    [Display(Name = "Business Party", Description = "Business party to use for generated documents.")]
    public Guid? BusinessPartyId { get; set; }

    /// <summary>
    /// Warehouse to use for generated documents (overrides template default)
    /// </summary>
    [Display(Name = "Warehouse", Description = "Warehouse to use for generated documents.")]
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Lead time in days before generating the document
    /// </summary>
    [Range(0, 365, ErrorMessage = "Lead time must be between 0 and 365 days.")]
    [Display(Name = "Lead Time Days", Description = "Lead time in days before generating the document.")]
    public int LeadTimeDays { get; set; } = 0;

    /// <summary>
    /// Notification settings (JSON configuration)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
    [Display(Name = "Notification Settings", Description = "Notification settings for generated documents.")]
    public string? NotificationSettings { get; set; }

    /// <summary>
    /// Additional configuration for document generation (JSON)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Additional config cannot exceed 2000 characters.")]
    [Display(Name = "Additional Config", Description = "Additional configuration for document generation.")]
    public string? AdditionalConfig { get; set; }

    /// <summary>
    /// Documents generated by this recurring schedule
    /// </summary>
    [Display(Name = "Generated Documents", Description = "Documents generated by this recurring schedule.")]
    public ICollection<DocumentHeader> GeneratedDocuments { get; set; } = new List<DocumentHeader>();
}