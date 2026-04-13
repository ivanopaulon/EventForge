using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.External.WhatsApp;

// ─── Webhook payload types (inbound from Meta API) ───────────────────────────

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

// ─── API request DTOs ─────────────────────────────────────────────────────────

/// <summary>DTO for assigning an unrecognised WhatsApp number to a BusinessParty.</summary>
public class AssegnaNumeroDto
{
    [Required]
    public string NumeroDiTelefono { get; set; } = string.Empty;

    public Guid? BusinessPartyId { get; set; }
    public NuovaAnagraficaDto? NuovaAnagrafica { get; set; }
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

// ─── WhatsApp configuration DTO ──────────────────────────────────────────────

/// <summary>DTO for reading and writing the WhatsApp Business Cloud API configuration.</summary>
public class WhatsAppConfigDto
{
    /// <summary>WhatsApp Business Phone Number ID from Meta Developer portal.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// Permanent access token for the WhatsApp Business Account.
    /// Returned masked (e.g. "•••••abc") on GET; send the full value to update.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Webhook verify token configured in the Meta App dashboard.</summary>
    public string VerifyToken { get; set; } = string.Empty;

    /// <summary>Meta Graph API version (default: v19.0).</summary>
    public string ApiVersion { get; set; } = "v19.0";

    /// <summary>Whether the WhatsApp integration is enabled.</summary>
    public bool IsEnabled { get; set; } = false;
}
