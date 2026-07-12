namespace Prym.DTOs.Business.Fidelity;

public class FidelityTierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Minimum trailing-window spend required to reach this tier (from the linked rule).</summary>
    public decimal? MinimumSpendThreshold { get; set; }

    /// <summary>Trailing evaluation window in months (from the linked rule).</summary>
    public int EvaluationPeriodMonths { get; set; } = 12;

    public DateTime CreatedAt { get; set; }
}
