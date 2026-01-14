using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of document header service using HTTP client.
/// </summary>
public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<DocumentHeaderService> _logger;
    private const string BaseUrl = "api/v1/DocumentHeaders";

    public DocumentHeaderService(IHttpClientService httpClientService, ILogger<DocumentHeaderService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<DocumentHeaderDto>?> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters)
    {
        try
        {
            var queryString = BuildQueryString(queryParameters);
            return await _httpClientService.GetAsync<PagedResult<DocumentHeaderDto>>($"{BaseUrl}?{queryString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated document headers");
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false)
    {
        try
        {
            return await _httpClientService.GetAsync<DocumentHeaderDto>($"{BaseUrl}/{id}?includeRows={includeRows}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document header with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateDocumentHeaderDto, DocumentHeaderDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document header");
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(Guid id, UpdateDocumentHeaderDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateDocumentHeaderDto, DocumentHeaderDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document header with ID {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentHeaderAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document header with ID {Id}", id);
            return false;
        }
    }

    public async Task<DocumentHeaderDto?> ApproveDocumentAsync(Guid id)
    {
        try
        {
            return await _httpClientService.PostAsync<object, DocumentHeaderDto>($"{BaseUrl}/{id}/approve", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving document with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id)
    {
        try
        {
            return await _httpClientService.PostAsync<object, DocumentHeaderDto>($"{BaseUrl}/{id}/close", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing document with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentRowDto?> AddDocumentRowAsync(CreateDocumentRowDto createRowDto)
    {
        try
        {
            // The AddDocumentRow is exposed via DocumentHeaderService on the server
            // We need to check the actual endpoint - it might be in a different controller
            return await _httpClientService.PostAsync<CreateDocumentRowDto, DocumentRowDto>("api/v1/documents/rows", createRowDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document row");
            return null;
        }
    }

    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(Guid rowId, UpdateDocumentRowDto updateRowDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateDocumentRowDto, DocumentRowDto>($"api/v1/documents/rows/{rowId}", updateRowDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document row with ID {RowId}", rowId);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentRowAsync(Guid rowId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/documents/rows/{rowId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document row with ID {RowId}", rowId);
            return false;
        }
    }

    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.PostAsync<object?, DocumentHeaderDto>($"{BaseUrl}/{documentId}/calculate-totals", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating document totals for {DocumentId}", documentId);
            return null;
        }
    }

    private static string BuildQueryString(DocumentHeaderQueryParameters parameters)
    {
        var queryParams = new List<string>
        {
            $"page={parameters.Page}",
            $"pageSize={parameters.PageSize}"
        };

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
