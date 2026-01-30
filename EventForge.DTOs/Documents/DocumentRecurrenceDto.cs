using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for DocumentRecurrence output/display operations
    /// </summary>
    public class DocumentRecurrenceDto
    {
        /// <summary>
        /// Unique identifier for the recurring schedule
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the recurring schedule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the recurring schedule
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Template to use for creating recurring documents
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// Template name for display
        /// </summary>
        public string? TemplateName { get; set; }

        /// <summary>
        /// Recurrence pattern
        /// </summary>
        public RecurrencePattern Pattern { get; set; }

        /// <summary>
        /// Interval for recurrence
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Days of week for weekly recurrence
        /// </summary>
        public string? DaysOfWeek { get; set; }

        /// <summary>
        /// Day of month for monthly recurrence
        /// </summary>
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Start date for the recurring schedule
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for the recurring schedule
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Maximum number of occurrences
        /// </summary>
        public int? MaxOccurrences { get; set; }

        /// <summary>
        /// Next scheduled execution date
        /// </summary>
        public DateTime? NextExecutionDate { get; set; }

        /// <summary>
        /// Last execution date
        /// </summary>
        public DateTime? LastExecutionDate { get; set; }

        /// <summary>
        /// Number of documents generated so far
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Indicates if the schedule is currently active
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Status of the recurring schedule
        /// </summary>
        public RecurrenceStatus Status { get; set; }

        /// <summary>
        /// Business party to use for generated documents
        /// </summary>
        public Guid? BusinessPartyId { get; set; }

        /// <summary>
        /// Warehouse to use for generated documents
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Lead time in days before generating the document
        /// </summary>
        public int LeadTimeDays { get; set; }

        /// <summary>
        /// Notification settings
        /// </summary>
        public string? NotificationSettings { get; set; }

        /// <summary>
        /// Additional configuration for document generation
        /// </summary>
        public string? AdditionalConfig { get; set; }

        /// <summary>
        /// Date and time when the schedule was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the schedule
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the schedule was last modified (UTC)
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the schedule
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates whether the schedule is active
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document recurrence
    /// </summary>
    public class CreateDocumentRecurrenceDto
    {
        /// <summary>
        /// Name of the recurring schedule
        /// </summary>
        [Required(ErrorMessage = "Schedule name is required.")]
        [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the recurring schedule
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Template to use for creating recurring documents
        /// </summary>
        [Required(ErrorMessage = "Template is required.")]
        public Guid TemplateId { get; set; }

        /// <summary>
        /// Recurrence pattern
        /// </summary>
        [Required(ErrorMessage = "Recurrence pattern is required.")]
        public RecurrencePattern Pattern { get; set; }

        /// <summary>
        /// Interval for recurrence
        /// </summary>
        [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
        public int Interval { get; set; } = 1;

        /// <summary>
        /// Days of week for weekly recurrence
        /// </summary>
        [StringLength(50, ErrorMessage = "Days of week cannot exceed 50 characters.")]
        public string? DaysOfWeek { get; set; }

        /// <summary>
        /// Day of month for monthly recurrence
        /// </summary>
        [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31.")]
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Start date for the recurring schedule
        /// </summary>
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for the recurring schedule
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Maximum number of occurrences
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Max occurrences must be between 1 and 1000.")]
        public int? MaxOccurrences { get; set; }

        /// <summary>
        /// Business party to use for generated documents
        /// </summary>
        public Guid? BusinessPartyId { get; set; }

        /// <summary>
        /// Warehouse to use for generated documents
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Lead time in days before generating the document
        /// </summary>
        [Range(0, 365, ErrorMessage = "Lead time must be between 0 and 365 days.")]
        public int LeadTimeDays { get; set; } = 0;

        /// <summary>
        /// Notification settings
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
        public string? NotificationSettings { get; set; }

        /// <summary>
        /// Additional configuration for document generation
        /// </summary>
        [StringLength(2000, ErrorMessage = "Additional config cannot exceed 2000 characters.")]
        public string? AdditionalConfig { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing document recurrence
    /// </summary>
    public class UpdateDocumentRecurrenceDto
    {
        /// <summary>
        /// Name of the recurring schedule
        /// </summary>
        [Required(ErrorMessage = "Schedule name is required.")]
        [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the recurring schedule
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Recurrence pattern
        /// </summary>
        [Required(ErrorMessage = "Recurrence pattern is required.")]
        public RecurrencePattern Pattern { get; set; }

        /// <summary>
        /// Interval for recurrence
        /// </summary>
        [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
        public int Interval { get; set; }

        /// <summary>
        /// Days of week for weekly recurrence
        /// </summary>
        [StringLength(50, ErrorMessage = "Days of week cannot exceed 50 characters.")]
        public string? DaysOfWeek { get; set; }

        /// <summary>
        /// Day of month for monthly recurrence
        /// </summary>
        [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31.")]
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Start date for the recurring schedule
        /// </summary>
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for the recurring schedule
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Maximum number of occurrences
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Max occurrences must be between 1 and 1000.")]
        public int? MaxOccurrences { get; set; }

        /// <summary>
        /// Indicates if the schedule is currently active
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Status of the recurring schedule
        /// </summary>
        public RecurrenceStatus Status { get; set; }

        /// <summary>
        /// Business party to use for generated documents
        /// </summary>
        public Guid? BusinessPartyId { get; set; }

        /// <summary>
        /// Warehouse to use for generated documents
        /// </summary>
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Lead time in days before generating the document
        /// </summary>
        [Range(0, 365, ErrorMessage = "Lead time must be between 0 and 365 days.")]
        public int LeadTimeDays { get; set; }

        /// <summary>
        /// Notification settings
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
        public string? NotificationSettings { get; set; }

        /// <summary>
        /// Additional configuration for document generation
        /// </summary>
        [StringLength(2000, ErrorMessage = "Additional config cannot exceed 2000 characters.")]
        public string? AdditionalConfig { get; set; }

        /// <summary>
        /// Indicates whether the schedule is active
        /// </summary>
        public bool IsActive { get; set; }
    }
}