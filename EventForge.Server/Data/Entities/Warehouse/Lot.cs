using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents a lot/batch of products for traceability purposes.
/// A lot groups multiple units of the same product with shared characteristics.
/// </summary>
public class Lot : AuditableEntity
{
    /// <summary>
    /// Unique lot code/number.
    /// </summary>
    [Required(ErrorMessage = "Lot code is required.")]
    [StringLength(50, ErrorMessage = "Lot code cannot exceed 50 characters.")]
    [Display(Name = "Lot Code", Description = "Unique lot code/number.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Product this lot belongs to.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product this lot belongs to.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Production date of the lot.
    /// </summary>
    [Display(Name = "Production Date", Description = "Production date of the lot.")]
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// Expiry date of the lot.
    /// </summary>
    [Display(Name = "Expiry Date", Description = "Expiry date of the lot.")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Supplier/vendor who provided this lot.
    /// </summary>
    [Display(Name = "Supplier", Description = "Supplier/vendor who provided this lot.")]
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// Navigation property for the supplier.
    /// </summary>
    public BusinessParty? Supplier { get; set; }

    /// <summary>
    /// Original quantity received for this lot.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    [Display(Name = "Original Quantity", Description = "Original quantity received for this lot.")]
    public decimal OriginalQuantity { get; set; }

    /// <summary>
    /// Current available quantity for this lot.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Available quantity must be non-negative.")]
    [Display(Name = "Available Quantity", Description = "Current available quantity for this lot.")]
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// Status of the lot.
    /// </summary>
    [Display(Name = "Status", Description = "Current status of the lot.")]
    public LotStatus Status { get; set; } = LotStatus.Active;

    /// <summary>
    /// Notes or additional information about the lot.
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Notes or additional information about the lot.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Quality control status for the lot.
    /// </summary>
    [Display(Name = "Quality Status", Description = "Quality control status for the lot.")]
    public QualityStatus QualityStatus { get; set; } = QualityStatus.Pending;

    /// <summary>
    /// Barcode for the lot (for scanning purposes).
    /// </summary>
    [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters.")]
    [Display(Name = "Barcode", Description = "Barcode for the lot.")]
    public string? Barcode { get; set; }

    /// <summary>
    /// Country of origin for this lot.
    /// </summary>
    [StringLength(50, ErrorMessage = "Country of origin cannot exceed 50 characters.")]
    [Display(Name = "Country of Origin", Description = "Country of origin for this lot.")]
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// Stock entries for this lot across different locations.
    /// </summary>
    public ICollection<Stock> StockEntries { get; set; } = new List<Stock>();

    /// <summary>
    /// Serial numbers associated with this lot.
    /// </summary>
    public ICollection<Serial> Serials { get; set; } = new List<Serial>();

    /// <summary>
    /// Stock movements involving this lot.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    /// <summary>
    /// Quality control records for this lot.
    /// </summary>
    public ICollection<QualityControl> QualityControls { get; set; } = new List<QualityControl>();
}

/// <summary>
/// Status for lots.
/// </summary>
public enum LotStatus
{
    Active,      // Lot is active and available
    Blocked,     // Lot is blocked (quality issues, recall, etc.)
    Expired,     // Lot has expired
    Consumed,    // Lot has been fully consumed
    Recalled     // Lot has been recalled
}

/// <summary>
/// Quality control status for lots.
/// </summary>
public enum QualityStatus
{
    Pending,     // Quality control pending
    Approved,    // Quality control approved
    Rejected,    // Quality control rejected
    OnHold       // Quality control on hold for review
}