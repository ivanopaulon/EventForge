using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Adapter scoped to a single campaign — tier multipliers are configured per campaign,
/// not tenant-wide, so every instance targets exactly one <see cref="CampaignId"/>.
/// </summary>
public class FidelityTierMultiplierManagementService(IFidelityTierMultiplierService tierMultiplierService, Guid campaignId)
    : IEntityManagementService<FidelityTierMultiplierDto>
{
    public Guid CampaignId => campaignId;

    public async Task<PagedResult<FidelityTierMultiplierDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await tierMultiplierService.GetByCampaignAsync(campaignId, ct))
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
        => tierMultiplierService.DeleteAsync(id, ct);
}
