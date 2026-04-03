using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;

namespace EventForge.Client.Shared.Management.Adapters;

public class UnitOfMeasureManagementService : IEntityManagementService<UMDto>
{
    private readonly IUMService _umService;

    public UnitOfMeasureManagementService(IUMService umService)
        => _umService = umService;

    public async Task<PagedResult<UMDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _umService.GetUMsAsync(page, pageSize, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _umService.DeleteUMAsync(id, ct);
        if (!success)
            throw new InvalidOperationException($"Failed to delete unit of measure {id}");
    }
}
