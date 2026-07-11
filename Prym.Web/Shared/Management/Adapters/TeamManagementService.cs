using Prym.DTOs.Common;
using Prym.DTOs.Teams;
using Prym.Web.Services.Teams;

namespace Prym.Web.Shared.Management.Adapters;

public class TeamManagementService(ITeamService teamService)
    : IEntityManagementService<TeamDto>
{
    public async Task<PagedResult<TeamDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var result = await teamService.GetTeamsAsync(page, pageSize, ct);

        if (result == null)
        {
            return new PagedResult<TeamDto>
            {
                Items = new List<TeamDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var items = result.Items.ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            items = items.Where(t =>
                t.Name.ToUpperInvariant().Contains(term) ||
                (t.Category ?? "").ToUpperInvariant().Contains(term) ||
                (t.FederationCode ?? "").ToUpperInvariant().Contains(term) ||
                (t.ClubCode ?? "").ToUpperInvariant().Contains(term)).ToList();
        }

        return new PagedResult<TeamDto>
        {
            Items = items,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await teamService.DeleteTeamAsync(id, ct);
}
