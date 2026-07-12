using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class CreateFidelityTierDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 999999999)]
    public decimal? MinimumSpendThreshold { get; set; }

    [Range(1, 120)]
    public int EvaluationPeriodMonths { get; set; } = 12;
}
