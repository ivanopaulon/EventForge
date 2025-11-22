using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents a transfer order to move stock between warehouses.
/// </summary>
public class TransferOrder : AuditableEntity
{
    /// <summary>
    /// Transfer order number.
    /// </summary>
    [Required(ErrorMessage = "Transfer order number is required.")]
    [MaxLength(50, ErrorMessage = "Number cannot exceed 50 characters.")]
    [Display(Name = "Number", Description = "Transfer order number.")]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Transfer order series/prefix (optional).
    /// </summary>
    [MaxLength(20, ErrorMessage = "Series cannot exceed 20 characters.")]
    [Display(Name = "Series", Description = "Transfer order series/prefix.")]
    public string? Series { get; set; }

    /// <summary>
    /// Date when the transfer order was created.
    /// </summary>
    [Required(ErrorMessage = "Order date is required.")]
    [Display(Name = "Order Date", Description = "Date when the transfer order was created.")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source warehouse from which stock will be transferred.
    /// </summary>
    [Required(ErrorMessage = "Source warehouse is required.")]
    [Display(Name = "Source Warehouse", Description = "Source warehouse from which stock will be transferred.")]
    public Guid SourceWarehouseId { get; set; }

    /// <summary>
    /// Navigation property for source warehouse.
    /// </summary>
    public virtual StorageFacility? SourceWarehouse { get; set; }

    /// <summary>
    /// Destination warehouse to which stock will be transferred.
    /// </summary>
    [Required(ErrorMessage = "Destination warehouse is required.")]
    [Display(Name = "Destination Warehouse", Description = "Destination warehouse to which stock will be transferred.")]
    public Guid DestinationWarehouseId { get; set; }

    /// <summary>
    /// Navigation property for destination warehouse.
    /// </summary>
    public virtual StorageFacility? DestinationWarehouse { get; set; }

    /// <summary>
    /// Current status of the transfer order.
    /// </summary>
    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status", Description = "Current status of the transfer order.")]
    public TransferOrderStatus Status { get; set; } = TransferOrderStatus.Draft;

    /// <summary>
    /// Date when the transfer was shipped.
    /// </summary>
    [Display(Name = "Shipment Date", Description = "Date when the transfer was shipped.")]
    public DateTime? ShipmentDate { get; set; }

    /// <summary>
    /// Expected arrival date at destination.
    /// </summary>
    [Display(Name = "Expected Arrival Date", Description = "Expected arrival date at destination.")]
    public DateTime? ExpectedArrivalDate { get; set; }

    /// <summary>
    /// Actual arrival date at destination.
    /// </summary>
    [Display(Name = "Actual Arrival Date", Description = "Actual arrival date at destination.")]
    public DateTime? ActualArrivalDate { get; set; }

    /// <summary>
    /// Additional notes for the transfer order.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the transfer order.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Shipping reference (tracking number, carrier info, etc.).
    /// </summary>
    [MaxLength(200, ErrorMessage = "Shipping reference cannot exceed 200 characters.")]
    [Display(Name = "Shipping Reference", Description = "Shipping reference (tracking number, carrier info, etc.).")]
    public string? ShippingReference { get; set; }

    /// <summary>
    /// Transfer order line items.
    /// </summary>
    [Display(Name = "Rows", Description = "Transfer order line items.")]
    public virtual ICollection<TransferOrderRow> Rows { get; set; } = new List<TransferOrderRow>();
}

/// <summary>
/// Status values for transfer orders.
/// </summary>
public enum TransferOrderStatus
{
    /// <summary>
    /// Transfer order is being created/edited.
    /// </summary>
    [Display(Name = "Draft", Description = "Transfer order is being created/edited.")]
    Draft = 0,

    /// <summary>
    /// Transfer order is pending shipment.
    /// </summary>
    [Display(Name = "Pending", Description = "Transfer order is pending shipment.")]
    Pending = 1,

    /// <summary>
    /// Transfer order has been shipped.
    /// </summary>
    [Display(Name = "Shipped", Description = "Transfer order has been shipped.")]
    Shipped = 2,

    /// <summary>
    /// Transfer is in transit to destination.
    /// </summary>
    [Display(Name = "In Transit", Description = "Transfer is in transit to destination.")]
    InTransit = 3,

    /// <summary>
    /// Transfer has been received at destination.
    /// </summary>
    [Display(Name = "Received", Description = "Transfer has been received at destination.")]
    Received = 4,

    /// <summary>
    /// Transfer order is completed.
    /// </summary>
    [Display(Name = "Completed", Description = "Transfer order is completed.")]
    Completed = 5,

    /// <summary>
    /// Transfer order has been cancelled.
    /// </summary>
    [Display(Name = "Cancelled", Description = "Transfer order has been cancelled.")]
    Cancelled = 6
}
