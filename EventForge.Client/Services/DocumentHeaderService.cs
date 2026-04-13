using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of document header service using HTTP client.
/// </summary>
public class DocumentHeaderService(
    IHttpClientService httpClientService,
    ILogger<DocumentHeaderService> logger) : IDocumentHeaderService
{
    private const string BaseUrl = "api/v1/documentheaders";

    public async Task<PagedResult<DocumentHeaderDto>?> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken ct = default)
    {
        try
        {
            var queryString = BuildQueryString(queryParameters);
            return await httpClientService.GetAsync<PagedResult<DocumentHeaderDto>>($"{BaseUrl}?{queryString}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paginated document headers");
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<DocumentHeaderDto>($"{BaseUrl}/{id}?includeRows={includeRows}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document header with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateDocumentHeaderDto, DocumentHeaderDto>(BaseUrl, createDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document header");
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(Guid id, UpdateDocumentHeaderDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateDocumentHeaderDto, DocumentHeaderDto>($"{BaseUrl}/{id}", updateDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document header with ID {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentHeaderAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document header with ID {Id}", id);
            return false;
        }
    }

    public async Task<DocumentHeaderDto?> ApproveDocumentAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, DocumentHeaderDto>($"{BaseUrl}/{id}/approve", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving document with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, DocumentHeaderDto>($"{BaseUrl}/{id}/close", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing document with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentRowDto?> AddDocumentRowAsync(CreateDocumentRowDto createRowDto, CancellationToken ct = default)
    {
        try
        {
            // The AddDocumentRow is exposed via DocumentHeaderService on the server
            // We need to check the actual endpoint - it might be in a different controller
            return await httpClientService.PostAsync<CreateDocumentRowDto, DocumentRowDto>("api/v1/documents/rows", createRowDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding document row");
            return null;
        }
    }

    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(Guid rowId, UpdateDocumentRowDto updateRowDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateDocumentRowDto, DocumentRowDto>($"api/v1/documents/rows/{rowId}", updateRowDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document row with ID {RowId}", rowId);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentRowAsync(Guid rowId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"api/v1/documents/rows/{rowId}", ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document row with ID {RowId}", rowId);
            return false;
        }
    }

    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object?, DocumentHeaderDto>($"{BaseUrl}/{documentId}/calculate-totals", null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating document totals for {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<Prym.DTOs.Bulk.BulkApprovalResultDto?> BulkApproveAsync(Prym.DTOs.Bulk.BulkApprovalDto bulkApprovalDto, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Starting bulk approval for {Count} documents", bulkApprovalDto.DocumentIds.Count);
            var result = await httpClientService.PostAsync<Prym.DTOs.Bulk.BulkApprovalDto, Prym.DTOs.Bulk.BulkApprovalResultDto>(
                "api/v1/documents/bulk-approve",
                bulkApprovalDto,
                ct);

            if (result is not null)
            {
                logger.LogInformation("Bulk approval completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing bulk approval");
            return null;
        }
    }

    public async Task<Prym.DTOs.Bulk.BulkStatusChangeResultDto?> BulkStatusChangeAsync(Prym.DTOs.Bulk.BulkStatusChangeDto bulkStatusChangeDto, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Starting bulk status change for {Count} documents to status '{Status}'",
                bulkStatusChangeDto.DocumentIds.Count, bulkStatusChangeDto.NewStatus);
            var result = await httpClientService.PostAsync<Prym.DTOs.Bulk.BulkStatusChangeDto, Prym.DTOs.Bulk.BulkStatusChangeResultDto>(
                "api/v1/documents/bulk-status-change",
                bulkStatusChangeDto,
                ct);

            if (result is not null)
            {
                logger.LogInformation("Bulk status change completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing bulk status change");
            return null;
        }
    }

    private static string BuildQueryString(DocumentHeaderQueryParameters parameters)
    {
        List<string> queryParams =
        [
            $"page={parameters.Page}",
            $"pageSize={parameters.PageSize}"
        ];

        if (parameters.DocumentTypeId.HasValue)
            queryParams.Add($"documentTypeId={parameters.DocumentTypeId.Value}");

        if (!string.IsNullOrEmpty(parameters.Number))
            queryParams.Add($"number={Uri.EscapeDataString(parameters.Number)}");

        if (!string.IsNullOrEmpty(parameters.Series))
            queryParams.Add($"series={Uri.EscapeDataString(parameters.Series)}");

        if (parameters.FromDate.HasValue)
            queryParams.Add($"fromDate={parameters.FromDate.Value:yyyy-MM-dd}");

        if (parameters.ToDate.HasValue)
            queryParams.Add($"toDate={parameters.ToDate.Value:yyyy-MM-dd}");

        if (parameters.BusinessPartyId.HasValue)
            queryParams.Add($"businessPartyId={parameters.BusinessPartyId.Value}");

        if (!string.IsNullOrEmpty(parameters.CustomerName))
            queryParams.Add($"customerName={Uri.EscapeDataString(parameters.CustomerName)}");

        if (parameters.Status.HasValue)
            queryParams.Add($"status={parameters.Status.Value}");

        if (parameters.PaymentStatus.HasValue)
            queryParams.Add($"paymentStatus={parameters.PaymentStatus.Value}");

        if (parameters.ApprovalStatus.HasValue)
            queryParams.Add($"approvalStatus={parameters.ApprovalStatus.Value}");

        if (parameters.TeamId.HasValue)
            queryParams.Add($"teamId={parameters.TeamId.Value}");

        if (parameters.EventId.HasValue)
            queryParams.Add($"eventId={parameters.EventId.Value}");

        if (parameters.SourceWarehouseId.HasValue)
            queryParams.Add($"sourceWarehouseId={parameters.SourceWarehouseId.Value}");

        if (parameters.DestinationWarehouseId.HasValue)
            queryParams.Add($"destinationWarehouseId={parameters.DestinationWarehouseId.Value}");

        if (parameters.IsFiscal.HasValue)
            queryParams.Add($"isFiscal={parameters.IsFiscal.Value}");

        if (parameters.IsProforma.HasValue)
            queryParams.Add($"isProforma={parameters.IsProforma.Value}");

        if (parameters.ProductId.HasValue)
            queryParams.Add($"productId={parameters.ProductId.Value}");

        if (!string.IsNullOrEmpty(parameters.SortBy))
            queryParams.Add($"sortBy={Uri.EscapeDataString(parameters.SortBy)}");

        if (!string.IsNullOrEmpty(parameters.SortDirection))
            queryParams.Add($"sortDirection={Uri.EscapeDataString(parameters.SortDirection)}");

        return string.Join("&", queryParams);
    }
}
