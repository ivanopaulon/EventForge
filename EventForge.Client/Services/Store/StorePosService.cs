using EventForge.DTOs.Store;
using EventForge.DTOs.Common;
using System.Net.Http.Json;

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
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store POS");
            throw;
        }
    }

    public async Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store POS {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBase}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store POS {Id}", id);
            throw;
        }
    }
}
