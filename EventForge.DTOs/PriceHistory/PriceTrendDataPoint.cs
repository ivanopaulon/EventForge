using System;

namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// DTO representing a single data point in a price trend chart.
/// </summary>
public class PriceTrendDataPoint
{
    /// <summary>
    /// Date of the price change.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Price at this date.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Source of the change (Manual, BulkEdit, CSVImport, AutoUpdate).
    /// </summary>
    public string ChangeSource { get; set; } = string.Empty;

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "EUR";
}
