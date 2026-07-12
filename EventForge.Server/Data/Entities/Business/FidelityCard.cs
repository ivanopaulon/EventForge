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

    [Obsolete("Deprecated: replaced by TierId. Retained indefinitely (no removal date planned) as a read-only historical/compatibility column for the FidelityCardType -> FidelityTier migration; do not use in new code.")]
    public FidelityCardType Type { get; set; } = FidelityCardType.Bronze;
    public FidelityCardStatus Status { get; set; } = FidelityCardStatus.Active;

    /// <summary>
    /// Current fidelity tier (level) of the card. Replaces the deprecated <see cref="Type"/> enum.
    /// </summary>
    public Guid? TierId { get; set; }
    public FidelityTier? Tier { get; set; }

    /// <summary>
    /// Timestamp when the card entered its current tier. Used by the periodic reevaluation to
    /// decide when a tier's evaluation window has elapsed.
    /// </summary>
    public DateTime? TierEnteredAt { get; set; }

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
