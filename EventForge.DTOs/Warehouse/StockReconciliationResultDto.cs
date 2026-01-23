namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Complete result of stock reconciliation calculation
/// </summary>
public class StockReconciliationResultDto
{
    /// <summary>
    /// List of reconciliation items
    /// </summary>
    public List<StockReconciliationItemDto> Items { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public StockReconciliationSummaryDto Summary { get; set; } = new();
}
