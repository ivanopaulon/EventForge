using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of document status service using HTTP client.
/// </summary>
public class DocumentStatusService : IDocumentStatusService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<DocumentStatusService> _logger;
    private const string BaseUrl = "api/v1/documents";

    public DocumentStatusService(IHttpClientService httpClientService, ILogger<DocumentStatusService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentHeaderDto?> ChangeStatusAsync(Guid documentId, DocumentStatus newStatus, string? reason = null)
    {
        try
        {
            var dto = new ChangeDocumentStatusDto
            {
                NewStatus = newStatus,
                Reason = reason
            };

            return await _httpClientService.PutAsync<ChangeDocumentStatusDto, DocumentHeaderDto>(
                $"{BaseUrl}/{documentId}/status",
                dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status for document {DocumentId} to {NewStatus}", documentId, newStatus);
            return null;
        }
    }

    public async Task<List<DocumentStatusHistoryDto>?> GetStatusHistoryAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.GetAsync<List<DocumentStatusHistoryDto>>(
                $"{BaseUrl}/{documentId}/status/history");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history for document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<List<DocumentStatus>?> GetAvailableTransitionsAsync(Guid documentId)
    {
        try
        {
            return await _httpClientService.GetAsync<List<DocumentStatus>>(
                $"{BaseUrl}/{documentId}/status/available-transitions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available transitions for document {DocumentId}", documentId);
            return null;
        }
    }
}
