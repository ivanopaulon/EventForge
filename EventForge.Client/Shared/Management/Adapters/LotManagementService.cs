using EventForge.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Client.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="ILotService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// All lots are loaded client-side so that filtering, search and quick-filters
/// operate without additional server round-trips.
/// </summary>
public class LotManagementService : IEntityManagementService<LotDto>
{
    private readonly ILotService _lotService;

    public LotManagementService(ILotService lotService)
        => _lotService = lotService;

    public async Task<PagedResult<LotDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var result = await _lotService.GetLotsAsync(1, int.MaxValue);
        var list = result?.Items?.ToList() ?? new List<LotDto>();
        return new PagedResult<LotDto> { Items = list, TotalCount = list.Count, Page = 1, PageSize = list.Count };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _lotService.DeleteLotAsync(id);
}
