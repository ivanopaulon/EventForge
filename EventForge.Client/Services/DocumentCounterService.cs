using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing document counters via API.
/// </summary>
public class DocumentCounterService(
    IHttpClientService httpClientService,
    ILogger<DocumentCounterService> logger) : IDocumentCounterService
{
    private const string BaseUrl = "api/v1/documentcounters";

    public async Task<IEnumerable<DocumentCounterDto>?> GetAllDocumentCountersAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<DocumentCounterDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all document counters");
            throw;
        }
    }

    public async Task<IEnumerable<DocumentCounterDto>?> GetDocumentCountersByTypeAsync(Guid documentTypeId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<DocumentCounterDto>>($"{BaseUrl}/by-type/{documentTypeId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting document counters for type {DocumentTypeId}", documentTypeId);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> GetDocumentCounterByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<DocumentCounterDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting document counter {Id}", id);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> CreateDocumentCounterAsync(CreateDocumentCounterDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateDocumentCounterDto, DocumentCounterDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document counter");
            throw;
        }
    }

    public async Task<DocumentCounterDto?> UpdateDocumentCounterAsync(Guid id, UpdateDocumentCounterDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateDocumentCounterDto, DocumentCounterDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document counter {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentCounterAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document counter {Id}", id);
            throw;
        }
    }
}
