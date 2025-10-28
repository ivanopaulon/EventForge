using EventForge.DTOs.Documents;
using System.Net.Http.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing document counters via API.
/// </summary>
public class DocumentCounterService : IDocumentCounterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentCounterService> _logger;
    private const string BaseUrl = "api/v1/DocumentCounters";

    public DocumentCounterService(HttpClient httpClient, ILogger<DocumentCounterService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DocumentCounterDto>?> GetAllDocumentCountersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BaseUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<DocumentCounterDto>>();
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
            var response = await _httpClient.GetAsync($"{BaseUrl}/by-type/{documentTypeId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<DocumentCounterDto>>();
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
            var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentCounterDto>();
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
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentCounterDto>();
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
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentCounterDto>();
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
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document counter {Id}", id);
            throw;
        }
    }
}
