namespace Prym.DTOs.Business.Fidelity;

public class FidelityPointsBaseRateDto
{
    public Guid Id { get; set; }
    public decimal Rate { get; set; }
    public FidelityPointsRoundingMode RoundingMode { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}
