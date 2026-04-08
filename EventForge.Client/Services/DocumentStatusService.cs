using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of document status service using HTTP client.
/// </summary>
public class DocumentStatusService(
    IHttpClientService httpClientService,
    ILogger<DocumentStatusService> logger) : IDocumentStatusService
{
    private const string BaseUrl = "api/v1/documents";

    public async Task<DocumentHeaderDto?> ChangeStatusAsync(Guid documentId, DocumentStatus newStatus, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var dto = new ChangeDocumentStatusDto
            {
                NewStatus = newStatus,
                Reason = reason
            };

            return await httpClientService.PutAsync<ChangeDocumentStatusDto, DocumentHeaderDto>(
                $"{BaseUrl}/{documentId}/status",
                dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing status for document {DocumentId} to {NewStatus}", documentId, newStatus);
            return null;
        }
    }

    public async Task<List<DocumentStatusHistoryDto>?> GetStatusHistoryAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<DocumentStatusHistoryDto>>(
                $"{BaseUrl}/{documentId}/status/history");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving status history for document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<List<DocumentStatus>?> GetAvailableTransitionsAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<DocumentStatus>>(
                $"{BaseUrl}/{documentId}/status/available-transitions");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available transitions for document {DocumentId}", documentId);
            return null;
        }
    }
}
