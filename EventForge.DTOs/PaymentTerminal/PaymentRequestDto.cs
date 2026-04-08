using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PaymentTerminal;

public class PaymentRequestDto
{
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }

    public string? ReferenceId { get; set; }
}
