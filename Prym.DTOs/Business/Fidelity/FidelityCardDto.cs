namespace Prym.DTOs.Business.Fidelity;

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

public class FidelityCardDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public FidelityCardType Type { get; set; }
    public FidelityCardStatus Status { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int CurrentPoints { get; set; }
    public int TotalPointsEarned { get; set; }
    public int TotalPointsRedeemed { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool HasPriorityAccess { get; set; }
    public bool HasBirthdayBonus { get; set; }
    public string? Notes { get; set; }
    public Guid? BusinessPartyId { get; set; }
    public DateTime CreatedAt { get; set; }
}
