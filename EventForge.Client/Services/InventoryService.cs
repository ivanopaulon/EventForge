using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of inventory management service using HTTP client.
/// </summary>
public class InventoryService(
    IHttpClientService httpClientService,
    ILogger<InventoryService> logger) : IInventoryService
{
    private const string BaseUrl = "api/v1/warehouse/inventory";

    public async Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<InventoryEntryDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inventory entries");
            return null;
        }
    }

    public async Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateInventoryEntryDto, InventoryEntryDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating inventory entry");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> StartInventoryDocumentAsync(CreateInventoryDocumentDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateInventoryDocumentDto, InventoryDocumentDto>($"{BaseUrl}/document/start", createDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> UpdateInventoryDocumentAsync(Guid documentId, UpdateInventoryDocumentDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateInventoryDocumentDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}", updateDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<AddInventoryDocumentRowDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row", rowDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> UpdateInventoryDocumentRowAsync(Guid documentId, Guid rowId, UpdateInventoryDocumentRowDto rowDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateInventoryDocumentRowDto, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row/{rowId}", rowDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> DeleteInventoryDocumentRowAsync(Guid documentId, Guid rowId, CancellationToken ct = default)
    {
        try
        {
            // Delete returns the updated document in our case
            return await httpClientService.DeleteAsync<InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/row/{rowId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting inventory document row");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, InventoryDocumentDto>($"{BaseUrl}/document/{documentId}/finalize", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finalizing inventory document");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<InventoryDocumentDto>($"{BaseUrl}/document/{documentId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting inventory document");
            return null;
        }
    }

    public async Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, bool includeRows = false, CancellationToken ct = default)
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
            return await httpClientService.GetAsync<PagedResult<InventoryDocumentDto>>($"{BaseUrl}/documents?{queryString}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inventory documents");
            return null;
        }
    }

    public async Task<InventoryDocumentDto?> GetMostRecentOpenInventoryDocumentAsync(CancellationToken ct = default)
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
            logger.LogError(ex, "Error getting most recent open inventory document");
            return null;
        }
    }

    public async Task<InventoryValidationResultDto?> ValidateInventoryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, InventoryValidationResultDto>($"{BaseUrl}/documents/{documentId}/validate", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating inventory document");
            throw;
        }
    }

    public async Task<List<InventoryDocumentDto>?> GetOpenInventoryDocumentsAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<InventoryDocumentDto>>($"{BaseUrl}/documents/open");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving open inventory documents");
            return null;
        }
    }

    public async Task<List<InventoryDocumentHeaderDto>?> GetOpenInventoryDocumentHeadersAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<InventoryDocumentHeaderDto>>($"{BaseUrl}/documents/open-headers");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving open inventory document headers");
            return null;
        }
    }

    public async Task<PagedResult<InventoryDocumentRowDto>?> GetInventoryDocumentRowsAsync(Guid documentId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<InventoryDocumentRowDto>>($"{BaseUrl}/documents/{documentId}/rows?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inventory document rows");
            return null;
        }
    }
    public async Task<bool> CancelInventoryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync<object, object>($"{BaseUrl}/documents/{documentId}/cancel", new { });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling inventory document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<List<InventoryDocumentDto>?> FinalizeAllOpenInventoriesAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, List<InventoryDocumentDto>>($"{BaseUrl}/documents/finalize-all", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finalizing all open inventories");
            return null;
        }
    }

    public async Task<int> CancelAllOpenInventoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<object, int>($"{BaseUrl}/documents/cancel-all", new { });
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling all open inventories");
            return 0;
        }
    }

    public async Task<InventoryDocumentDto?> MergeInventoryDocumentsAsync(List<Guid> sourceDocumentIds, string? notes = null, CancellationToken ct = default)
    {
        try
        {
            var request = new { SourceDocumentIds = sourceDocumentIds, Notes = notes };
            return await httpClientService.PostAsync<object, InventoryDocumentDto>($"{BaseUrl}/documents/merge", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error merging inventory documents");
            return null;
        }
    }

    public async Task<MergeInventoryDocumentsPreviewDto?> PreviewMergeInventoryDocumentsAsync(List<Guid> documentIds, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<List<Guid>, MergeInventoryDocumentsPreviewDto>(
                $"{BaseUrl}/documents/merge-preview", documentIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating merge preview for inventory documents");
            return null;
        }
    }

    public async Task<MergeInventoryDocumentsResultDto?> MergeInventoryDocumentsExtendedAsync(MergeInventoryDocumentsDto mergeDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<MergeInventoryDocumentsDto, MergeInventoryDocumentsResultDto>(
                $"{BaseUrl}/documents/merge", mergeDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error merging inventory documents");
            return null;
        }
    }

    public async Task<InventoryDiagnosticReportDto?> DiagnoseInventoryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, InventoryDiagnosticReportDto>($"{BaseUrl}/documents/{documentId}/diagnose", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error diagnosing inventory document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<InventoryRepairResultDto?> AutoRepairInventoryDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<InventoryAutoRepairOptionsDto, InventoryRepairResultDto>($"{BaseUrl}/documents/{documentId}/auto-repair", options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-repairing inventory document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<bool> RepairInventoryRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PatchAsync<InventoryRowRepairDto, object>($"{BaseUrl}/documents/{documentId}/rows/{rowId}/repair", repairData);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error repairing row {RowId} in inventory document {DocumentId}", rowId, documentId);
            return false;
        }
    }

    public async Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<List<Guid>, Dictionary<string, int>>($"{BaseUrl}/documents/{documentId}/remove-problematic-rows", rowIds);
            return result?.GetValueOrDefault("removedCount", 0) ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing problematic rows from inventory document {DocumentId}", documentId);
            return 0;
        }
    }
}
