using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Business.Fidelity;

public class ModifyFidelityPointsDto
{
    [Range(1, 100000)]
    public int Points { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}
