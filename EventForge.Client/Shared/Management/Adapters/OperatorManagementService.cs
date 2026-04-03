using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.Shared.Management.Adapters;

public class OperatorManagementService : IEntityManagementService<StoreUserDto>
{
    private readonly IStoreUserService _storeUserService;

    public OperatorManagementService(IStoreUserService storeUserService)
        => _storeUserService = storeUserService;

    public async Task<PagedResult<StoreUserDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _storeUserService.GetPagedAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _storeUserService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete operator {id}");
    }
}
