using EventForge.Server.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document summary links.
/// </summary>
public interface IDocumentSummaryLinkService
{
    /// <summary>
    /// Gets all summary links for a summary document.
    /// </summary>
    /// <param name="summaryDocumentId">Summary document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document summary links</returns>
    Task<IEnumerable<DocumentSummaryLinkDto>> GetLinksBySummaryDocumentIdAsync(
        Guid summaryDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all summary links where a document is included as a detailed document.
    /// </summary>
    /// <param name="detailedDocumentId">Detailed document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document summary links</returns>
    Task<IEnumerable<DocumentSummaryLinkDto>> GetLinksByDetailedDocumentIdAsync(
        Guid detailedDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document summary link by ID.
    /// </summary>
    /// <param name="id">Document summary link ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document summary link DTO or null if not found</returns>
    Task<DocumentSummaryLinkDto?> GetDocumentSummaryLinkByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document summary link.
    /// </summary>
    /// <param name="createDto">Document summary link creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document summary link DTO</returns>
    Task<DocumentSummaryLinkDto> CreateDocumentSummaryLinkAsync(
        CreateDocumentSummaryLinkDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document summary link.
    /// </summary>
    /// <param name="id">Document summary link ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentSummaryLinkAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk creates multiple document summary links.
    /// </summary>
    /// <param name="summaryDocumentId">Summary document ID</param>
    /// <param name="detailedDocumentIds">Collection of detailed document IDs</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of created document summary links</returns>
    Task<IEnumerable<DocumentSummaryLinkDto>> BulkCreateDocumentSummaryLinksAsync(
        Guid summaryDocumentId,
        IEnumerable<Guid> detailedDocumentIds,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document summary link exists.
    /// </summary>
    /// <param name="id">Document summary link ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> DocumentSummaryLinkExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a link between summary and detailed document already exists.
    /// </summary>
    /// <param name="summaryDocumentId">Summary document ID</param>
    /// <param name="detailedDocumentId">Detailed document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if link exists, false otherwise</returns>
    Task<bool> LinkExistsAsync(
        Guid summaryDocumentId,
        Guid? detailedDocumentId,
        CancellationToken cancellationToken = default);
}