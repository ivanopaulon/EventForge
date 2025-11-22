namespace EventForge.DTOs.Products;

/// <summary>
/// Represents an error encountered during CSV import.
/// </summary>
public class CsvImportError
{
    /// <summary>
    /// Row number where the error occurred.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Product code from the CSV row.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Type of error (e.g., Validation, NotFound, DuplicateKey).
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
