using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

public enum FidelityPointsRoundingMode
{
    Floor = 0,
    Ceiling = 1,
    Nearest = 2
}

public class FidelityPointsBaseRate : AuditableEntity
{
    [Range(0.01, 1000)]
    public decimal Rate { get; set; } = 1.0m;

    public FidelityPointsRoundingMode RoundingMode { get; set; } = FidelityPointsRoundingMode.Floor;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
