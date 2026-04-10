using EventForge.Server.Data.Entities.Audit;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Represents a blocked WhatsApp phone number.
/// </summary>
public class NumeroBloccato : AuditableEntity
{
    [Required][MaxLength(30)] public string NumeroDiTelefono { get; set; } = string.Empty;
    public DateTime BloccatoAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? Note { get; set; }
}
