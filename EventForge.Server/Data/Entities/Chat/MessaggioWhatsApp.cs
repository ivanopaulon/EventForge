using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Auth;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Represents a single WhatsApp message within a conversation.
/// </summary>
public class MessaggioWhatsApp : AuditableEntity
{
    [Required] public Guid ConversazioneWhatsAppId { get; set; }
    public ConversazioneWhatsApp ConversazioneWhatsApp { get; set; } = null!;
    [MaxLength(4000)] public string Testo { get; set; } = string.Empty;
    public DirezioneMessaggio Direzione { get; set; }
    public StatoInvioMessaggio StatoInvio { get; set; } = StatoInvioMessaggio.Inviato;
    [MaxLength(200)] public string? WhatsAppMessageId { get; set; }
    public Guid? MittenteOperatoreId { get; set; }
    public User? MittenteOperatore { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsLetto { get; set; } = false;
}
