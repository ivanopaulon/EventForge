namespace EventForge.Client.Models.Fidelity;

/// <summary>
/// Tipo di carta fedeltà
/// </summary>
public enum FidelityCardType
{
    Bronze,
    Silver,
    Gold,
    Platinum
}

/// <summary>
/// Stato della carta fedeltà
/// </summary>
public enum FidelityCardStatus
{
    Active,
    Suspended,
    Expired,
    Revoked
}

/// <summary>
/// View Model per la carta fedeltà (client-side mock)
/// </summary>
public class FidelityCardViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CardNumber { get; set; } = string.Empty;

    public FidelityCardType Type { get; set; } = FidelityCardType.Bronze;

    public FidelityCardStatus Status { get; set; } = FidelityCardStatus.Active;

    public DateTime IssuedDate { get; set; } = DateTime.Now;

    public DateTime ValidFrom { get; set; } = DateTime.Now;

    public DateTime ValidTo { get; set; } = DateTime.Now.AddYears(1);

    public int CurrentPoints { get; set; } = 0;

    public int TotalPointsEarned { get; set; } = 0;

    public int TotalPointsRedeemed { get; set; } = 0;

    public decimal DiscountPercentage { get; set; } = 0;

    public bool HasPriorityAccess { get; set; } = false;

    public bool HasBirthdayBonus { get; set; } = false;

    public string? Notes { get; set; }
}
