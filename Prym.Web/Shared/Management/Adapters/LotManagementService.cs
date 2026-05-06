using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="ILotService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// </summary>
public class LotManagementService : IEntityManagementService<LotDto>
{
    private readonly ILotService _lotService;

    public LotManagementService(ILotService lotService)
        => _lotService = lotService;

    public async Task<PagedResult<LotDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var status = filters != null && filters.TryGetValue("status", out var statusValue) ? statusValue as string : null;
        var expiringSoon = filters != null && filters.TryGetValue("expiringSoon", out var expiringValue) ? expiringValue as bool? : null;
        var recent = filters != null && filters.TryGetValue("recent", out var recentValue) ? recentValue as bool? : null;
        var result = await _lotService.GetLotsAsync(page, pageSize, status: status, expiringSoon: expiringSoon, recent: recent, searchTerm: searchTerm, ct: ct);
        return result ?? new PagedResult<LotDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _lotService.DeleteLotAsync(id);
}
