using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

public enum FidelityCardType
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}

public enum FidelityCardStatus
{
    Active = 0,
    Suspended = 1,
    Expired = 2,
    Revoked = 3
}

public class FidelityCard : AuditableEntity
{
    [Required, MaxLength(50)]
    public string CardNumber { get; set; } = string.Empty;

    public FidelityCardType Type { get; set; } = FidelityCardType.Bronze;
    public FidelityCardStatus Status { get; set; } = FidelityCardStatus.Active;

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddYears(1);

    public int CurrentPoints { get; set; }
    public int TotalPointsEarned { get; set; }
    public int TotalPointsRedeemed { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public bool HasPriorityAccess { get; set; }
    public bool HasBirthdayBonus { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Guid? BusinessPartyId { get; set; }
    public BusinessParty? BusinessParty { get; set; }

    public ICollection<FidelityPointsTransaction> Transactions { get; set; } = new List<FidelityPointsTransaction>();
}
