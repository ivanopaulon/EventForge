using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents stock levels for a product/lot combination at a specific location.
/// </summary>
public class Stock : AuditableEntity
{
    /// <summary>
    /// Product this stock entry refers to.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product this stock entry refers to.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Storage location where this stock is held.
    /// </summary>
    [Required(ErrorMessage = "Storage location is required.")]
    [Display(Name = "Storage Location", Description = "Storage location where this stock is held.")]
    public Guid StorageLocationId { get; set; }

    /// <summary>
    /// Navigation property for the storage location.
    /// </summary>
    public StorageLocation? StorageLocation { get; set; }

    /// <summary>
    /// Lot this stock belongs to (optional for non-lot managed products).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot this stock belongs to.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Current quantity available in stock.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    [Display(Name = "Quantity", Description = "Current quantity available in stock.")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Reserved quantity (allocated but not yet moved).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Reserved quantity must be non-negative.")]
    [Display(Name = "Reserved Quantity", Description = "Reserved quantity (allocated but not yet moved).")]
    public decimal ReservedQuantity { get; set; }

    /// <summary>
    /// Available quantity (Quantity - ReservedQuantity).
    /// </summary>
    [Display(Name = "Available Quantity", Description = "Available quantity (Quantity - ReservedQuantity).")]
    public decimal AvailableQuantity => Quantity - ReservedQuantity;

    /// <summary>
    /// Minimum stock level threshold for alerts.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Minimum level must be non-negative.")]
    [Display(Name = "Minimum Level", Description = "Minimum stock level threshold for alerts.")]
    public decimal? MinimumLevel { get; set; }

    /// <summary>
    /// Maximum stock level threshold for alerts.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Maximum level must be non-negative.")]
    [Display(Name = "Maximum Level", Description = "Maximum stock level threshold for alerts.")]
    public decimal? MaximumLevel { get; set; }

    /// <summary>
    /// Reorder point threshold.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Reorder point must be non-negative.")]
    [Display(Name = "Reorder Point", Description = "Reorder point threshold.")]
    public decimal? ReorderPoint { get; set; }

    /// <summary>
    /// Economic order quantity for reordering.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Reorder quantity must be non-negative.")]
    [Display(Name = "Reorder Quantity", Description = "Economic order quantity for reordering.")]
    public decimal? ReorderQuantity { get; set; }

    /// <summary>
    /// Last movement date for this stock entry.
    /// </summary>
    [Display(Name = "Last Movement Date", Description = "Last movement date for this stock entry.")]
    public DateTime? LastMovementDate { get; set; }

    /// <summary>
    /// Cost per unit for this stock (for valuation purposes).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative.")]
    [Display(Name = "Unit Cost", Description = "Cost per unit for this stock.")]
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Total value of this stock entry (Quantity * UnitCost).
    /// </summary>
    [Display(Name = "Total Value", Description = "Total value of this stock entry.")]
    public decimal? TotalValue => Quantity * (UnitCost ?? 0);

    /// <summary>
    /// Date of last physical inventory count.
    /// </summary>
    [Display(Name = "Last Inventory Date", Description = "Date of last physical inventory count.")]
    public DateTime? LastInventoryDate { get; set; }

    /// <summary>
    /// Notes about this stock entry.
    /// </summary>
    [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Notes about this stock entry.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Stock alerts generated for this stock entry.
    /// </summary>
    public ICollection<StockAlert> StockAlerts { get; set; } = new List<StockAlert>();

    /// <summary>
    /// Stock movements affecting this stock entry.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}