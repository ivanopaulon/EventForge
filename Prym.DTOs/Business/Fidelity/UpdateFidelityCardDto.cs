using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class UpdateFidelityCardDto
{
    /// <summary>Fidelity tier (level) to assign.</summary>
    public Guid? TierId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public bool HasPriorityAccess { get; set; }
    public bool HasBirthdayBonus { get; set; }
    public string? Notes { get; set; }
}
