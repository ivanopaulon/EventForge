namespace EventForge.DTOs.Products;

/// <summary>
/// Options for CSV import operations.
/// </summary>
public class CsvImportOptions
{
    /// <summary>
    /// Whether to update existing product-supplier relationships.
    /// </summary>
    public bool UpdateExisting { get; set; } = true;

    /// <summary>
    /// Whether to create new products if not found.
    /// </summary>
    public bool CreateNew { get; set; } = true;

    /// <summary>
    /// Whether to set imported products as preferred suppliers.
    /// </summary>
    public bool SetAsPreferred { get; set; } = false;

    /// <summary>
    /// Whether to skip duplicate entries.
    /// </summary>
    public bool SkipDuplicates { get; set; } = false;

    /// <summary>
    /// Default currency for products without currency specified.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Column mapping configuration.
    /// </summary>
    public ColumnMapping ColumnMapping { get; set; } = new();
}
