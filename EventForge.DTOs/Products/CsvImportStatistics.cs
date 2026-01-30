namespace EventForge.DTOs.Products;

/// <summary>
/// Statistics about a CSV import operation.
/// </summary>
public class CsvImportStatistics
{
    /// <summary>
    /// Total value of all imported products.
    /// </summary>
    public decimal TotalImportedValue { get; set; }

    /// <summary>
    /// Average price change across all updated products.
    /// </summary>
    public decimal AveragePriceChange { get; set; }

    /// <summary>
    /// Time taken to process the import.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}
