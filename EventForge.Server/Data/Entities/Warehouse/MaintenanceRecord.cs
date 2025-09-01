using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents maintenance records for serialized items.
/// </summary>
public class MaintenanceRecord : AuditableEntity
{
    /// <summary>
    /// Serial this maintenance record is for.
    /// </summary>
    [Required(ErrorMessage = "Serial is required.")]
    [Display(Name = "Serial", Description = "Serial this maintenance record is for.")]
    public Guid SerialId { get; set; }

    /// <summary>
    /// Navigation property for the serial.
    /// </summary>
    public Serial? Serial { get; set; }

    /// <summary>
    /// Maintenance record number.
    /// </summary>
    [Required(ErrorMessage = "Record number is required.")]
    [StringLength(50, ErrorMessage = "Record number cannot exceed 50 characters.")]
    [Display(Name = "Record Number", Description = "Maintenance record number.")]
    public string RecordNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of maintenance performed.
    /// </summary>
    [Required(ErrorMessage = "Maintenance type is required.")]
    [Display(Name = "Maintenance Type", Description = "Type of maintenance performed.")]
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>
    /// Status of the maintenance.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the maintenance.")]
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    /// <summary>
    /// Date when maintenance was scheduled.
    /// </summary>
    [Required(ErrorMessage = "Scheduled date is required.")]
    [Display(Name = "Scheduled Date", Description = "Date when maintenance was scheduled.")]
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Date when maintenance was started.
    /// </summary>
    [Display(Name = "Started Date", Description = "Date when maintenance was started.")]
    public DateTime? StartedDate { get; set; }

    /// <summary>
    /// Date when maintenance was completed.
    /// </summary>
    [Display(Name = "Completed Date", Description = "Date when maintenance was completed.")]
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Technician who performed the maintenance.
    /// </summary>
    [StringLength(100, ErrorMessage = "Technician name cannot exceed 100 characters.")]
    [Display(Name = "Technician", Description = "Technician who performed the maintenance.")]
    public string? Technician { get; set; }

    /// <summary>
    /// Description of the maintenance work performed.
    /// </summary>
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of the maintenance work performed.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parts used during maintenance.
    /// </summary>
    [StringLength(500, ErrorMessage = "Parts used cannot exceed 500 characters.")]
    [Display(Name = "Parts Used", Description = "Parts used during maintenance.")]
    public string? PartsUsed { get; set; }

    /// <summary>
    /// Cost of the maintenance.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Cost must be non-negative.")]
    [Display(Name = "Cost", Description = "Cost of the maintenance.")]
    public decimal? Cost { get; set; }

    /// <summary>
    /// Labor hours spent on maintenance.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Labor hours must be non-negative.")]
    [Display(Name = "Labor Hours", Description = "Labor hours spent on maintenance.")]
    public decimal? LaborHours { get; set; }

    /// <summary>
    /// Next scheduled maintenance date.
    /// </summary>
    [Display(Name = "Next Maintenance Date", Description = "Next scheduled maintenance date.")]
    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// Maintenance interval in days.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Maintenance interval must be positive.")]
    [Display(Name = "Maintenance Interval", Description = "Maintenance interval in days.")]
    public int? MaintenanceIntervalDays { get; set; }

    /// <summary>
    /// Issues found during maintenance.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Issues found cannot exceed 1000 characters.")]
    [Display(Name = "Issues Found", Description = "Issues found during maintenance.")]
    public string? IssuesFound { get; set; }

    /// <summary>
    /// Recommendations from the maintenance.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Recommendations cannot exceed 1000 characters.")]
    [Display(Name = "Recommendations", Description = "Recommendations from the maintenance.")]
    public string? Recommendations { get; set; }

    /// <summary>
    /// External service provider (if outsourced).
    /// </summary>
    [StringLength(200, ErrorMessage = "Service provider cannot exceed 200 characters.")]
    [Display(Name = "Service Provider", Description = "External service provider (if outsourced).")]
    public string? ServiceProvider { get; set; }

    /// <summary>
    /// Warranty information related to this maintenance.
    /// </summary>
    [StringLength(200, ErrorMessage = "Warranty info cannot exceed 200 characters.")]
    [Display(Name = "Warranty Info", Description = "Warranty information related to this maintenance.")]
    public string? WarrantyInfo { get; set; }

    /// <summary>
    /// Priority of the maintenance.
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of the maintenance.")]
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;

    /// <summary>
    /// Reference to related document (work order, invoice, etc.).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference to related document.")]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Additional notes about the maintenance.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the maintenance.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Types of maintenance.
/// </summary>
public enum MaintenanceType
{
    Preventive,      // Scheduled preventive maintenance
    Corrective,      // Corrective maintenance (fixing issues)
    Emergency,       // Emergency maintenance
    Inspection,      // Inspection only
    Calibration,     // Equipment calibration
    Cleaning,        // Cleaning maintenance
    Upgrade,         // Upgrade or improvement
    Replacement,     // Part replacement
    Repair,          // Repair work
    Testing,         // Testing and verification
    Overhaul,        // Complete overhaul
    Warranty,        // Warranty maintenance
    Custom           // Custom maintenance type
}

/// <summary>
/// Status of maintenance records.
/// </summary>
public enum MaintenanceStatus
{
    Scheduled,       // Maintenance is scheduled
    InProgress,      // Maintenance is in progress
    Completed,       // Maintenance is completed
    Cancelled,       // Maintenance was cancelled
    OnHold,          // Maintenance is on hold
    Failed,          // Maintenance failed
    RequiresFollow   // Requires follow-up
}

/// <summary>
/// Priority levels for maintenance.
/// </summary>
public enum MaintenancePriority
{
    Low,
    Normal,
    High,
    Critical,
    Emergency
}