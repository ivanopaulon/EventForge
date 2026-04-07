using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document counters and generating document numbers.
/// </summary>
public interface IDocumentCounterService
{
    /// <summary>
    /// Gets all document counters.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document counter DTOs</returns>
    Task<IEnumerable<DocumentCounterDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all document counters for a specific document type.
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document counter DTOs</returns>
    Task<IEnumerable<DocumentCounterDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document counter by ID.
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document counter DTO or null if not found</returns>
    Task<DocumentCounterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document counter by document type, series, and year.
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="series">Series identifier</param>
    /// <param name="year">Year (null for non-year-specific counters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document counter DTO or null if not found</returns>
    Task<DocumentCounterDto?> GetByDocumentTypeSeriesYearAsync(
        Guid documentTypeId,
        string series,
        int? year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document counter.
    /// </summary>
    /// <param name="createDto">Document counter creation data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document counter DTO</returns>
    Task<DocumentCounterDto> CreateAsync(CreateDocumentCounterDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document counter.
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="updateDto">Document counter update data</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document counter DTO or null if not found</returns>
    Task<DocumentCounterDto?> UpdateAsync(Guid id, UpdateDocumentCounterDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document counter (soft delete).
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next document number for a document type and series.
    /// This method increments the counter and returns the formatted document number.
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="series">Series identifier (empty string for default)</param>
    /// <param name="currentUser">User performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated document number</returns>
    Task<string> GenerateDocumentNumberAsync(
        Guid documentTypeId,
        string series,
        string currentUser,
        CancellationToken cancellationToken = default);
}
