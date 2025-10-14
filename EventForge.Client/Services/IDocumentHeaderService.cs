using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

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
    Task<PagedResult<DocumentHeaderDto>?> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters);
}
