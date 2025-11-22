namespace EventForge.DTOs.Products;

/// <summary>
/// Mapping of CSV columns to product fields.
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// CSV column name for product code (required).
    /// </summary>
    public string? ProductCodeColumn { get; set; }

    /// <summary>
    /// CSV column name for product name (optional).
    /// </summary>
    public string? ProductNameColumn { get; set; }

    /// <summary>
    /// CSV column name for unit cost (required).
    /// </summary>
    public string? UnitCostColumn { get; set; }

    /// <summary>
    /// CSV column name for lead time in days (optional).
    /// </summary>
    public string? LeadTimeDaysColumn { get; set; }

    /// <summary>
    /// CSV column name for minimum order quantity (optional).
    /// </summary>
    public string? MinOrderQuantityColumn { get; set; }

    /// <summary>
    /// CSV column name for currency (optional).
    /// </summary>
    public string? CurrencyColumn { get; set; }

    /// <summary>
    /// CSV column name for notes (optional).
    /// </summary>
    public string? NotesColumn { get; set; }
}
