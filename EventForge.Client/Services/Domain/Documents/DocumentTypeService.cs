using EventForge.DTOs.Documents;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Domain.Documents;

/// <summary>
/// Implementation of document type service using HTTP client.
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<DocumentTypeService> _logger;
    private const string BaseUrl = "api/v1/documents/types";

    public DocumentTypeService(IHttpClientService httpClientService, ILogger<DocumentTypeService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DocumentTypeDto>?> GetAllDocumentTypesAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<DocumentTypeDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document types");
            return null;
        }
    }

    public async Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<DocumentTypeDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document type with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentTypeDto?> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateDocumentTypeDto, DocumentTypeDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document type");
            return null;
        }
    }

    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateDocumentTypeDto, DocumentTypeDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document type with ID {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentTypeAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document type with ID {Id}", id);
            return false;
        }
    }
}
