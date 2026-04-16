using Prym.Web.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="IDocumentHeaderService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// Supports optional server-side filtering by <see cref="DocumentTypeId"/> and <see cref="Status"/>,
/// which the hosting page can set before calling <c>RefreshAsync()</c> on the management page.
/// </summary>
public class DocumentListManagementService : IEntityManagementService<DocumentHeaderDto>
{
    private readonly IDocumentHeaderService _service;

    /// <summary>
    /// Optional filter by document type ID. Set by the hosting page before triggering a refresh.
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Optional filter by document status. Set by the hosting page before triggering a refresh.
    /// </summary>
    public DocumentStatus? Status { get; set; }

    public DocumentListManagementService(IDocumentHeaderService service)
        => _service = service;

    public async Task<PagedResult<DocumentHeaderDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var queryParams = new DocumentHeaderQueryParameters
        {
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, 100),
            DocumentTypeId = DocumentTypeId,
            Status = Status,
            SortBy = "Date",
            SortDirection = "desc",
            IncludeRows = false
        };

        var result = await _service.GetPagedDocumentHeadersAsync(queryParams);
        return result ?? new PagedResult<DocumentHeaderDto>
        {
            Items = Array.Empty<DocumentHeaderDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _service.DeleteDocumentHeaderAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete document {id}");
    }
}
