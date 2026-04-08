namespace EventForge.DTOs.PaymentTerminal;

public class PaymentTerminalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string ConnectionType { get; set; } = "Tcp";
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 60000;
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public int TimeoutMs { get; set; } = 30000;
    public bool AmountConfirmationRequired { get; set; }
    public string? TerminalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
