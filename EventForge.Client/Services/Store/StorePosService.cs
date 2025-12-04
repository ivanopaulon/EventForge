using EventForge.DTOs.Store;
using EventForge.DTOs.Common;
using System.Net.Http.Json;
using System.Net;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store POS terminals.
/// </summary>
public class StorePosService : IStorePosService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StorePosService> _logger;
    private const string ApiBase = "api/v1/storeusers/pos";

    public StorePosService(HttpClient httpClient, ILogger<StorePosService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    public async Task<List<StorePosDto>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page=1&pageSize=1000");
            response.EnsureSuccessStatusCode();
            
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StorePosDto>>();
            return pagedResult?.Items?.ToList() ?? new List<StorePosDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all store POS terminals");
            throw;
        }
    }

    public async Task<List<StorePosDto>> GetActiveAsync()
    {
        try
        {
            var allPos = await GetAllAsync();
            return allPos.Where(p => p.Status == CashRegisterStatus.Active).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active store POS terminals");
            throw;
        }
    }

    public async Task<StorePosDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StorePosDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store POS {Id}", id);
            throw;
        }
    }

    public async Task<StorePosDto?> CreateAsync(CreateStorePosDto createDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBase, createDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", _logger);
                _logger.LogError("Error creating store POS: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store POS");
            throw new InvalidOperationException("Errore nella creazione del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", _logger);
                _logger.LogError("Error updating store POS {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBase}/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", _logger);
                _logger.LogError("Error deleting store POS {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione del punto cassa.", ex);
        }
    }

    public async Task<PagedResult<StorePosDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", _logger);
                _logger.LogError("Error getting paged store POS terminals (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}", 
                    page, pageSize, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            return await response.Content.ReadFromJsonAsync<PagedResult<StorePosDto>>() 
                ?? new PagedResult<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged store POS terminals (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento dei punti cassa.", ex);
        }
    }
}
