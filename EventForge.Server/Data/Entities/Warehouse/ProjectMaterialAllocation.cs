using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents material allocation for a project or job order.
/// </summary>
public class ProjectMaterialAllocation : AuditableEntity
{
    /// <summary>
    /// Project this allocation belongs to.
    /// </summary>
    [Required(ErrorMessage = "Project is required.")]
    [Display(Name = "Project", Description = "Project this allocation belongs to.")]
    public Guid ProjectOrderId { get; set; }

    /// <summary>
    /// Navigation property for the project.
    /// </summary>
    public ProjectOrder? ProjectOrder { get; set; }

    /// <summary>
    /// Product being allocated.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product being allocated.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot being allocated (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot being allocated.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Serial being allocated (if applicable).
    /// </summary>
    [Display(Name = "Serial", Description = "Serial being allocated.")]
    public Guid? SerialId { get; set; }

    /// <summary>
    /// Navigation property for the serial.
    /// </summary>
    public Serial? Serial { get; set; }

    /// <summary>
    /// Storage location from which material is allocated.
    /// </summary>
    [Display(Name = "Storage Location", Description = "Storage location from which material is allocated.")]
    public Guid? StorageLocationId { get; set; }

    /// <summary>
    /// Navigation property for the storage location.
    /// </summary>
    public StorageLocation? StorageLocation { get; set; }

    /// <summary>
    /// Planned quantity to allocate.
    /// </summary>
    [Required(ErrorMessage = "Planned quantity is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Planned quantity must be non-negative.")]
    [Display(Name = "Planned Quantity", Description = "Planned quantity to allocate.")]
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Quantity actually allocated.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Allocated quantity must be non-negative.")]
    [Display(Name = "Allocated Quantity", Description = "Quantity actually allocated.")]
    public decimal AllocatedQuantity { get; set; } = 0;

    /// <summary>
    /// Quantity consumed or used.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Consumed quantity must be non-negative.")]
    [Display(Name = "Consumed Quantity", Description = "Quantity consumed or used.")]
    public decimal ConsumedQuantity { get; set; } = 0;

    /// <summary>
    /// Quantity returned to stock.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Returned quantity must be non-negative.")]
    [Display(Name = "Returned Quantity", Description = "Quantity returned to stock.")]
    public decimal ReturnedQuantity { get; set; } = 0;

    /// <summary>
    /// Unit of measure for quantities.
    /// </summary>
    [Display(Name = "Unit of Measure", Description = "Unit of measure for quantities.")]
    public Guid? UnitOfMeasureId { get; set; }

    /// <summary>
    /// Navigation property for the unit of measure.
    /// </summary>
    public UM? UnitOfMeasure { get; set; }

    /// <summary>
    /// Status of the allocation.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the allocation.")]
    public AllocationStatus Status { get; set; } = AllocationStatus.Planned;

    /// <summary>
    /// Date when allocation was planned.
    /// </summary>
    [Display(Name = "Planned Date", Description = "Date when allocation was planned.")]
    public DateTime? PlannedDate { get; set; }

    /// <summary>
    /// Date when material was allocated.
    /// </summary>
    [Display(Name = "Allocation Date", Description = "Date when material was allocated.")]
    public DateTime? AllocationDate { get; set; }

    /// <summary>
    /// Date when consumption started.
    /// </summary>
    [Display(Name = "Consumption Start Date", Description = "Date when consumption started.")]
    public DateTime? ConsumptionStartDate { get; set; }

    /// <summary>
    /// Date when consumption completed.
    /// </summary>
    [Display(Name = "Consumption End Date", Description = "Date when consumption completed.")]
    public DateTime? ConsumptionEndDate { get; set; }

    /// <summary>
    /// Unit cost of the material.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative.")]
    [Display(Name = "Unit Cost", Description = "Unit cost of the material.")]
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Total cost (calculated: ConsumedQuantity * UnitCost).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Total cost must be non-negative.")]
    [Display(Name = "Total Cost", Description = "Total cost of consumed materials.")]
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Reference to stock movement for allocation.
    /// </summary>
    [Display(Name = "Stock Movement", Description = "Reference to stock movement.")]
    public Guid? StockMovementId { get; set; }

    /// <summary>
    /// Navigation property for the stock movement.
    /// </summary>
    public StockMovement? StockMovement { get; set; }

    /// <summary>
    /// Purpose or reason for allocation.
    /// </summary>
    [StringLength(200, ErrorMessage = "Purpose cannot exceed 200 characters.")]
    [Display(Name = "Purpose", Description = "Purpose or reason for allocation.")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Person who requested the allocation.
    /// </summary>
    [StringLength(100, ErrorMessage = "Requested by cannot exceed 100 characters.")]
    [Display(Name = "Requested By", Description = "Person who requested the allocation.")]
    public string? RequestedBy { get; set; }

    /// <summary>
    /// Person who approved the allocation.
    /// </summary>
    [StringLength(100, ErrorMessage = "Approved by cannot exceed 100 characters.")]
    [Display(Name = "Approved By", Description = "Person who approved the allocation.")]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Notes and additional information.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Notes and additional information.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Status of material allocations.
/// </summary>
public enum AllocationStatus
{
    Planned,            // Allocation planned
    Reserved,           // Material reserved for project
    Allocated,          // Material allocated to project
    InUse,              // Material in use
    PartiallyConsumed,  // Partially consumed
    Consumed,           // Fully consumed
    Returned,           // Returned to stock
    Cancelled           // Allocation cancelled
}
