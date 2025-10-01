using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents a project or job order for tracking material allocation and work orders.
/// </summary>
public class ProjectOrder : AuditableEntity
{
    /// <summary>
    /// Project order number.
    /// </summary>
    [Required(ErrorMessage = "Order number is required.")]
    [StringLength(50, ErrorMessage = "Order number cannot exceed 50 characters.")]
    [Display(Name = "Order Number", Description = "Project order number.")]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name or title of the project.
    /// </summary>
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(200, ErrorMessage = "Project name cannot exceed 200 characters.")]
    [Display(Name = "Project Name", Description = "Name or title of the project.")]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the project.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of the project.")]
    public string? Description { get; set; }

    /// <summary>
    /// Customer or client for this project.
    /// </summary>
    [Display(Name = "Customer", Description = "Customer or client for this project.")]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Navigation property for the customer.
    /// </summary>
    public BusinessParty? Customer { get; set; }

    /// <summary>
    /// Type of project.
    /// </summary>
    [Required(ErrorMessage = "Project type is required.")]
    [Display(Name = "Project Type", Description = "Type of project.")]
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// Status of the project.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the project.")]
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    /// <summary>
    /// Priority of the project.
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of the project.")]
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;

    /// <summary>
    /// Start date of the project.
    /// </summary>
    [Display(Name = "Start Date", Description = "Start date of the project.")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Planned completion date.
    /// </summary>
    [Display(Name = "Planned End Date", Description = "Planned completion date.")]
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Actual completion date.
    /// </summary>
    [Display(Name = "Actual End Date", Description = "Actual completion date.")]
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Project manager or responsible person.
    /// </summary>
    [StringLength(100, ErrorMessage = "Project manager cannot exceed 100 characters.")]
    [Display(Name = "Project Manager", Description = "Project manager or responsible person.")]
    public string? ProjectManager { get; set; }

    /// <summary>
    /// Estimated budget for the project.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Budget must be non-negative.")]
    [Display(Name = "Estimated Budget", Description = "Estimated budget for the project.")]
    public decimal? EstimatedBudget { get; set; }

    /// <summary>
    /// Actual cost incurred.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Actual cost must be non-negative.")]
    [Display(Name = "Actual Cost", Description = "Actual cost incurred.")]
    public decimal? ActualCost { get; set; }

    /// <summary>
    /// Estimated labor hours.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Estimated hours must be non-negative.")]
    [Display(Name = "Estimated Hours", Description = "Estimated labor hours.")]
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Actual labor hours spent.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Actual hours must be non-negative.")]
    [Display(Name = "Actual Hours", Description = "Actual labor hours spent.")]
    public decimal? ActualHours { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100.")]
    [Display(Name = "Progress %", Description = "Progress percentage (0-100).")]
    public decimal ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Storage location where project materials are allocated.
    /// </summary>
    [Display(Name = "Storage Location", Description = "Storage location for project materials.")]
    public Guid? StorageLocationId { get; set; }

    /// <summary>
    /// Navigation property for the storage location.
    /// </summary>
    public StorageLocation? StorageLocation { get; set; }

    /// <summary>
    /// Reference to related document (contract, work order, etc.).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference to related document.")]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// External reference number (PO, contract number, etc.).
    /// </summary>
    [StringLength(100, ErrorMessage = "External reference cannot exceed 100 characters.")]
    [Display(Name = "External Reference", Description = "External reference number.")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Notes and additional information.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    [Display(Name = "Notes", Description = "Notes and additional information.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Material allocations for this project.
    /// </summary>
    public ICollection<ProjectMaterialAllocation> MaterialAllocations { get; set; } = new List<ProjectMaterialAllocation>();

    /// <summary>
    /// Stock movements associated with this project.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

/// <summary>
/// Types of projects.
/// </summary>
public enum ProjectType
{
    Production,         // Production order
    Maintenance,        // Maintenance project
    Construction,       // Construction project
    Installation,       // Installation project
    Service,            // Service project
    Research,           // R&D project
    Consulting,         // Consulting project
    Event,              // Event project
    Custom,             // Custom project
    Internal,           // Internal project
    CustomerOrder       // Customer order project
}

/// <summary>
/// Status of projects.
/// </summary>
public enum ProjectStatus
{
    Planning,           // Project in planning phase
    Approved,           // Project approved
    InProgress,         // Project in progress
    OnHold,             // Project on hold
    Completed,          // Project completed
    Cancelled,          // Project cancelled
    Closed,             // Project closed and archived
    UnderReview         // Project under review
}

/// <summary>
/// Priority levels for projects.
/// </summary>
public enum ProjectPriority
{
    Low,
    Normal,
    High,
    Urgent,
    Critical
}
