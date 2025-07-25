using EventForge.Server.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document rows.
/// </summary>
public interface IDocumentRowService
{
    /// <summary>
    /// Gets all rows for a document header.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document rows</returns>
    Task<IEnumerable<DocumentRowDto>> GetRowsByDocumentHeaderIdAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document row by ID.
    /// </summary>
    /// <param name="id">Document row ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document row DTO or null if not found</returns>
    Task<DocumentRowDto?> GetDocumentRowByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document row.
    /// </summary>
    /// <param name="createDto">Document row creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document row DTO</returns>
    Task<DocumentRowDto> CreateDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    /// <param name="id">Document row ID</param>
    /// <param name="updateDto">Document row update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document row DTO or null if not found</returns>
    Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid id,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document row (soft delete).
    /// </summary>
    /// <param name="id">Document row ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentRowAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk creates multiple document rows for a document header.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="createDtos">Collection of document row creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of created document rows</returns>
    Task<IEnumerable<DocumentRowDto>> BulkCreateDocumentRowsAsync(
        Guid documentHeaderId,
        IEnumerable<CreateDocumentRowDto> createDtos,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders document rows within a document header.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="rowOrderUpdates">Dictionary of row ID to new sort order</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if reordering was successful</returns>
    Task<bool> ReorderDocumentRowsAsync(
        Guid documentHeaderId,
        Dictionary<Guid, int> rowOrderUpdates,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document row exists.
    /// </summary>
    /// <param name="id">Document row ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> DocumentRowExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}