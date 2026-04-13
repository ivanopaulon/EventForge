using Prym.DTOs.Chat;
using Prym.DTOs.External.WhatsApp;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Chat;

namespace EventForge.Server.Services.External.WhatsApp;

public interface IWhatsAppConversazioneService
{
    Task GestisciMessaggioEntranteAsync(string numero, string testo, string msgId, DateTime timestamp, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ChatThread> GetOrCreateConversazioneAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default);
    Task<BusinessParty?> TrovaBusinessPartyByTelefonoAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default);
    Task AssegnaAdBusinessPartyEsistenteAsync(string numero, Guid businessPartyId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<BusinessParty> CreaBusinessPartyEAssegnaAsync(AssegnaNumeroDto dto, Guid tenantId, string currentUser, CancellationToken cancellationToken = default);
    Task BloccaNumeroAsync(string numero, string? note, Guid tenantId, string currentUser, CancellationToken cancellationToken = default);
    Task<ChatMessage> InviaRispostaOperatoreAsync(Guid chatThreadId, string testo, Guid operatoreId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<ChatResponseDto>> GetConversazioniAttiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<ChatMessageDto>> GetMessaggiAsync(Guid chatThreadId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AggiornaStatoMessaggioAsync(string whatsAppMessageId, WhatsAppDeliveryStatus stato, Guid tenantId, CancellationToken cancellationToken = default);
}
