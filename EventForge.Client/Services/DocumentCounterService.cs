using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing document counters via API.
/// </summary>
public class DocumentCounterService : IDocumentCounterService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<DocumentCounterService> _logger;
    private const string BaseUrl = "api/v1/DocumentCounters";

    public DocumentCounterService(IHttpClientService httpClientService, ILogger<DocumentCounterService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DocumentCounterDto>?> GetAllDocumentCountersAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<DocumentCounterDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all document counters");
            throw;
        }
    }

    public async Task<IEnumerable<DocumentCounterDto>?> GetDocumentCountersByTypeAsync(Guid documentTypeId)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<DocumentCounterDto>>($"{BaseUrl}/by-type/{documentTypeId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document counters for type {DocumentTypeId}", documentTypeId);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> GetDocumentCounterByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<DocumentCounterDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document counter {Id}", id);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> CreateDocumentCounterAsync(CreateDocumentCounterDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateDocumentCounterDto, DocumentCounterDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document counter");
            throw;
        }
    }

    public async Task<DocumentCounterDto?> UpdateDocumentCounterAsync(Guid id, UpdateDocumentCounterDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateDocumentCounterDto, DocumentCounterDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document counter {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentCounterAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document counter {Id}", id);
            throw;
        }
    }
}
