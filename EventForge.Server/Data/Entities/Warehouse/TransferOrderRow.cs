using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents a single line item in a transfer order.
/// </summary>
public class TransferOrderRow : AuditableEntity
{
    /// <summary>
    /// Parent transfer order.
    /// </summary>
    [Required(ErrorMessage = "Transfer order is required.")]
    [Display(Name = "Transfer Order", Description = "Parent transfer order.")]
    public Guid TransferOrderId { get; set; }

    /// <summary>
    /// Navigation property for the transfer order.
    /// </summary>
    public virtual TransferOrder? TransferOrder { get; set; }

    /// <summary>
    /// Product being transferred.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product being transferred.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// Source location in source warehouse.
    /// </summary>
    [Required(ErrorMessage = "Source location is required.")]
    [Display(Name = "Source Location", Description = "Source location in source warehouse.")]
    public Guid SourceLocationId { get; set; }

    /// <summary>
    /// Navigation property for source location.
    /// </summary>
    public virtual StorageLocation? SourceLocation { get; set; }

    /// <summary>
    /// Destination location in destination warehouse (can be set during receiving).
    /// </summary>
    [Display(Name = "Destination Location", Description = "Destination location in destination warehouse.")]
    public Guid? DestinationLocationId { get; set; }

    /// <summary>
    /// Navigation property for destination location.
    /// </summary>
    public virtual StorageLocation? DestinationLocation { get; set; }

    /// <summary>
    /// Quantity ordered for transfer.
    /// </summary>
    [Required(ErrorMessage = "Quantity ordered is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity ordered must be greater than zero.")]
    [Display(Name = "Quantity Ordered", Description = "Quantity ordered for transfer.")]
    public decimal QuantityOrdered { get; set; }

    /// <summary>
    /// Quantity actually shipped.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Quantity shipped must be non-negative.")]
    [Display(Name = "Quantity Shipped", Description = "Quantity actually shipped.")]
    public decimal QuantityShipped { get; set; }

    /// <summary>
    /// Quantity received at destination.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Quantity received must be non-negative.")]
    [Display(Name = "Quantity Received", Description = "Quantity received at destination.")]
    public decimal QuantityReceived { get; set; }

    /// <summary>
    /// Lot associated with this transfer (if applicable).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot associated with this transfer.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public virtual Lot? Lot { get; set; }

    /// <summary>
    /// Notes specific to this transfer row.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Notes specific to this transfer row.")]
    public string? Notes { get; set; }
}
