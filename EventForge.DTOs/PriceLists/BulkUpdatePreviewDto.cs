using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Preview result of a bulk price update operation.
/// </summary>
public class BulkUpdatePreviewDto
{
    /// <summary>
    /// Number of items that will be affected by the update.
    /// </summary>
    public int AffectedCount { get; set; }

    /// <summary>
    /// List of price changes for preview.
    /// </summary>
    public List<PriceChangePreview> Changes { get; set; } = new();

    /// <summary>
    /// Total current value of all affected items.
    /// </summary>
    public decimal TotalCurrentValue { get; set; }

    /// <summary>
    /// Total new value of all affected items after update.
    /// </summary>
    public decimal TotalNewValue { get; set; }

    /// <summary>
    /// Average percentage increase across all items.
    /// </summary>
    public decimal AverageIncreasePercentage { get; set; }
}
