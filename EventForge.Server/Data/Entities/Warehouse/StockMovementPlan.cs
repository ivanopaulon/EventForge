using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Documents;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents planned stock movements for future execution.
/// </summary>
public class StockMovementPlan : AuditableEntity
{
    /// <summary>
    /// Planned movement type.
    /// </summary>
    [Required(ErrorMessage = "Movement type is required.")]
    [Display(Name = "Movement Type", Description = "Type of planned stock movement.")]
    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Product to be moved.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product to be moved.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot to be moved (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot to be moved.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Source location for the planned movement.
    /// </summary>
    [Display(Name = "From Location", Description = "Source location for the planned movement.")]
    public Guid? FromLocationId { get; set; }

    /// <summary>
    /// Navigation property for the source location.
    /// </summary>
    public StorageLocation? FromLocation { get; set; }

    /// <summary>
    /// Destination location for the planned movement.
    /// </summary>
    [Display(Name = "To Location", Description = "Destination location for the planned movement.")]
    public Guid? ToLocationId { get; set; }

    /// <summary>
    /// Navigation property for the destination location.
    /// </summary>
    public StorageLocation? ToLocation { get; set; }

    /// <summary>
    /// Planned quantity to move.
    /// </summary>
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be positive.")]
    [Display(Name = "Planned Quantity", Description = "Planned quantity to move.")]
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Planned date for the movement.
    /// </summary>
    [Required(ErrorMessage = "Planned date is required.")]
    [Display(Name = "Planned Date", Description = "Planned date for the movement.")]
    public DateTime PlannedDate { get; set; }

    /// <summary>
    /// Priority of the planned movement.
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of the planned movement.")]
    public MovementPriority Priority { get; set; } = MovementPriority.Normal;

    /// <summary>
    /// Status of the planned movement.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the planned movement.")]
    public PlanStatus Status { get; set; } = PlanStatus.Planned;

    /// <summary>
    /// Reason for the planned movement.
    /// </summary>
    [Display(Name = "Reason", Description = "Reason for the planned movement.")]
    public StockMovementReason Reason { get; set; }

    /// <summary>
    /// Notes about the planned movement.
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Notes about the planned movement.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Reference document for the planned movement.
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference document for the planned movement.")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document reference.
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// User who created the plan.
    /// </summary>
    [StringLength(100, ErrorMessage = "Plan creator cannot exceed 100 characters.")]
    [Display(Name = "Plan Creator", Description = "User who created the plan.")]
    public string? PlanCreator { get; set; }

    /// <summary>
    /// User assigned to execute the plan.
    /// </summary>
    [StringLength(100, ErrorMessage = "Assigned to cannot exceed 100 characters.")]
    [Display(Name = "Assigned To", Description = "User assigned to execute the plan.")]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Date when the plan was approved.
    /// </summary>
    [Display(Name = "Approved Date", Description = "Date when the plan was approved.")]
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// User who approved the plan.
    /// </summary>
    [StringLength(100, ErrorMessage = "Approved by cannot exceed 100 characters.")]
    [Display(Name = "Approved By", Description = "User who approved the plan.")]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Date when the plan was executed.
    /// </summary>
    [Display(Name = "Executed Date", Description = "Date when the plan was executed.")]
    public DateTime? ExecutedDate { get; set; }

    /// <summary>
    /// Actual stock movements created from this plan.
    /// </summary>
    public ICollection<StockMovement> ExecutedMovements { get; set; } = new List<StockMovement>();
}

/// <summary>
/// Priority levels for movement plans.
/// </summary>
public enum MovementPriority
{
    Low,
    Normal,
    High,
    Critical,
    Emergency
}

/// <summary>
/// Status of movement plans.
/// </summary>
public enum PlanStatus
{
    Draft,           // Plan is being created
    Planned,         // Plan is finalized but not approved
    Approved,        // Plan is approved and ready for execution
    InProgress,      // Plan is being executed
    Completed,       // Plan has been fully executed
    PartiallyExecuted, // Plan has been partially executed
    Cancelled,       // Plan has been cancelled
    Failed,          // Plan execution failed
    OnHold          // Plan is on hold
}