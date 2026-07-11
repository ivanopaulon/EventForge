namespace Prym.DTOs.Business.Fidelity;

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

    /// <summary>Current fidelity tier (level) identifier. Replaces the old FidelityCardType enum.</summary>
    public Guid? TierId { get; set; }

    /// <summary>Display name of the current tier, resolved from the tier lookup.</summary>
    public string? TierName { get; set; }

    /// <summary>Optional tier colour for display.</summary>
    public string? TierColor { get; set; }

    /// <summary>Optional tier icon for display.</summary>
    public string? TierIcon { get; set; }

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
