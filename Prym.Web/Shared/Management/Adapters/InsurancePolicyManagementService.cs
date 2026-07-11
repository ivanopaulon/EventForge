using Prym.DTOs.Common;
using Prym.DTOs.Teams;
using Prym.Web.Services.Teams;

namespace Prym.Web.Shared.Management.Adapters;

public class InsurancePolicyManagementService(ITeamService teamService, Guid memberId)
    : IEntityManagementService<InsurancePolicyDto>
{
    public async Task<PagedResult<InsurancePolicyDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await teamService.GetInsurancePoliciesAsync(memberId, ct))?.ToList() ?? new List<InsurancePolicyDto>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(p =>
                p.Provider.ToUpperInvariant().Contains(term) ||
                p.PolicyNumber.ToUpperInvariant().Contains(term) ||
                (p.CoverageType ?? "").ToUpperInvariant().Contains(term)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<InsurancePolicyDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await teamService.DeleteInsurancePolicyAsync(id, ct);
}
