using EventForge.Client.Services;
using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management.Adapters;

public class ClassificationNodeManagementService : IEntityManagementService<ClassificationNodeDto>
{
    private readonly IEntityManagementService _entityManagementService;

    public ClassificationNodeManagementService(IEntityManagementService entityManagementService)
        => _entityManagementService = entityManagementService;

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
