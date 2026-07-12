using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class CreateFidelityTierMultiplierDto
{
    [Required]
    public Guid CampaignId { get; set; }

    [Required]
    public Guid TierId { get; set; }

    [Range(0.01, 100)]
    public decimal Multiplier { get; set; } = 1.0m;
}
