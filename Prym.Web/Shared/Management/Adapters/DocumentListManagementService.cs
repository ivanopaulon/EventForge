using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using Prym.Web.Services;
using System.Linq;

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
    private readonly IDocumentStatusService _documentStatusService;
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

    public DocumentListManagementService(
        IDocumentHeaderService service,
        IDocumentStatusService documentStatusService)
    {
        _service = service;
        _documentStatusService = documentStatusService;
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

        _availableTransitionsByDocumentId.Clear();
        if (pagedResult.Items.Any())
        {
            var transitionsTasks = pagedResult.Items
                .Select(async item =>
                {
                    var transitions = await _documentStatusService.GetAvailableTransitionsAsync(item.Id, ct);
                    return new
                    {
                        item.Id,
                        Transitions = (IReadOnlyList<DocumentStatus>)(transitions ?? [])
                    };
                })
                .ToList();

            var transitionsByDocument = await Task.WhenAll(transitionsTasks);
            foreach (var transition in transitionsByDocument)
            {
                _availableTransitionsByDocumentId[transition.Id] = transition.Transitions;
            }
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
