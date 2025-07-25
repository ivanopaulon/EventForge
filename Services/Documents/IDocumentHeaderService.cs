using EventForge.DTOs.Audit;
using EventForge.DTOs.Documents;

namespace EventForge.Services.Documents;

/// <summary>
/// Service interface for managing document headers.
/// </summary>
public interface IDocumentHeaderService
{
    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated document headers</returns>
    Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document header by ID.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id, 
        bool includeRows = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document headers by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document headers</returns>
    Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document header DTO</returns>
    Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="updateDto">Document header update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id, 
        UpdateDocumentHeaderDto updateDto, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document header (soft delete).
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentHeaderAsync(
        Guid id, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates document totals (net, VAT, gross) for a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header with updated totals or null if not found</returns>
    Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document header exists.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> DocumentHeaderExistsAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
}