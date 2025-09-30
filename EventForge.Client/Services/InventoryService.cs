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
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;
    private const string BaseUrl = "api/v1/warehouse/inventory";

    public InventoryService(HttpClient httpClient, ILogger<InventoryService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");

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
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, createDto);

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
