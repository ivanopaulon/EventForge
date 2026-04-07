namespace EventForge.DTOs.Products;

/// <summary>
/// Request DTO for CSV import operations (without file - used for serialization).
/// The file is passed separately via multipart form data.
/// </summary>
public class CsvImportRequest
{
    /// <summary>
    /// Import options and settings.
    /// </summary>
    public CsvImportOptions Options { get; set; } = new();
}
