using Prym.DTOs.Common;
using Prym.DTOs.Teams;
using Prym.Web.Services.Teams;

namespace Prym.Web.Shared.Management.Adapters;

public class TeamMemberManagementService(ITeamService teamService, Guid teamId)
    : IEntityManagementService<TeamMemberDto>
{
    public async Task<PagedResult<TeamMemberDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await teamService.GetTeamMembersAsync(teamId, ct))?.ToList() ?? new List<TeamMemberDto>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(m =>
                m.FullName.ToUpperInvariant().Contains(term) ||
                (m.FiscalCode ?? "").ToUpperInvariant().Contains(term) ||
                (m.Role ?? "").ToUpperInvariant().Contains(term)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<TeamMemberDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await teamService.DeleteTeamMemberAsync(id, ct);
}
