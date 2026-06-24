using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class FidelityCardManagementService(IFidelityService fidelityService)
    : IEntityManagementService<FidelityCardDto>
{
    public async Task<PagedResult<FidelityCardDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await fidelityService.GetAllCardsAsync(ct)).ToList();

        // Apply status filter
        if (filters != null && filters.TryGetValue("Status", out var rawStatus) && rawStatus is int statusInt)
        {
            var status = (FidelityCardStatus)statusInt;
            all = all.Where(c => c.Status == status).ToList();
        }

        // Apply type filter
        if (filters != null && filters.TryGetValue("Type", out var rawType) && rawType is int typeInt)
        {
            var type = (FidelityCardType)typeInt;
            all = all.Where(c => c.Type == type).ToList();
        }

        // Apply text search
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(c =>
                c.CardNumber.ToUpperInvariant().Contains(term) ||
                (c.Notes != null && c.Notes.ToUpperInvariant().Contains(term))).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FidelityCardDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await fidelityService.DeleteCardAsync(id, ct);
}
