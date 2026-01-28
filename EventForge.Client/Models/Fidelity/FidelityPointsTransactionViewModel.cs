namespace EventForge.Client.Models.Fidelity;

/// <summary>
/// Tipo di transazione punti
/// </summary>
public enum TransactionType
{
    Earned,
    Redeemed,
    Adjusted,
    Expired
}

/// <summary>
/// View Model per transazione punti fedeltà (client-side mock)
/// </summary>
public class FidelityPointsTransactionViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid FidelityCardId { get; set; }
    
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    
    public TransactionType Type { get; set; }
    
    public int Points { get; set; }
    
    public int BalanceAfter { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? Reference { get; set; }
}
