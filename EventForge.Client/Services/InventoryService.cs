using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of inventory management service using HTTP client.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InventoryService> _logger;
    private const string BaseUrl = "api/v1/warehouse/inventory";

    public InventoryService(IHttpClientFactory httpClientFactory, ILogger<InventoryService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResult<InventoryEntryDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to retrieve inventory entries. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory entries");
            return null;
        }
    }

    public async Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryEntryDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create inventory entry. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory entry");
            return null;
        }
    }
}
