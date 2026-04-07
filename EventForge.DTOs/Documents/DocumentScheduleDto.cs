using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for document schedule information
    /// </summary>
    public class DocumentScheduleDto
    {
        /// <summary>
        /// Unique identifier for the schedule
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the document header (optional for template-based schedules)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to document type for type-based schedules
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Name of the schedule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the schedule
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Type of scheduled activity
        /// </summary>
        public ScheduleType ScheduleType { get; set; }

        /// <summary>
        /// Category for organizing schedules
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Start date of the schedule
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the schedule (optional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Frequency of the scheduled activity
        /// </summary>
        public ScheduleFrequency Frequency { get; set; }

        /// <summary>
        /// Interval for the frequency (e.g., every 2 months)
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Specific days for weekly/monthly schedules (JSON array)
        /// </summary>
        public string? SpecificDays { get; set; }

        /// <summary>
        /// Time of day for the scheduled activity
        /// </summary>
        public TimeSpan? TimeOfDay { get; set; }

        /// <summary>
        /// Timezone for the schedule
        /// </summary>
        public string? Timezone { get; set; }

        /// <summary>
        /// Priority of the schedule
        /// </summary>
        public SchedulePriority Priority { get; set; }

        /// <summary>
        /// Status of the schedule
        /// </summary>
        public ScheduleStatus Status { get; set; }

        /// <summary>
        /// Next execution date
        /// </summary>
        public DateTime? NextExecutionDate { get; set; }

        /// <summary>
        /// Last execution date
        /// </summary>
        public DateTime? LastExecutionDate { get; set; }

        /// <summary>
        /// Number of times executed
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the schedule
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// User who last modified the schedule
        /// </summary>
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document schedule
    /// </summary>
    public class CreateDocumentScheduleDto
    {
        /// <summary>
        /// Reference to the document header (optional for template-based schedules)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to document type for type-based schedules
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Name of the schedule
        /// </summary>
        [Required(ErrorMessage = "Schedule name is required.")]
        [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the schedule
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Type of scheduled activity
        /// </summary>
        [Required(ErrorMessage = "Schedule type is required.")]
        public ScheduleType ScheduleType { get; set; } = ScheduleType.Renewal;

        /// <summary>
        /// Category for organizing schedules
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Start date of the schedule
        /// </summary>
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the schedule (optional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Frequency of the scheduled activity
        /// </summary>
        public ScheduleFrequency Frequency { get; set; } = ScheduleFrequency.Monthly;

        /// <summary>
        /// Interval for the frequency (e.g., every 2 months)
        /// </summary>
        [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
        public int Interval { get; set; } = 1;

        /// <summary>
        /// Specific days for weekly/monthly schedules (JSON array)
        /// </summary>
        [StringLength(100, ErrorMessage = "Specific days cannot exceed 100 characters.")]
        public string? SpecificDays { get; set; }

        /// <summary>
        /// Time of day for the scheduled activity
        /// </summary>
        public TimeSpan? TimeOfDay { get; set; }

        /// <summary>
        /// Timezone for the schedule
        /// </summary>
        [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters.")]
        public string? Timezone { get; set; }

        /// <summary>
        /// Priority of the schedule
        /// </summary>
        public SchedulePriority Priority { get; set; } = SchedulePriority.Normal;

        /// <summary>
        /// Actions to perform (JSON configuration)
        /// </summary>
        [StringLength(2000, ErrorMessage = "Actions cannot exceed 2000 characters.")]
        public string? Actions { get; set; }

        /// <summary>
        /// Conditions for execution (JSON configuration)
        /// </summary>
        [StringLength(1000, ErrorMessage = "Conditions cannot exceed 1000 characters.")]
        public string? Conditions { get; set; }

        /// <summary>
        /// Notification settings (JSON configuration)
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
        public string? NotificationSettings { get; set; }
    }

    /// <summary>
    /// DTO for updating document schedule information
    /// </summary>
    public class UpdateDocumentScheduleDto
    {
        /// <summary>
        /// Name of the schedule
        /// </summary>
        [StringLength(100, ErrorMessage = "Schedule name cannot exceed 100 characters.")]
        public string? Name { get; set; }

        /// <summary>
        /// Description of the schedule
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Category for organizing schedules
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Start date of the schedule
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date of the schedule
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Frequency of the scheduled activity
        /// </summary>
        public ScheduleFrequency? Frequency { get; set; }

        /// <summary>
        /// Interval for the frequency
        /// </summary>
        [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365.")]
        public int? Interval { get; set; }

        /// <summary>
        /// Priority of the schedule
        /// </summary>
        public SchedulePriority? Priority { get; set; }

        /// <summary>
        /// Status of the schedule
        /// </summary>
        public ScheduleStatus? Status { get; set; }

        /// <summary>
        /// Time of day for the scheduled activity
        /// </summary>
        public TimeSpan? TimeOfDay { get; set; }

        /// <summary>
        /// Timezone for the schedule
        /// </summary>
        [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters.")]
        public string? Timezone { get; set; }
    }
}