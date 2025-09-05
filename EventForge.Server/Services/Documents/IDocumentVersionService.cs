using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document versions
/// </summary>
public interface IDocumentVersionService
{
    /// <summary>
    /// Gets all versions for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document versions</returns>
    Task<IEnumerable<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentHeaderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific document version by ID
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document version details</returns>
    Task<DocumentVersionDto?> GetDocumentVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document version
    /// </summary>
    /// <param name="createDto">Version creation data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document version</returns>
    Task<DocumentVersionDto> CreateDocumentVersionAsync(CreateDocumentVersionDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document version
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <param name="updateDto">Version update data</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document version</returns>
    Task<DocumentVersionDto?> UpdateDocumentVersionAsync(Guid versionId, UpdateDocumentVersionDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document version
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentVersionAsync(Guid versionId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a version as the current version for a document
    /// </summary>
    /// <param name="versionId">Version ID</param>
    /// <param name="currentUser">Current user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if set successfully</returns>
    Task<bool> SetCurrentVersionAsync(Guid versionId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version for a document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current document version</returns>
    Task<DocumentVersionDto?> GetCurrentVersionAsync(Guid documentHeaderId, CancellationToken cancellationToken = default);
}