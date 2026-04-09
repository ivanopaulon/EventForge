using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Chat;

namespace EventForge.Server.Services.External.WhatsApp;

public interface IWhatsAppConversazioneService
{
    Task GestisciMessaggioEntranteAsync(string numero, string testo, string msgId, DateTime timestamp, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ConversazioneWhatsApp> GetOrCreateConversazioneAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default);
    Task<BusinessParty?> TrovaBusinessPartyByTelefonoAsync(string numero, Guid tenantId, CancellationToken cancellationToken = default);
    Task AssegnaAdBusinessPartyEsistenteAsync(string numero, Guid businessPartyId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<BusinessParty> CreaBusinessPartyEAssegnaAsync(AssegnaNumeroDto dto, Guid tenantId, string currentUser, CancellationToken cancellationToken = default);
    Task BloccaNumeroAsync(string numero, string? note, Guid tenantId, string currentUser, CancellationToken cancellationToken = default);
    Task<MessaggioWhatsApp> InviaRispostaOperatoreAsync(Guid conversazioneId, string testo, Guid operatoreId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<ConversazioneWhatsAppDto>> GetConversazioniAttiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<MessaggioWhatsAppDto>> GetMessaggiAsync(Guid conversazioneId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AggiornaStatoMessaggioAsync(string whatsAppMessageId, StatoInvioMessaggio stato, Guid tenantId, CancellationToken cancellationToken = default);
}
