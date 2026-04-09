using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.External.WhatsApp;

public class WhatsAppTextBody
{
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppStatus
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public class WhatsAppMessage
{
    public string Id { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public WhatsAppTextBody? Text { get; set; }
}

public class WhatsAppValue
{
    public List<WhatsAppMessage>? Messages { get; set; }
    public List<WhatsAppStatus>? Statuses { get; set; }
}

public class WhatsAppChange
{
    public WhatsAppValue? Value { get; set; }
}

public class WhatsAppEntry
{
    public List<WhatsAppChange>? Changes { get; set; }
}

public class WhatsAppInboundPayloadDto
{
    public List<WhatsAppEntry>? Entry { get; set; }
}

public class MessaggioWhatsAppDto
{
    public Guid Id { get; set; }
    public Guid ConversazioneId { get; set; }
    public string Testo { get; set; } = string.Empty;
    public DirezioneMessaggio Direzione { get; set; }
    public DateTime Timestamp { get; set; }
    public string? NomeOperatore { get; set; }
    public StatoInvioMessaggio StatoInvio { get; set; }
    public bool IsUscente => Direzione == DirezioneMessaggio.Uscente;
}

public class ConversazioneWhatsAppDto
{
    public Guid Id { get; set; }
    public string NumeroDiTelefono { get; set; } = string.Empty;
    public string? NomeAnagrafica { get; set; }
    public bool NumeroNonRiconosciuto { get; set; }
    public string? UltimoMessaggio { get; set; }
    public DateTime? UltimoMessaggioAt { get; set; }
    public StatoConversazioneWhatsApp StatoConversazione { get; set; }
    public int MessaggiNonLetti { get; set; }
    public bool IsWhatsApp { get; set; } = true;
}

/// <summary>DTO for creating a new BusinessParty from an unrecognized WhatsApp number.</summary>
public class NuovaAnagraficaDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Maps to BusinessPartyType enum: 0 = Cliente (default)</summary>
    public int PartyType { get; set; } = 0;

    public string? TaxCode { get; set; }
    public string? VatNumber { get; set; }

    /// <summary>Pre-compiled with the WhatsApp phone number.</summary>
    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }
}

public class AssegnaNumeroDto
{
    [Required]
    public string NumeroDiTelefono { get; set; } = string.Empty;

    public Guid? BusinessPartyId { get; set; }
    public NuovaAnagraficaDto? NuovaAnagrafica { get; set; }
}
