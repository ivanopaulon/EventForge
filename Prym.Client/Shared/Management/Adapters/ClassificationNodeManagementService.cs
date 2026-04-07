using Prym.Client.Services;
using Prym.DTOs.Common;

namespace Prym.Client.Shared.Management.Adapters;

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

    public async Task<PagedResult<ClassificationNodeDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var roots = (await _entityManagementService.GetRootClassificationNodesAsync(ct)).ToList();
        return new PagedResult<ClassificationNodeDto>
        {
            Items = roots,
            Page = 1,
            PageSize = roots.Count,
            TotalCount = roots.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _entityManagementService.DeleteClassificationNodeAsync(id, ct);
}
