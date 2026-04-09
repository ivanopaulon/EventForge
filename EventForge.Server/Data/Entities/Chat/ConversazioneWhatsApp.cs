using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Business;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Represents a WhatsApp conversation with a phone number / business party.
/// </summary>
public class ConversazioneWhatsApp : AuditableEntity
{
    [Required][MaxLength(30)] public string NumeroDiTelefono { get; set; } = string.Empty;
    public Guid? BusinessPartyId { get; set; }
    public BusinessParty? BusinessParty { get; set; }
    public StatoConversazioneWhatsApp Stato { get; set; } = StatoConversazioneWhatsApp.Attiva;
    public bool NumeroNonRiconosciuto { get; set; } = true;
    public DateTime UltimoMessaggioAt { get; set; } = DateTime.UtcNow;
    public ICollection<MessaggioWhatsApp> Messaggi { get; set; } = new List<MessaggioWhatsApp>();
}
