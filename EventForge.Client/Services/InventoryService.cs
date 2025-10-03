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

    public async Task<InventoryDocumentDto?> StartInventoryDocumentAsync(CreateInventoryDocumentDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/document/start", createDto);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryDocumentDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to start inventory document. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/document/{documentId}/row", rowDto);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryDocumentDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to add inventory document row. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/document/{documentId}/finalize", null);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryDocumentDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to finalize inventory document. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/document/{documentId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryDocumentDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to get inventory document. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory document");
            return null;
        }
    }
}
