using System;

namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// DTO for price history statistics.
/// </summary>
public class PriceHistoryStatistics
{
    /// <summary>
    /// Average price change percentage across all entries.
    /// </summary>
    public decimal AveragePriceChange { get; set; }

    /// <summary>
    /// Maximum price increase (as a percentage).
    /// </summary>
    public decimal MaxPriceIncrease { get; set; }

    /// <summary>
    /// Maximum price decrease (as a percentage, negative value).
    /// </summary>
    public decimal MaxPriceDecrease { get; set; }

    /// <summary>
    /// Total number of price changes.
    /// </summary>
    public int TotalChanges { get; set; }

    /// <summary>
    /// Date of the most recent price change.
    /// </summary>
    public DateTime? LastChangeDate { get; set; }

    /// <summary>
    /// Average absolute price change in currency units.
    /// </summary>
    public decimal AverageAbsolutePriceChange { get; set; }

    /// <summary>
    /// Total number of price increases.
    /// </summary>
    public int TotalIncreases { get; set; }

    /// <summary>
    /// Total number of price decreases.
    /// </summary>
    public int TotalDecreases { get; set; }
}
