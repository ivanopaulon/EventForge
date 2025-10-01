using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using System.Text;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of warehouse management service using HTTP client.
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WarehouseService> _logger;
    private const string BaseUrl = "api/v1/warehouse/facilities";

    public WarehouseService(IHttpClientFactory httpClientFactory, ILogger<WarehouseService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageFacilityDto>?> GetStorageFacilitiesAsync(int page = 1, int pageSize = 100)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResult<StorageFacilityDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to retrieve storage facilities. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facilities");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> GetStorageFacilityAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StorageFacilityDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to retrieve storage facility {FacilityId}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(BaseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StorageFacilityDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create storage facility. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storage facility");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto dto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"{BaseUrl}/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StorageFacilityDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to update storage facility {FacilityId}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteStorageFacilityAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogError("Failed to delete storage facility {FacilityId}. Status: {StatusCode}", id, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage facility {FacilityId}", id);
            return false;
        }
    }
}
