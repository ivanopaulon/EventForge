namespace EventForge.DTOs.Products;

/// <summary>
/// Represents a preview row from a CSV file.
/// </summary>
public class CsvPreviewRow
{
    /// <summary>
    /// Row number in the CSV file.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Dictionary of column name to value for this row.
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>
    /// Whether this row has validation errors.
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// Summary of errors for this row.
    /// </summary>
    public string? ErrorSummary { get; set; }
}
