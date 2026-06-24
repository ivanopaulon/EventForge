using Prym.DTOs.AI;
using Prym.DTOs.Business;

namespace Prym.Web.Services.AI;

/// <summary>
/// Client-side service for the AI order assistant and simulation endpoints.
/// </summary>
public interface IAIOrderClientService
{
    Task<SimulateInboundResultDto?> SimulateInboundAsync(SimulateInboundDto request, CancellationToken cancellationToken = default);
    Task ResetSessionAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<OrderConversationSessionDto?> GetSessionAsync(Guid chatThreadId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of <see cref="IAIOrderClientService"/>.
/// </summary>
public class AIOrderClientService(
    IHttpClientService httpClientService,
    ILogger<AIOrderClientService> logger) : IAIOrderClientService
{
    public async Task<SimulateInboundResultDto?> SimulateInboundAsync(SimulateInboundDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<SimulateInboundDto, SimulateInboundResultDto>(
                "api/v1/whatsapp/simulate-inbound", request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SimulateInboundAsync failed");
            return null;
        }
    }

    public async Task ResetSessionAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var encoded = Uri.EscapeDataString(phoneNumber);
            await httpClientService.PostAsync<object>(
                $"api/v1/whatsapp/simulate-reset-session?phoneNumber={encoded}", new { }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ResetSessionAsync failed for {Phone}", phoneNumber);
        }
    }

    public async Task<OrderConversationSessionDto?> GetSessionAsync(Guid chatThreadId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<OrderConversationSessionDto>(
                $"api/v1/whatsapp/conversations/{chatThreadId}/ai-session", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetSessionAsync failed for thread {ThreadId}", chatThreadId);
            return null;
        }
    }
}
