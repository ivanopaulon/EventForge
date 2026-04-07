using EventForge.DTOs.DevTools;

namespace EventForge.Client.Services;

/// <summary>
/// Servizio per accedere agli strumenti di sviluppo (DevTools).
/// </summary>
public interface IDevToolsService
{
    /// <summary>
    /// Avvia la generazione di prodotti di test.
    /// </summary>
    /// <param name="request">Parametri di generazione</param>
    /// <returns>Risposta con ID del job avviato</returns>
    Task<GenerateProductsResponseDto?> GenerateProductsAsync(GenerateProductsRequestDto request);

    /// <summary>
    /// Ottiene lo stato di un job di generazione prodotti.
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <returns>Stato del job</returns>
    Task<GenerateProductsStatusDto?> GetGenerateProductsStatusAsync(string jobId);

    /// <summary>
    /// Cancella un job di generazione prodotti.
    /// </summary>
    /// <param name="jobId">ID del job da cancellare</param>
    /// <returns>True se cancellato con successo</returns>
    Task<bool> CancelGenerateProductsAsync(string jobId);
}

/// <summary>
/// Implementazione del servizio DevTools usando HTTP client.
/// </summary>
public class DevToolsService : IDevToolsService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<DevToolsService> _logger;
    private const string BaseUrl = "api/v1/devtools";

    public DevToolsService(IHttpClientService httpClientService, ILogger<DevToolsService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GenerateProductsResponseDto?> GenerateProductsAsync(GenerateProductsRequestDto request)
    {
        try
        {
            var response = await _httpClientService.PostAsync<GenerateProductsRequestDto, GenerateProductsResponseDto>(
                $"{BaseUrl}/generate-products",
                request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio della generazione prodotti");
            return null;
        }
    }

    public async Task<GenerateProductsStatusDto?> GetGenerateProductsStatusAsync(string jobId)
    {
        try
        {
            return await _httpClientService.GetAsync<GenerateProductsStatusDto>(
                $"{BaseUrl}/generate-products/status/{Uri.EscapeDataString(jobId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero dello stato del job {JobId}", jobId);
            return null;
        }
    }

    public async Task<bool> CancelGenerateProductsAsync(string jobId)
    {
        try
        {
            await _httpClientService.PostAsync(
                $"{BaseUrl}/generate-products/cancel/{Uri.EscapeDataString(jobId)}",
                new { }); // Empty object as payload

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella cancellazione del job {JobId}", jobId);
            return false;
        }
    }
}
