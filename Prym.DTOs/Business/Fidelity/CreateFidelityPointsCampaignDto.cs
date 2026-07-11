using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class CreateFidelityPointsCampaignDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0.01, 100)]
    public decimal Multiplier { get; set; } = 1.0m;

    public FidelityPointsRoundingMode RoundingMode { get; set; } = FidelityPointsRoundingMode.Floor;

    public bool IsActive { get; set; } = true;

    public string? ProductIdsJSON { get; set; }

    public string? CategoryIdsJSON { get; set; }
}
