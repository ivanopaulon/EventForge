using EventForge.DTOs.Business;
using EventForge.DTOs.External.WhatsApp;

namespace EventForge.Client.Services.WhatsApp;

public interface IWhatsAppClientService
{
    Task<List<ConversazioneWhatsAppDto>> GetConversazioniAttiveAsync(CancellationToken cancellationToken = default);
    Task<List<MessaggioWhatsAppDto>> GetMessaggiAsync(Guid conversazioneId, CancellationToken cancellationToken = default);
    Task AssegnaAdBusinessPartyAsync(AssegnaNumeroDto dto, CancellationToken cancellationToken = default);
    Task BloccaNumeroAsync(string numero, string? note, CancellationToken cancellationToken = default);
    Task<List<BusinessPartyDto>> SearchBusinessPartiesAsync(string query, CancellationToken cancellationToken = default);
}
