using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents a stock movement transaction (in, out, transfer, adjustment).
/// </summary>
public class StockMovement : AuditableEntity
{
    /// <summary>
    /// Movement type.
    /// </summary>
    [Required(ErrorMessage = "Movement type is required.")]
    [Display(Name = "Movement Type", Description = "Type of stock movement.")]
    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Product being moved.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product being moved.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot being moved (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot being moved.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Serial being moved (if applicable).
    /// </summary>
    [Display(Name = "Serial", Description = "Serial being moved.")]
    public Guid? SerialId { get; set; }

    /// <summary>
    /// Navigation property for the serial.
    /// </summary>
    public Serial? Serial { get; set; }

    /// <summary>
    /// Source location (for transfers and outbound movements).
    /// </summary>
    [Display(Name = "From Location", Description = "Source location for the movement.")]
    public Guid? FromLocationId { get; set; }

    /// <summary>
    /// Navigation property for the source location.
    /// </summary>
    public StorageLocation? FromLocation { get; set; }

    /// <summary>
    /// Destination location (for transfers and inbound movements).
    /// </summary>
    [Display(Name = "To Location", Description = "Destination location for the movement.")]
    public Guid? ToLocationId { get; set; }

    /// <summary>
    /// Navigation property for the destination location.
    /// </summary>
    public StorageLocation? ToLocation { get; set; }

    /// <summary>
    /// Quantity moved (positive for inbound, negative for outbound).
    /// </summary>
    [Required(ErrorMessage = "Quantity is required.")]
    [Display(Name = "Quantity", Description = "Quantity moved.")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost for this movement (for valuation).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative.")]
    [Display(Name = "Unit Cost", Description = "Unit cost for this movement.")]
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Total value of this movement.
    /// </summary>
    [Display(Name = "Total Value", Description = "Total value of this movement.")]
    public decimal? TotalValue => Math.Abs(Quantity) * (UnitCost ?? 0);

    /// <summary>
    /// Movement date and time.
    /// </summary>
    [Required(ErrorMessage = "Movement date is required.")]
    [Display(Name = "Movement Date", Description = "Movement date and time.")]
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reference document header (order, invoice, etc.).
    /// </summary>
    [Display(Name = "Document Reference", Description = "Reference document for this movement.")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document reference.
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Reference document row.
    /// </summary>
    [Display(Name = "Document Row Reference", Description = "Reference document row for this movement.")]
    public Guid? DocumentRowId { get; set; }

    /// <summary>
    /// Navigation property for the document row reference.
    /// </summary>
    public DocumentRow? DocumentRow { get; set; }

    /// <summary>
    /// Reason for the movement.
    /// </summary>
    [Display(Name = "Reason", Description = "Reason for the movement.")]
    public StockMovementReason Reason { get; set; } = StockMovementReason.Sale;

    /// <summary>
    /// Notes about this movement.
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Notes about this movement.")]
    public string? Notes { get; set; }

    /// <summary>
    /// User who performed the movement.
    /// </summary>
    [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
    [Display(Name = "User", Description = "User who performed the movement.")]
    public string? UserId { get; set; }

    /// <summary>
    /// Reference number for the movement (internal tracking).
    /// </summary>
    [StringLength(50, ErrorMessage = "Reference cannot exceed 50 characters.")]
    [Display(Name = "Reference", Description = "Reference number for the movement.")]
    public string? Reference { get; set; }

    /// <summary>
    /// Status of the movement.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the movement.")]
    public MovementStatus Status { get; set; } = MovementStatus.Completed;

    /// <summary>
    /// Related stock movement plan (if this movement was planned).
    /// </summary>
    [Display(Name = "Movement Plan", Description = "Related stock movement plan.")]
    public Guid? MovementPlanId { get; set; }

    /// <summary>
    /// Navigation property for the movement plan.
    /// </summary>
    public StockMovementPlan? MovementPlan { get; set; }
}

/// <summary>
/// Types of stock movements.
/// </summary>
public enum StockMovementType
{
    Inbound,         // Goods received
    Outbound,        // Goods shipped/sold
    Transfer,        // Transfer between locations
    Adjustment,      // Inventory adjustment
    Return,          // Return from customer
    Damage,          // Damaged goods
    Loss,            // Lost goods
    Found,           // Found goods (inventory correction)
    Production,      // Production input/output
    Consumption      // Internal consumption
}

/// <summary>
/// Reasons for stock movements.
/// </summary>
public enum StockMovementReason
{
    Sale,            // Sale to customer
    Purchase,        // Purchase from supplier
    Transfer,        // Transfer between locations
    Adjustment,      // Inventory adjustment
    Return,          // Return from customer
    Defect,          // Defective product
    Expiry,          // Expired product
    Loss,            // Lost product
    Theft,           // Stolen product
    Damage,          // Damaged product
    Production,      // Production process
    QualityControl,  // Quality control
    Maintenance,     // Maintenance
    Recall,          // Product recall
    Sample,          // Sample for testing
    Promotion,       // Promotional activity
    Other            // Other reason
}

/// <summary>
/// Status of stock movements.
/// </summary>
public enum MovementStatus
{
    Planned,         // Movement is planned but not executed
    InProgress,      // Movement is in progress
    Completed,       // Movement is completed
    Cancelled,       // Movement is cancelled
    Failed           // Movement failed
}