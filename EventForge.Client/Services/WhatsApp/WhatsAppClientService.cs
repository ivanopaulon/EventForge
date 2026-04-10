using EventForge.DTOs.Business;
using EventForge.DTOs.Chat;
using EventForge.DTOs.External.WhatsApp;

namespace EventForge.Client.Services.WhatsApp;

/// <summary>
/// Client-side service for WhatsApp Business integration.
/// Calls the unified /api/v1/whatsapp/* endpoints.
/// </summary>
public class WhatsAppClientService(
    IHttpClientService httpClientService,
    ILogger<WhatsAppClientService> logger) : IWhatsAppClientService
{
    public async Task<List<ChatResponseDto>> GetConversazioniAttiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<ChatResponseDto>>("api/v1/whatsapp/conversations", cancellationToken)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active WhatsApp conversations");
            return [];
        }
    }

    public async Task<List<ChatMessageDto>> GetMessaggiAsync(Guid chatThreadId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<ChatMessageDto>>($"api/v1/whatsapp/conversations/{chatThreadId}/messages", cancellationToken)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting WhatsApp messages for thread {ThreadId}", chatThreadId);
            return [];
        }
    }

    public async Task AssegnaAdBusinessPartyAsync(AssegnaNumeroDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            await httpClientService.PostAsync("api/v1/whatsapp/assign", dto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning WhatsApp number to BusinessParty");
            throw;
        }
    }

    public async Task BloccaNumeroAsync(string numero, string? note, CancellationToken cancellationToken = default)
    {
        try
        {
            await httpClientService.PostAsync("api/v1/whatsapp/block", new { Numero = numero, Note = note }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error blocking WhatsApp number {Number}", numero);
            throw;
        }
    }

    public async Task<List<BusinessPartyDto>> SearchBusinessPartiesAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var encoded = Uri.EscapeDataString(query);
            return await httpClientService.GetAsync<List<BusinessPartyDto>>($"api/v1/businessparties/search?q={encoded}", cancellationToken)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching BusinessParties for WhatsApp assignment");
            return [];
        }
    }

    public async Task<WhatsAppConfigDto?> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<WhatsAppConfigDto>("api/v1/whatsapp/config", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading WhatsApp configuration");
            return null;
        }
    }

    public async Task SaveConfigAsync(WhatsAppConfigDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            await httpClientService.PutAsync("api/v1/whatsapp/config", dto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving WhatsApp configuration");
            throw;
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<object, TestResultDto>("api/v1/whatsapp/config/test", new { }, cancellationToken);
            if (result == null) return (false, "Nessuna risposta dal server.");
            return (result.Success, result.Message ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing WhatsApp connection");
            return (false, $"Errore: {ex.Message}");
        }
    }

    private class TestResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
