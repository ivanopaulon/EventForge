using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

public enum FidelityTransactionType
{
    Earned = 0,
    Redeemed = 1,
    Adjustment = 2
}

public class FidelityPointsTransaction : AuditableEntity
{
    [Required]
    public Guid FidelityCardId { get; set; }
    public FidelityCard FidelityCard { get; set; } = null!;

    public FidelityTransactionType TransactionType { get; set; } = FidelityTransactionType.Earned;

    public int Points { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
