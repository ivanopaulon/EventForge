using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PaymentTerminal;

public class UpdatePaymentTerminalDto
{
    [Required(ErrorMessage = "Il nome del terminale è obbligatorio.")]
    [MaxLength(100, ErrorMessage = "Il nome non può superare 100 caratteri.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "La descrizione non può superare 200 caratteri.")]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    [Required(ErrorMessage = "Il tipo di connessione è obbligatorio.")]
    [MaxLength(20, ErrorMessage = "Il tipo di connessione non può superare 20 caratteri.")]
    public string ConnectionType { get; set; } = "Tcp";

    [MaxLength(45, ErrorMessage = "L'indirizzo IP non può superare 45 caratteri.")]
    public string? IpAddress { get; set; }

    [Range(1, 65535, ErrorMessage = "La porta deve essere compresa tra 1 e 65535.")]
    public int Port { get; set; } = 60000;

    public Guid? AgentId { get; set; }

    [Range(1000, 120000, ErrorMessage = "Il timeout deve essere compreso tra 1000 e 120000 millisecondi.")]
    public int TimeoutMs { get; set; } = 30000;

    public bool AmountConfirmationRequired { get; set; }

    [MaxLength(8, ErrorMessage = "L'ID terminale non può superare 8 caratteri.")]
    public string? TerminalId { get; set; }
}
