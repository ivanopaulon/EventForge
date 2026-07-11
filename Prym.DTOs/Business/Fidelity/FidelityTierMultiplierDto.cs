namespace Prym.DTOs.Business.Fidelity;

public class FidelityTierMultiplierDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid TierId { get; set; }
    public string? TierName { get; set; }
    public decimal Multiplier { get; set; }
    public DateTime CreatedAt { get; set; }
}
