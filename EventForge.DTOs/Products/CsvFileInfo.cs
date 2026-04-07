namespace EventForge.DTOs.Products;

/// <summary>
/// Information about a CSV file.
/// </summary>
public class CsvFileInfo
{
    /// <summary>
    /// Name of the CSV file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Total number of rows in the CSV.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Detected encoding of the file.
    /// </summary>
    public string Encoding { get; set; } = string.Empty;

    /// <summary>
    /// Detected delimiter character.
    /// </summary>
    public string Delimiter { get; set; } = string.Empty;
}
