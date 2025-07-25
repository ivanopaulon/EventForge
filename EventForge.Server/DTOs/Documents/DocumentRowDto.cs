namespace EventForge.Server.DTOs.Documents;

/// <summary>
/// DTO for DocumentRow output/display operations.
/// </summary>
public class DocumentRowDto
{
    /// <summary>
    /// Unique identifier for the document row.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the document header.
    /// </summary>
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Row type (Product, Discount, Service, Bundle, etc.).
    /// </summary>
    public DocumentRowType RowType { get; set; }

    /// <summary>
    /// Parent row ID (for bundles or grouping).
    /// </summary>
    public Guid? ParentRowId { get; set; }

    /// <summary>
    /// Product code (SKU, barcode, etc.).
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product or service description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Line discount in percentage.
    /// </summary>
    public decimal LineDiscount { get; set; }

    /// <summary>
    /// VAT rate applied to the line (percentage).
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// VAT description.
    /// </summary>
    public string? VatDescription { get; set; }

    /// <summary>
    /// Indicates if the row is a gift.
    /// </summary>
    public bool IsGift { get; set; }

    /// <summary>
    /// Indicates if the row was manually entered.
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// Source warehouse for this row.
    /// </summary>
    public Guid? SourceWarehouseId { get; set; }

    /// <summary>
    /// Source warehouse name for display.
    /// </summary>
    public string? SourceWarehouseName { get; set; }

    /// <summary>
    /// Destination warehouse for this row.
    /// </summary>
    public Guid? DestinationWarehouseId { get; set; }

    /// <summary>
    /// Destination warehouse name for display.
    /// </summary>
    public string? DestinationWarehouseName { get; set; }

    /// <summary>
    /// Additional notes for the row.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Sort order for the row in the document.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Related station (optional, for logistics/traceability).
    /// </summary>
    public Guid? StationId { get; set; }

    /// <summary>
    /// Station name for display.
    /// </summary>
    public string? StationName { get; set; }

    /// <summary>
    /// Date and time when the row was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the row.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the row was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the row.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Total for the row after discount (calculated).
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Total VAT for the row (calculated).
    /// </summary>
    public decimal VatTotal { get; set; }

    /// <summary>
    /// Total discount applied to the row (calculated).
    /// </summary>
    public decimal DiscountTotal { get; set; }
}