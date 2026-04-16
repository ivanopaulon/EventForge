using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace Prym.Web.Services;

/// <summary>
/// Service interface for document header operations.
/// </summary>
public interface IDocumentHeaderService
{
    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <returns>Paginated document headers</returns>
    Task<PagedResult<DocumentHeaderDto>?> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken ct = default);

    /// <summary>
    /// Gets a document header by ID.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <returns>Document header details or null if not found</returns>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken ct = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <returns>Created document header</returns>
    Task<DocumentHeaderDto?> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="updateDto">Document header update data</param>
    /// <returns>Updated document header or null if not found</returns>
    Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(Guid id, UpdateDocumentHeaderDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentHeaderAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <returns>Approved document header or null if not found</returns>
    Task<DocumentHeaderDto?> ApproveDocumentAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Closes a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <returns>Closed document header or null if not found</returns>
    Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a row to a document.
    /// </summary>
    /// <param name="createRowDto">Document row creation data</param>
    /// <returns>Created document row</returns>
    Task<DocumentRowDto?> AddDocumentRowAsync(CreateDocumentRowDto createRowDto, CancellationToken ct = default);

    /// <summary>
    /// Updates a document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="updateRowDto">Document row update data</param>
    /// <returns>Updated document row or null if not found</returns>
    Task<DocumentRowDto?> UpdateDocumentRowAsync(Guid rowId, UpdateDocumentRowDto updateRowDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDocumentRowAsync(Guid rowId, CancellationToken ct = default);

    /// <summary>
    /// Calculates document totals (net, VAT, gross) for a document header.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <returns>Document header with updated totals or null if not found</returns>
    Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Performs bulk approval of multiple documents.
    /// </summary>
    Task<Prym.DTOs.Bulk.BulkApprovalResultDto?> BulkApproveAsync(Prym.DTOs.Bulk.BulkApprovalDto bulkApprovalDto, CancellationToken ct = default);

    /// <summary>
    /// Performs bulk status change of multiple documents.
    /// </summary>
    Task<Prym.DTOs.Bulk.BulkStatusChangeResultDto?> BulkStatusChangeAsync(Prym.DTOs.Bulk.BulkStatusChangeDto bulkStatusChangeDto, CancellationToken ct = default);
}
