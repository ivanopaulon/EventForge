using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of document type service using HTTP client.
/// </summary>
public class DocumentTypeService(
    IHttpClientService httpClientService,
    ILogger<DocumentTypeService> logger) : IDocumentTypeService
{
    private const string BaseUrl = "api/v1/documents/types";

    public async Task<IEnumerable<DocumentTypeDto>?> GetAllDocumentTypesAsync()
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<DocumentTypeDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document types");
            return null;
        }
    }

    public async Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id)
    {
        try
        {
            return await httpClientService.GetAsync<DocumentTypeDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document type with ID {Id}", id);
            return null;
        }
    }

    public async Task<DocumentTypeDto?> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto)
    {
        try
        {
            return await httpClientService.PostAsync<CreateDocumentTypeDto, DocumentTypeDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document type");
            return null;
        }
    }

    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateDocumentTypeDto, DocumentTypeDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document type with ID {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentTypeAsync(Guid id)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document type with ID {Id}", id);
            return false;
        }
    }
}
