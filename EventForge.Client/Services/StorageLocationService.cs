using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of storage location management service using HTTP client.
/// </summary>
public class StorageLocationService : IStorageLocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StorageLocationService> _logger;
    private const string BaseUrl = "api/v1/warehouse/locations";

    public StorageLocationService(HttpClient httpClient, ILogger<StorageLocationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResult<StorageLocationDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to retrieve storage locations. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage locations");
            return null;
        }
    }
}
