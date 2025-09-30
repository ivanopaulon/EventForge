using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Business;

namespace EventForge.Server.Data.Entities.Products;

/// <summary>
/// Represents the relationship between a product and its supplier.
/// </summary>
public class ProductSupplier : AuditableEntity
{
    /// <summary>
    /// Product identifier (foreign key).
    /// </summary>
    [Required(ErrorMessage = "The product is required.")]
    [Display(Name = "Product", Description = "Product identifier.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product associated with this supplier relationship.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Supplier identifier (foreign key to BusinessParty).
    /// </summary>
    [Required(ErrorMessage = "The supplier is required.")]
    [Display(Name = "Supplier", Description = "Supplier identifier.")]
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Supplier (BusinessParty with PartyType = Fornitore or ClienteFornitore).
    /// </summary>
    public BusinessParty? Supplier { get; set; }

    /// <summary>
    /// Supplier's product code/SKU.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The supplier product code cannot exceed 100 characters.")]
    [Display(Name = "Supplier Product Code", Description = "Supplier's product code/SKU.")]
    public string? SupplierProductCode { get; set; }

    /// <summary>
    /// Purchase description (specific to this supplier).
    /// </summary>
    [MaxLength(500, ErrorMessage = "The purchase description cannot exceed 500 characters.")]
    [Display(Name = "Purchase Description", Description = "Purchase description.")]
    public string? PurchaseDescription { get; set; }

    /// <summary>
    /// Unit cost from this supplier.
    /// </summary>
    [Display(Name = "Unit Cost", Description = "Unit cost from this supplier.")]
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Currency for the unit cost.
    /// </summary>
    [MaxLength(10, ErrorMessage = "The currency cannot exceed 10 characters.")]
    [Display(Name = "Currency", Description = "Currency for the unit cost.")]
    public string? Currency { get; set; }

    /// <summary>
    /// Minimum order quantity.
    /// </summary>
    [Display(Name = "Min Order Quantity", Description = "Minimum order quantity.")]
    public int? MinOrderQty { get; set; }

    /// <summary>
    /// Order quantity increment (e.g., must order in multiples of this number).
    /// </summary>
    [Display(Name = "Increment Quantity", Description = "Order quantity increment.")]
    public int? IncrementQty { get; set; }

    /// <summary>
    /// Lead time in days for delivery.
    /// </summary>
    [Display(Name = "Lead Time Days", Description = "Lead time in days for delivery.")]
    public int? LeadTimeDays { get; set; }

    /// <summary>
    /// Last purchase price from this supplier.
    /// </summary>
    [Display(Name = "Last Purchase Price", Description = "Last purchase price.")]
    public decimal? LastPurchasePrice { get; set; }

    /// <summary>
    /// Date of last purchase from this supplier.
    /// </summary>
    [Display(Name = "Last Purchase Date", Description = "Date of last purchase.")]
    public DateTime? LastPurchaseDate { get; set; }

    /// <summary>
    /// Indicates if this is the preferred supplier for this product.
    /// Only one supplier per product can be preferred.
    /// </summary>
    [Display(Name = "Preferred", Description = "Preferred supplier for this product.")]
    public bool Preferred { get; set; } = false;

    /// <summary>
    /// Additional notes about this supplier relationship.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; }
}
