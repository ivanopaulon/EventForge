namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Preview of a price change for a single product.
/// </summary>
public class PriceChangePreview
{
    /// <summary>
    /// Product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product code (SKU).
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Current price before update.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// New price after update.
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Absolute change amount (NewPrice - CurrentPrice).
    /// </summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>
    /// Change percentage ((NewPrice - CurrentPrice) / CurrentPrice * 100).
    /// </summary>
    public decimal ChangePercentage { get; set; }
}
