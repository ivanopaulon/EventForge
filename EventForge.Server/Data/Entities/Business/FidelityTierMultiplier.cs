using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

public class FidelityTierMultiplier : AuditableEntity
{
    public Guid CampaignId { get; set; }

    public FidelityPointsCampaign? Campaign { get; set; }

    /// <summary>
    /// Fidelity tier (level) this multiplier applies to. Replaces the former FidelityCardType enum.
    /// </summary>
    public Guid TierId { get; set; }

    public FidelityTier? Tier { get; set; }

    [Range(0.01, 100)]
    public decimal Multiplier { get; set; } = 1.0m;
}
