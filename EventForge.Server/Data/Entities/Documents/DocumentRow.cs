using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Documents;


/// <summary>
/// Represents a line item in the document.
/// </summary>
public class DocumentRow : AuditableEntity
{
    /// <summary>
    /// Reference to the document header.
    /// </summary>
    [Required(ErrorMessage = "The document header ID is required.")]
    [Display(Name = "Document Header", Description = "Reference to the document header.")]
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header.
    /// </summary>
    [Display(Name = "Document Header", Description = "Navigation property for the document header.")]
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Row type (e.g., Product, Discount, Service, Bundle, Other).
    /// </summary>
    [Display(Name = "Row Type", Description = "Type of the row (Product, Discount, Service, Bundle, etc.).")]
    public DocumentRowType RowType { get; set; } = DocumentRowType.Product;

    /// <summary>
    /// Parent row ID (for bundles or grouping, optional).
    /// </summary>
    [Display(Name = "Parent Row", Description = "Parent row ID (for grouping/bundles).")]
    public Guid? ParentRowId { get; set; }

    /// <summary>
    /// Product code (SKU, barcode, etc.).
    /// </summary>
    [StringLength(50, ErrorMessage = "Product code cannot exceed 50 characters.")]
    [Display(Name = "Product Code", Description = "Product code (SKU, barcode, etc.).")]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product identifier (for traceability and inventory operations).
    /// </summary>
    [Display(Name = "Product ID", Description = "Product identifier for traceability.")]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Storage location identifier (for inventory operations).
    /// </summary>
    [Display(Name = "Location ID", Description = "Storage location identifier for inventory operations.")]
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Navigation property for the storage location.
    /// </summary>
    public StorageLocation? Location { get; set; }

    /// <summary>
    /// Product or service description.
    /// </summary>
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Product or service description.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measure.
    /// </summary>
    [StringLength(10, ErrorMessage = "Unit of measure cannot exceed 10 characters.")]
    [Display(Name = "Unit of Measure", Description = "Unit of measure (e.g., Pcs, Kg).")]
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Unit of measure identifier.
    /// </summary>
    [Display(Name = "Unit of Measure ID", Description = "Reference to the unit of measure.")]
    public Guid? UnitOfMeasureId { get; set; }

