using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class FidelityPointsBaseRateManagementService(IFidelityPointsBaseRateService baseRateService)
    : IEntityManagementService<FidelityPointsBaseRateDto>
{
    public async Task<PagedResult<FidelityPointsBaseRateDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await baseRateService.GetAllAsync(ct))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FidelityPointsBaseRateDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await baseRateService.DeleteAsync(id, ct);
}
