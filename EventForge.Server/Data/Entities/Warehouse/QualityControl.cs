using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents quality control records for lots and products.
/// </summary>
public class QualityControl : AuditableEntity
{
    /// <summary>
    /// Product this quality control is for.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product this quality control is for.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot this quality control is for (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot this quality control is for.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Serial this quality control is for (if applicable).
    /// </summary>
    [Display(Name = "Serial", Description = "Serial this quality control is for.")]
    public Guid? SerialId { get; set; }

    /// <summary>
    /// Navigation property for the serial.
    /// </summary>
    public Serial? Serial { get; set; }

    /// <summary>
    /// Quality control test code/number.
    /// </summary>
    [Required(ErrorMessage = "Control number is required.")]
    [StringLength(50, ErrorMessage = "Control number cannot exceed 50 characters.")]
    [Display(Name = "Control Number", Description = "Quality control test code/number.")]
    public string ControlNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of quality control test.
    /// </summary>
    [Required(ErrorMessage = "Control type is required.")]
    [Display(Name = "Control Type", Description = "Type of quality control test.")]
    public QualityControlType ControlType { get; set; }

    /// <summary>
    /// Status of the quality control.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the quality control.")]
    public QualityControlStatus Status { get; set; } = QualityControlStatus.InProgress;

    /// <summary>
    /// Date when quality control was started.
    /// </summary>
    [Required(ErrorMessage = "Test date is required.")]
    [Display(Name = "Test Date", Description = "Date when quality control was started.")]
    public DateTime TestDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when quality control was completed.
    /// </summary>
    [Display(Name = "Completion Date", Description = "Date when quality control was completed.")]
    public DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Inspector who performed the quality control.
    /// </summary>
    [Required(ErrorMessage = "Inspector is required.")]
    [StringLength(100, ErrorMessage = "Inspector name cannot exceed 100 characters.")]
    [Display(Name = "Inspector", Description = "Inspector who performed the quality control.")]
    public string Inspector { get; set; } = string.Empty;

    /// <summary>
    /// Test method or procedure used.
    /// </summary>
    [StringLength(200, ErrorMessage = "Test method cannot exceed 200 characters.")]
    [Display(Name = "Test Method", Description = "Test method or procedure used.")]
    public string? TestMethod { get; set; }

    /// <summary>
    /// Sample size tested.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Sample size must be non-negative.")]
    [Display(Name = "Sample Size", Description = "Sample size tested.")]
    public decimal? SampleSize { get; set; }

    /// <summary>
    /// Test results summary.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Results cannot exceed 1000 characters.")]
    [Display(Name = "Results", Description = "Test results summary.")]
    public string? Results { get; set; }

    /// <summary>
    /// Observations and notes.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Observations cannot exceed 1000 characters.")]
    [Display(Name = "Observations", Description = "Observations and notes.")]
    public string? Observations { get; set; }

    /// <summary>
    /// Whether the quality control passed.
    /// </summary>
    [Display(Name = "Passed", Description = "Whether the quality control passed.")]
    public bool? Passed { get; set; }

    /// <summary>
    /// Defects found during quality control.
    /// </summary>
    [StringLength(500, ErrorMessage = "Defects cannot exceed 500 characters.")]
    [Display(Name = "Defects", Description = "Defects found during quality control.")]
    public string? Defects { get; set; }

    /// <summary>
    /// Corrective actions taken.
    /// </summary>
    [StringLength(500, ErrorMessage = "Corrective actions cannot exceed 500 characters.")]
    [Display(Name = "Corrective Actions", Description = "Corrective actions taken.")]
    public string? CorrectiveActions { get; set; }

    /// <summary>
    /// Reference to related document (certificate, report, etc.).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference to related document.")]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Certificate number (if applicable).
    /// </summary>
    [StringLength(100, ErrorMessage = "Certificate number cannot exceed 100 characters.")]
    [Display(Name = "Certificate Number", Description = "Certificate number (if applicable).")]
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Expiry date of the quality control (if applicable).
    /// </summary>
    [Display(Name = "Expiry Date", Description = "Expiry date of the quality control.")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Next scheduled quality control date.
    /// </summary>
    [Display(Name = "Next Control Date", Description = "Next scheduled quality control date.")]
    public DateTime? NextControlDate { get; set; }

    /// <summary>
    /// Cost of the quality control test.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Cost must be non-negative.")]
    [Display(Name = "Cost", Description = "Cost of the quality control test.")]
    public decimal? Cost { get; set; }

    /// <summary>
    /// External laboratory or inspector (if outsourced).
    /// </summary>
    [StringLength(200, ErrorMessage = "External lab cannot exceed 200 characters.")]
    [Display(Name = "External Lab", Description = "External laboratory or inspector (if outsourced).")]
    public string? ExternalLab { get; set; }
}

/// <summary>
/// Types of quality control tests.
/// </summary>
public enum QualityControlType
{
    Incoming,        // Incoming goods inspection
    InProcess,       // In-process quality control
    Final,           // Final product inspection
    Periodic,        // Periodic quality check
    Random,          // Random sampling
    CustomerComplaint, // Quality control due to customer complaint
    ReturnInspection, // Inspection of returned goods
    PreShipment,     // Pre-shipment inspection
    Calibration,     // Equipment calibration
    Environmental,   // Environmental testing
    Safety,          // Safety testing
    Compliance,      // Compliance testing
    Custom           // Custom quality control
}

/// <summary>
/// Status of quality control tests.
/// </summary>
public enum QualityControlStatus
{
    Scheduled,       // Quality control is scheduled
    InProgress,      // Quality control is in progress
    Completed,       // Quality control is completed
    Passed,          // Quality control passed
    Failed,          // Quality control failed
    OnHold,          // Quality control is on hold
    Cancelled,       // Quality control was cancelled
    RequiresRetest   // Requires retesting
}