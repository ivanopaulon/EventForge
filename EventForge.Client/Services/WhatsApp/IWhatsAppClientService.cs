using Prym.DTOs.Business;
using Prym.DTOs.Chat;
using Prym.DTOs.External.WhatsApp;

namespace EventForge.Client.Services.WhatsApp;

public interface IWhatsAppClientService
{
    // ─── Conversations / messages (unified DTOs) ─────────────────────────────
    Task<List<ChatResponseDto>> GetConversazioniAttiveAsync(CancellationToken cancellationToken = default);
    Task<List<ChatMessageDto>> GetMessaggiAsync(Guid chatThreadId, CancellationToken cancellationToken = default);

    // ─── BusinessParty association ───────────────────────────────────────────
    Task AssegnaAdBusinessPartyAsync(AssegnaNumeroDto dto, CancellationToken cancellationToken = default);
    Task BloccaNumeroAsync(string numero, string? note, CancellationToken cancellationToken = default);
    Task<List<BusinessPartyDto>> SearchBusinessPartiesAsync(string query, CancellationToken cancellationToken = default);

    // ─── Configuration ───────────────────────────────────────────────────────
    Task<WhatsAppConfigDto?> GetConfigAsync(CancellationToken cancellationToken = default);
    Task SaveConfigAsync(WhatsAppConfigDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default);
}
