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
    /// Unit price.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
    [Display(Name = "Unit Price", Description = "Unit price of the product or service.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>
    /// Quantity.
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    [Display(Name = "Quantity", Description = "Quantity of the product or service.")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Line discount in percentage.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Line discount must be between 0 and 100.")]
    [Display(Name = "Line Discount (%)", Description = "Discount applied to the row in percentage.")]
    public decimal LineDiscount { get; set; } = 0m;

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
    /// Total for the row after discount (not mapped).
    /// </summary>
    [NotMapped]
    [Display(Name = "Line Total", Description = "Total for the row after discount.")]
    public decimal LineTotal => Math.Round((UnitPrice * Quantity) * (1 - LineDiscount / 100), 2);

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
    public decimal DiscountTotal => Math.Round((UnitPrice * Quantity) * (LineDiscount / 100), 2);

    /// <summary>
    /// Gets or sets the collection of summary links that include this document row.
    /// </summary>
    [NotMapped]
    public ICollection<DocumentSummaryLink> IncludedInSummaries { get; set; } = new List<DocumentSummaryLink>();
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