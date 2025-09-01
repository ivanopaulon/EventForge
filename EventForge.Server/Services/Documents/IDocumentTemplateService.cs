using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document templates
/// </summary>
public interface IDocumentTemplateService
{
    /// <summary>
    /// Gets all document templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document template DTOs</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document template by ID
    /// </summary>
    /// <param name="id">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template DTO or null if not found</returns>
    Task<DocumentTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document templates by document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document template DTOs for the specified document type</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets public document templates available to all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of public document template DTOs</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document templates owned by or available to a specific user
    /// </summary>
    /// <param name="owner">Owner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document template DTOs</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document template
    /// </summary>
    /// <param name="createDto">Document template creation data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document template DTO</returns>
    Task<DocumentTemplateDto> CreateAsync(CreateDocumentTemplateDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document template
    /// </summary>
    /// <param name="id">Document template ID</param>
    /// <param name="updateDto">Document template update data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document template DTO or null if not found</returns>
    Task<DocumentTemplateDto?> UpdateAsync(Guid id, UpdateDocumentTemplateDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document template (soft delete)
    /// </summary>
    /// <param name="id">Document template ID</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last used date and usage count for a template
    /// </summary>
    /// <param name="id">Document template ID</param>
    /// <param name="currentUser">User who used the template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateUsageAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document templates by category
    /// </summary>
    /// <param name="category">Template category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document template DTOs for the specified category</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}