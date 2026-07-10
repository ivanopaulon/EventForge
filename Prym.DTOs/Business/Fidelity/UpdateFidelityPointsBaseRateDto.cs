using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class UpdateFidelityPointsBaseRateDto
{
    [Range(0.01, 1000)]
    public decimal Rate { get; set; } = 1.0m;

    public FidelityPointsRoundingMode RoundingMode { get; set; } = FidelityPointsRoundingMode.Floor;

    [Required]
    public DateTime EffectiveFrom { get; set; }
}
