namespace EventForge.DTOs.Products;

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public class CsvImportResult
{
    /// <summary>
    /// Whether the import was successful overall.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of rows in the CSV file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of products created.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of products updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of rows skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of rows with errors.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of errors encountered during import.
    /// </summary>
    public List<CsvImportError> Errors { get; set; } = new();

    /// <summary>
    /// Import statistics.
    /// </summary>
    public CsvImportStatistics Statistics { get; set; } = new();
}
