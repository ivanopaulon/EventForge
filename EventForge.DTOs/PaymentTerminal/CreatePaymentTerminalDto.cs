using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PaymentTerminal;

public class CreatePaymentTerminalDto
{
    [Required(ErrorMessage = "Il nome del terminale è obbligatorio.")]
    [MaxLength(100, ErrorMessage = "Il nome non può superare 100 caratteri.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    [Required]
    [MaxLength(20)]
    public string ConnectionType { get; set; } = "Tcp";

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 60000;

    public Guid? AgentId { get; set; }

    [Range(1000, 120000)]
    public int TimeoutMs { get; set; } = 30000;

    public bool AmountConfirmationRequired { get; set; }

    [MaxLength(8)]
    public string? TerminalId { get; set; }
}
