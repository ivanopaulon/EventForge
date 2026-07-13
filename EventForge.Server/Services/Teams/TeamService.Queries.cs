using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<PagedResult<TeamDto>> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for team operations.");
        }

        var query = context.Teams
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var teamDtos = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                ShortDescription = t.ShortDescription,
                LongDescription = t.LongDescription,
                Email = t.Email,
                Status = (Prym.DTOs.Common.TeamStatus)t.Status,
                EventId = t.EventId,
                EventName = t.Event != null ? t.Event.Name : null,
                ClubCode = t.ClubCode,
                FederationCode = t.FederationCode,
                Category = t.Category,
                CoachContactId = t.CoachContactId,
                TeamLogoDocumentId = t.TeamLogoDocumentId,
                MemberCount = t.Members.Count(m => !m.IsDeleted && m.TenantId == currentTenantId.Value),
                CreatedAt = t.CreatedAt,
                CreatedBy = t.CreatedBy,
                ModifiedAt = t.ModifiedAt,
                ModifiedBy = t.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TeamDto>
        {
            Items = teamDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IEnumerable<TeamDto>> GetTeamsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var teams = await context.Teams
            .AsNoTracking()
            .Where(t => t.EventId == eventId && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(MapToTeamDto);
    }

    public async Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var team = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == id && t.TenantId == currentTenantId && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            logger.LogWarning("Team con ID {TeamId} non trovato.", id);
            return null;
        }

        return MapToTeamDto(team);
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var team = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == id && t.TenantId == currentTenantId && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            logger.LogWarning("Team con ID {TeamId} non trovato per dettagli.", id);
            return null;
        }

        return MapToTeamDetailDto(team);
    }

}