    /// <summary>
    /// Navigation property for the unit of measure.
    /// </summary>
    public UM? UnitOfMeasureEntity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
    [Display(Name = "Unit Price", Description = "Unit price of the product or service.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>
    /// Base quantity normalized to the product's base unit.
    /// </summary>
    [Display(Name = "Base Quantity", Description = "Quantity normalized to the product's base unit.")]
    public decimal? BaseQuantity { get; set; }

    /// <summary>
    /// Base unit price normalized to the product's base unit.
    /// </summary>
    [Display(Name = "Base Unit Price", Description = "Unit price normalized to the product's base unit.")]
    public decimal? BaseUnitPrice { get; set; }

    /// <summary>
    /// Base unit of measure identifier (the product's base unit).
    /// </summary>
    [Display(Name = "Base Unit of Measure ID", Description = "Reference to the base unit of measure.")]
    public Guid? BaseUnitOfMeasureId { get; set; }

    /// <summary>
    /// Quantity.
    /// </summary>
    [Range(0.0001, 10000, ErrorMessage = "Quantity must be between 0.0001 and 10000.")]
    [Display(Name = "Quantity", Description = "Quantity of the product or service.")]
    public decimal Quantity { get; set; } = 1m;

    /// <summary>
    /// Line discount in percentage.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Line discount must be between 0 and 100.")]
    [Display(Name = "Line Discount (%)", Description = "Discount applied to the row in percentage.")]
    public decimal LineDiscount { get; set; } = 0m;

    /// <summary>
    /// Line discount value (absolute amount).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Line discount value must be non-negative.")]
    [Display(Name = "Line Discount Value", Description = "Discount applied to the row as an absolute amount.")]
    public decimal LineDiscountValue { get; set; } = 0m;

    /// <summary>
    /// Discount type (percentage or value).
    /// </summary>
    [Display(Name = "Discount Type", Description = "Type of discount applied (percentage or value).")]
    public EventForge.DTOs.Common.DiscountType DiscountType { get; set; } = EventForge.DTOs.Common.DiscountType.Percentage;

    /// <summary>
    /// VAT rate applied to the line (percentage).
    /// </summary>
    [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100.")]
    [Display(Name = "VAT Rate (%)", Description = "VAT rate applied to the row.")]
    public decimal VatRate { get; set; } = 0m;

    /// <summary>
    /// VAT description (e.g., "VAT 22%").
    /// </summary>
    [StringLength(30, ErrorMessage = "VAT description cannot exceed 30 characters.")]
    [Display(Name = "VAT Description", Description = "Description of the VAT rate.")]
    public string? VatDescription { get; set; }

    /// <summary>
    /// Indicates if the row is a gift.
    /// </summary>
    [Display(Name = "Is Gift", Description = "Indicates if the row is a gift.")]
    public bool IsGift { get; set; } = false;

    /// <summary>
    /// Indicates if the row was manually entered.
    /// </summary>
    [Display(Name = "Is Manual", Description = "Indicates if the row was manually entered.")]
    public bool IsManual { get; set; } = false;

    /// <summary>
    /// Source warehouse for this row (overrides header if set).
    /// </summary>
    [Display(Name = "Source Warehouse", Description = "Source warehouse for this row (overrides header if set).")]
    public Guid? SourceWarehouseId { get; set; }

    /// <summary>
    /// Navigation property for the source warehouse.
    /// </summary>
    public StorageFacility? SourceWarehouse { get; set; }

    /// <summary>
    /// Destination warehouse for this row (overrides header if set).
    /// </summary>
    [Display(Name = "Destination Warehouse", Description = "Destination warehouse for this row (overrides header if set).")]
    public Guid? DestinationWarehouseId { get; set; }

    /// <summary>
    /// Navigation property for the destination warehouse.
    /// </summary>
    public StorageFacility? DestinationWarehouse { get; set; }

    /// <summary>
    /// Additional notes for the row.
    /// </summary>
    [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the row.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Sort order for the row in the document.
    /// </summary>
    [Display(Name = "Sort Order", Description = "Sort order for the row in the document.")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Foreign key to the related station (optional, for logistics/traceability).
    /// </summary>
    [Display(Name = "Station", Description = "Related station (optional, for logistics/traceability).")]
    public Guid? StationId { get; set; }

    /// <summary>
    /// Navigation property for the station.
    /// </summary>
    public Station? Station { get; set; }

    /// <summary>
    /// Calculates the actual discount amount, clamped to subtotal to prevent negative totals.
    /// </summary>
    private decimal GetEffectiveDiscount()
    {
        var subtotal = UnitPrice * Quantity;
        var discount = DiscountType == EventForge.DTOs.Common.DiscountType.Percentage
            ? subtotal * (LineDiscount / 100)
            : LineDiscountValue;

        // Ensure discount doesn't exceed subtotal (prevent negative line totals)
        return Math.Min(discount, subtotal);
    }

    /// <summary>
    /// Total for the row after discount (not mapped).
    /// </summary>
    [NotMapped]
    [Display(Name = "Line Total", Description = "Total for the row after discount.")]
    public decimal LineTotal
    {
        get
        {
            var subtotal = UnitPrice * Quantity;
            return Math.Round(subtotal - GetEffectiveDiscount(), 2);
        }
    }

    /// <summary>
    /// VAT total for the row (not mapped).
    /// </summary>
    [NotMapped]
    [Display(Name = "VAT Total", Description = "Total VAT for the row.")]
    public decimal VatTotal => Math.Round(LineTotal * VatRate / 100, 2);

    /// <summary>
    /// Discount total for the row (not mapped).
    /// </summary>
    [NotMapped]
    [Display(Name = "Discount Total", Description = "Total discount applied to the row.")]
    public decimal DiscountTotal => Math.Round(GetEffectiveDiscount(), 2);

    /// <summary>
    /// Gets or sets the collection of summary links that include this document row.
    /// </summary>
    [NotMapped]
    public ICollection<DocumentSummaryLink> IncludedInSummaries { get; set; } = new List<DocumentSummaryLink>();

    /// <summary>
    /// Document attachments linked to this row
    /// </summary>
    [Display(Name = "Attachments", Description = "Document attachments linked to this row.")]
    public ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();

    /// <summary>
    /// Document comments linked to this row
    /// </summary>
    [Display(Name = "Comments", Description = "Document comments linked to this row.")]
    public ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();
}

/// <summary>
/// Document row type enumeration.
/// </summary>
public enum DocumentRowType
{
    Product,
    Discount,
    Service,
    Bundle,
    Other
}