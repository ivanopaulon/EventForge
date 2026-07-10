using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class FidelityTierMultiplierManagementService(IFidelityTierMultiplierService tierMultiplierService)
    : IEntityManagementService<FidelityTierMultiplierDto>
{
    public async Task<PagedResult<FidelityTierMultiplierDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await tierMultiplierService.GetAllAsync(ct))
            .OrderBy(x => x.CardType)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FidelityTierMultiplierDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        };
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotSupportedException();
}
