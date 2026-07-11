using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class FidelityTierManagementService(IFidelityTierService tierService)
    : IEntityManagementService<FidelityTierDto>
{
    public async Task<PagedResult<FidelityTierDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await tierService.GetAllAsync(ct))
            .OrderBy(x => x.SortOrder)
            .ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(t => t.Name.ToUpperInvariant().Contains(term)).ToList();
        }

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FidelityTierDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await tierService.DeleteAsync(id, ct);
}
