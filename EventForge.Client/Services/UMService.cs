using System.Net.Http.Json;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService : IUMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UMService> _logger;
    private const string BaseUrl = "api/v1/product-management/units";

    public UMService(HttpClient httpClient, ILogger<UMService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PagedResult<UMDto>>();
            return result ?? new PagedResult<UMDto> { Items = new List<UMDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving units of measure");
            throw;
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
            }
            
            return await response.Content.ReadFromJsonAsync<UMDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unit of measure with ID {Id}", id);
            throw;
        }
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, createUMDto);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<UMDto>();
            return result ?? throw new InvalidOperationException("Failed to deserialize created unit of measure");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit of measure");
            throw;
        }
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateUMDto);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
            }
            
            return await response.Content.ReadFromJsonAsync<UMDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit of measure with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUMAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
            
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting unit of measure with ID {Id}", id);
            throw;
        }
    }
}
