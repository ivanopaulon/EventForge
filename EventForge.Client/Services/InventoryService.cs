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

    public async Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, bool includeRows = false)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
                $"includeRows={includeRows.ToString().ToLower()}"
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
            // Don't include rows for performance - caller can load them separately if needed
            var result = await GetInventoryDocumentsAsync(page: 1, pageSize: 1, status: "Open", includeRows: false);
            return result?.Items?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most recent open inventory document");
            return null;
        }
    }

    public async Task<InventoryValidationResultDto?> ValidateInventoryDocumentAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.PostAsync<object, InventoryValidationResultDto>($"{BaseUrl}/documents/{documentId}/validate", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating inventory document");
            throw;
        }
    }

    public async Task<List<InventoryDocumentDto>?> GetOpenInventoryDocumentsAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<InventoryDocumentDto>>($"{BaseUrl}/documents/open");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open inventory documents");
            return null;
        }
    }

    public async Task<PagedResult<InventoryDocumentRowDto>?> GetInventoryDocumentRowsAsync(Guid documentId, int page = 1, int pageSize = 50)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<InventoryDocumentRowDto>>($"{BaseUrl}/documents/{documentId}/rows?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory document rows");
            return null;
        }
    }
    public async Task<bool> CancelInventoryDocumentAsync(Guid documentId)
    {
        try
        {
            await _httpClientService.PostAsync<object, object>($"{BaseUrl}/documents/{documentId}/cancel", new { });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling inventory document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<List<InventoryDocumentDto>?> FinalizeAllOpenInventoriesAsync()
    {
        try
        {
            return await _httpClientService.PostAsync<object, List<InventoryDocumentDto>>($"{BaseUrl}/documents/finalize-all", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing all open inventories");
            return null;
        }
    }

    public async Task<int> CancelAllOpenInventoriesAsync()
    {
        try
        {
            var result = await _httpClientService.PostAsync<object, int>($"{BaseUrl}/documents/cancel-all", new { });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling all open inventories");
            return 0;
        }
    }
}
