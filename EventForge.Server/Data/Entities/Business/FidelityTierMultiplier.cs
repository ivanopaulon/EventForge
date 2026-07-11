using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

public class FidelityTierMultiplier : AuditableEntity
{
    public Guid CampaignId { get; set; }

    public FidelityPointsCampaign? Campaign { get; set; }

    public FidelityCardType CardType { get; set; }

    [Range(0.01, 100)]
    public decimal Multiplier { get; set; } = 1.0m;
}
