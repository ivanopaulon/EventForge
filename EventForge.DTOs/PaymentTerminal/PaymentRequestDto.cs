using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PaymentTerminal;

public class PaymentRequestDto
{
    [Required(ErrorMessage = "L'importo è obbligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "L'importo deve essere compreso tra 0.01 e 999999.99 EUR.")]
    public decimal Amount { get; set; }

    [MaxLength(50, ErrorMessage = "L'ID riferimento non può superare 50 caratteri.")]
    public string? ReferenceId { get; set; }
}
