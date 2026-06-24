namespace Prym.DTOs.Business.Fidelity;

public enum FidelityTransactionType
{
    Earned = 0,
    Redeemed = 1,
    Adjustment = 2
}

public class FidelityPointsTransactionDto
{
    public Guid Id { get; set; }
    public Guid FidelityCardId { get; set; }
    public FidelityTransactionType TransactionType { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
