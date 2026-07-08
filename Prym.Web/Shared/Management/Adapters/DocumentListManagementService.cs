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
    // Component-scoped service state; accessed on Blazor UI flow (not thread-safe by design).
    private readonly Dictionary<Guid, IReadOnlyList<DocumentStatus>> _availableTransitionsByDocumentId = new();

    /// <summary>
    /// Allowed document status transitions, mirroring the server-side
    /// <c>EventForge.Server.Services.Documents.DocumentStateMachine.AllowedTransitions</c>.
    /// Computed locally from the Status already present in <see cref="DocumentHeaderDto"/> — no HTTP call needed.
    /// <para>
    /// ⚠️ Keep in sync with the server-side <c>DocumentStateMachine</c> whenever the state model changes.
    /// </para>
    /// </summary>
    private static readonly Dictionary<DocumentStatus, IReadOnlyList<DocumentStatus>> Transitions = new()
    {
        { DocumentStatus.Active,   [DocumentStatus.Archived] },
        { DocumentStatus.Archived, [DocumentStatus.Active]   },
    };

    /// <summary>
    /// Optional filter by document type ID. Set by the hosting page before triggering a refresh.
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Optional filter by document status. Set by the hosting page before triggering a refresh.
    /// </summary>
    public DocumentStatus? Status { get; set; }

    public DocumentListManagementService(IDocumentHeaderService service)
    {
        _service = service;
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

        // Compute available transitions locally from the Status already in each DocumentHeaderDto.
        // The state machine is purely status-driven; no extra server round-trip is needed.
        _availableTransitionsByDocumentId.Clear();
        foreach (var item in pagedResult.Items)
        {
            _availableTransitionsByDocumentId[item.Id] =
                Transitions.TryGetValue(item.Status, out var t) ? t : [];
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
