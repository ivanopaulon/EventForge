using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a Protocol 17 (ECR17) POS payment terminal configuration.
/// </summary>
public class PaymentTerminal : EventForge.Server.Data.Entities.Audit.AuditableEntity
{
    [Required]
    [MaxLength(100)]
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
