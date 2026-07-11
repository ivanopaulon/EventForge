using Prym.DTOs.Common;
using Prym.DTOs.Teams;
using Prym.Web.Services.Teams;

namespace Prym.Web.Shared.Management.Adapters;

public class MembershipCardManagementService(ITeamService teamService, Guid memberId)
    : IEntityManagementService<MembershipCardDto>
{
    public async Task<PagedResult<MembershipCardDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await teamService.GetMembershipCardsAsync(memberId, ct))?.ToList() ?? new List<MembershipCardDto>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(c =>
                c.CardNumber.ToUpperInvariant().Contains(term) ||
                c.Federation.ToUpperInvariant().Contains(term) ||
                (c.Category ?? "").ToUpperInvariant().Contains(term)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<MembershipCardDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await teamService.DeleteMembershipCardAsync(id, ct);
}
