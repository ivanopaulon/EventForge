using EventForge.Client.Services;
using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management.Adapters;

/// <summary>
/// Management adapter that exposes only root classification nodes (ParentId == null).
/// Client-side filtering and pagination are handled by EntityManagementPage; all root
/// nodes are loaded in a single call and wrapped in a PagedResult.
/// </summary>
public class ClassificationNodeManagementService : IEntityManagementService<ClassificationNodeDto>
{
    private readonly IEntityManagementService _entityManagementService;

    public ClassificationNodeManagementService(IEntityManagementService entityManagementService)
        => _entityManagementService = entityManagementService;

    /// <inheritdoc />
    /// <remarks>
    /// The root-nodes API returns a flat list without server-side paging.
    /// All items are returned so that EntityManagementPage can apply client-side
    /// filtering and pagination (UseServerSidePaging defaults to false).
    /// The <paramref name="ct"/> parameter cannot be forwarded because
    /// <see cref="IEntityManagementService.GetRootClassificationNodesAsync"/> does not
    /// accept a CancellationToken.
    /// </remarks>
    public async Task<PagedResult<ClassificationNodeDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var roots = (await _entityManagementService.GetRootClassificationNodesAsync()).ToList();
        return new PagedResult<ClassificationNodeDto>
        {
            Items = roots,
            Page = 1,
            PageSize = roots.Count,
            TotalCount = roots.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _entityManagementService.DeleteClassificationNodeAsync(id);
}
