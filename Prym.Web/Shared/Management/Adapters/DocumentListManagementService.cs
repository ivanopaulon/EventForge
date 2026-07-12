using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using Prym.Web.Services;

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
    private readonly IDocumentStatusService _statusService;
    // Component-scoped service state; accessed on Blazor UI flow (not thread-safe by design).
    private readonly Dictionary<Guid, IReadOnlyList<DocumentStatus>> _availableTransitionsByDocumentId = new();

    /// <summary>
    /// Optional filter by document type ID. Set by the hosting page before triggering a refresh.
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Optional filter by document status. Set by the hosting page before triggering a refresh.
    /// </summary>
    public DocumentStatus? Status { get; set; }

    public DocumentListManagementService(IDocumentHeaderService service, IDocumentStatusService statusService)
    {
        _service = service;
        _statusService = statusService;
    }

    public IReadOnlyList<DocumentStatus> GetAvailableTransitions(Guid documentId)
        => _availableTransitionsByDocumentId.TryGetValue(documentId, out var transitions)
            ? transitions
            : [];

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
        var pagedResult = result ?? new PagedResult<DocumentHeaderDto>
        {
            Items = Array.Empty<DocumentHeaderDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = queryParams.PageSize
        };

        // Available transitions are authoritative from the server-side state machine
        // (IDocumentStatusService.GetAvailableTransitionsAsync), avoiding a client-side
        // mirror of DocumentStateMachine that could drift out of sync.
        var transitionResults = await Task.WhenAll(pagedResult.Items.Select(async item =>
        {
            var transitions = await _statusService.GetAvailableTransitionsAsync(item.Id, ct);
            return (item.Id, transitions ?? (IReadOnlyList<DocumentStatus>)[]);
        }));

        _availableTransitionsByDocumentId.Clear();
        foreach (var (documentId, transitions) in transitionResults)
        {
            _availableTransitionsByDocumentId[documentId] = transitions;
        }

        return pagedResult;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _service.DeleteDocumentHeaderAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete document {id}");
    }
}
