using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.Shared.Management.Adapters;

public class OperatorGroupManagementService : IEntityManagementService<StoreUserGroupDto>
{
    private readonly IStoreUserGroupService _storeUserGroupService;

    public OperatorGroupManagementService(IStoreUserGroupService storeUserGroupService)
        => _storeUserGroupService = storeUserGroupService;

    public async Task<PagedResult<StoreUserGroupDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _storeUserGroupService.GetPagedAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _storeUserGroupService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete operator group {id}");
    }
}
