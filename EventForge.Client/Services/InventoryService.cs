using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of inventory management service using HTTP client.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<InventoryService> _logger;
    private const string BaseUrl = "api/v1/warehouse/inventory";

    public InventoryService(IHttpClientService httpClientService, ILogger<InventoryService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<InventoryEntryDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
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
            return await _httpClientService.PostAsync<CreateInventoryEntryDto, InventoryEntryDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory entry");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> StartInventoryDocumentAsync(CreateInventoryDocumentDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateInventoryDocumentDto, InventoryDocumentDto>($"{BaseUrl}/document/start", createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> UpdateInventoryDocumentAsync(Guid documentId, UpdateInventoryDocumentDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateInventoryDocumentDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto)
    {
        try
        {
            return await _httpClientService.PostAsync<AddInventoryDocumentRowDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row", rowDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> UpdateInventoryDocumentRowAsync(Guid documentId, Guid rowId, UpdateInventoryDocumentRowDto rowDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateInventoryDocumentRowDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row/{rowId}", rowDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> DeleteInventoryDocumentRowAsync(Guid documentId, Guid rowId)
    {
        try
        {
            // Delete returns the updated document in our case
            return await _httpClientService.DeleteAsync<InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row/{rowId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.PostAsync<object, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/finalize", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.GetAsync<InventoryDocumentDto>($"{BaseUrl}/document/{documentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory document");
            return null;
        }
    }

    public async Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (fromDate.HasValue)
            {
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("O"))}");
            }

            if (toDate.HasValue)
            {
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("O"))}");
            }

            var queryString = string.Join("&", queryParams);
            return await _httpClientService.GetAsync<PagedResult<InventoryDocumentDto>>($"{BaseUrl}/documents?{queryString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory documents");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> GetMostRecentOpenInventoryDocumentAsync()
    {
        try
        {
            // Query for Open documents, sorted by date descending, get only the first one
            var result = await GetInventoryDocumentsAsync(page: 1, pageSize: 1, status: "Open");
            return result?.Items?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most recent open inventory document");
            return null;
        }
    }
}
