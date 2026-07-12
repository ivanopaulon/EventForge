using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class CreateFidelityCardDto
{
    [Required, MaxLength(50)]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Fidelity tier (level) to assign. Optional; defaults to the base tier when omitted.</summary>
    public Guid? TierId { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddYears(1);

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public bool HasPriorityAccess { get; set; }
    public bool HasBirthdayBonus { get; set; }
    public string? Notes { get; set; }
    public Guid? BusinessPartyId { get; set; }
}
