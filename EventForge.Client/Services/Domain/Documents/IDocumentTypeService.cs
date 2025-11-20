using EventForge.DTOs.Documents;

namespace EventForge.Client.Services.Domain.Documents;

/// <summary>
/// Service interface for document type operations.
/// </summary>
public interface IDocumentTypeService
{
    /// <summary>
    /// Gets all document types.
    /// </summary>
    /// <returns>Collection of document types</returns>
    Task<IEnumerable<DocumentTypeDto>?> GetAllDocumentTypesAsync();

    /// <summary>
    /// Gets a document type by ID.
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <returns>Document type details or null if not found</returns>
    Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id);

    /// <summary>
    /// Creates a new document type.
    /// </summary>
    /// <param name="createDto">Document type creation data</param>
    /// <returns>Created document type</returns>
    Task<DocumentTypeDto?> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto);

    /// <summary>
    /// Updates an existing document type.
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="updateDto">Document type update data</param>
    /// <returns>Updated document type or null if not found</returns>
    Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto);

    /// <summary>
    /// Deletes a document type.
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentTypeAsync(Guid id);
}
