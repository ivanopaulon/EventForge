namespace EventForge.Client.Models.Documents;

/// <summary>
/// Represents a single entry in continuous scan mode
/// </summary>
public class ContinuousScanEntry
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime Timestamp { get; set; }
}
