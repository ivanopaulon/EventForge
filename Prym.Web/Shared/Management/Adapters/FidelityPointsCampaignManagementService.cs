using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class FidelityPointsCampaignManagementService(IFidelityPointsCampaignService campaignService)
    : IEntityManagementService<FidelityPointsCampaignDto>
{
    public async Task<PagedResult<FidelityPointsCampaignDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await campaignService.GetAllAsync(ct))
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FidelityPointsCampaignDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await campaignService.DeleteAsync(id, ct);
}
