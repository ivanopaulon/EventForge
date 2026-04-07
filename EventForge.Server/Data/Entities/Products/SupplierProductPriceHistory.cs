using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Products;

/// <summary>
/// Represents the price history for a supplier product relationship.
/// Tracks all changes to unit cost and lead time over time for audit and analysis purposes.
/// </summary>
public class SupplierProductPriceHistory : AuditableEntity
{
    /// <summary>
    /// ProductSupplier relationship identifier (foreign key).
    /// </summary>
    [Required(ErrorMessage = "The product supplier relationship is required.")]
    [Display(Name = "Product Supplier", Description = "Product supplier relationship identifier.")]
    public Guid ProductSupplierId { get; set; }

    /// <summary>
    /// Supplier identifier for quick lookups.
    /// </summary>
    [Required(ErrorMessage = "The supplier is required.")]
    [Display(Name = "Supplier", Description = "Supplier identifier.")]
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Product identifier for quick lookups.
    /// </summary>
    [Required(ErrorMessage = "The product is required.")]
    [Display(Name = "Product", Description = "Product identifier.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Previous unit cost before the change.
    /// </summary>
    [Display(Name = "Old Unit Cost", Description = "Previous unit cost.")]
    public decimal OldUnitCost { get; set; }

    /// <summary>
    /// New unit cost after the change.
    /// </summary>
    [Display(Name = "New Unit Cost", Description = "New unit cost.")]
    public decimal NewUnitCost { get; set; }

    /// <summary>
    /// Absolute difference in price (NewUnitCost - OldUnitCost).
    /// </summary>
    [Display(Name = "Price Change", Description = "Absolute difference in price.")]
    public decimal PriceChange { get; set; }

    /// <summary>
    /// Percentage change in price.
    /// </summary>
    [Display(Name = "Price Change Percentage", Description = "Percentage change in price.")]
    public decimal PriceChangePercentage { get; set; }

    /// <summary>
    /// Currency code for the prices.
    /// </summary>
    [MaxLength(10, ErrorMessage = "The currency cannot exceed 10 characters.")]
    [Display(Name = "Currency", Description = "Currency code.")]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Previous lead time in days.
    /// </summary>
    [Display(Name = "Old Lead Time Days", Description = "Previous lead time in days.")]
    public int? OldLeadTimeDays { get; set; }

    /// <summary>
    /// New lead time in days.
    /// </summary>
    [Display(Name = "New Lead Time Days", Description = "New lead time in days.")]
    public int? NewLeadTimeDays { get; set; }

    /// <summary>
    /// Date and time when the change occurred.
    /// </summary>
    [Required]
    [Display(Name = "Changed At", Description = "Date and time of the change.")]
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// User who made the change (foreign key).
    /// </summary>
    [Required]
    [Display(Name = "Changed By User", Description = "User who made the change.")]
    public Guid ChangedByUserId { get; set; }

    /// <summary>
    /// Source of the change (Manual, BulkEdit, CSVImport, AutoUpdate).
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "The change source cannot exceed 50 characters.")]
    [Display(Name = "Change Source", Description = "Source of the change.")]
    public string ChangeSource { get; set; } = "Manual";

    /// <summary>
    /// Optional user-provided reason for the change.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The change reason cannot exceed 500 characters.")]
    [Display(Name = "Change Reason", Description = "Reason for the change.")]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Additional notes about the change.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// ProductSupplier relationship.
    /// </summary>
    public ProductSupplier? ProductSupplier { get; set; }

    /// <summary>
    /// User who made the change.
    /// </summary>
    public User? ChangedByUser { get; set; }

    /// <summary>
    /// Supplier entity.
    /// </summary>
    public BusinessParty? Supplier { get; set; }

    /// <summary>
    /// Product entity.
    /// </summary>
    public Product? Product { get; set; }
}
