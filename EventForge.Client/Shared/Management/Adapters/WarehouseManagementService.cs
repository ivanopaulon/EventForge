using EventForge.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Client.Shared.Management.Adapters;

public class WarehouseManagementService : IEntityManagementService<StorageFacilityDto>
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseManagementService(IWarehouseService warehouseService)
        => _warehouseService = warehouseService;

    public async Task<PagedResult<StorageFacilityDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var result = await _warehouseService.GetStorageFacilitiesAsync(page, pageSize);
        return result ?? new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _warehouseService.DeleteStorageFacilityAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete storage facility {id}");
    }
}
