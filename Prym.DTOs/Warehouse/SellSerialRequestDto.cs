using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Warehouse;

public class SellSerialRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
}
