using EventForge.DTOs.Teams;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Teams;

/// <summary>
/// Service implementation for managing teams and team members.
/// </summary>
public class TeamService : ITeamService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TeamService> _logger;

    public TeamService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<TeamService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Team CRUD operations

    public async Task<PagedResult<TeamDto>> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for team operations.");
            }

            var query = _context.Teams
                .WhereActiveTenant(currentTenantId.Value)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value));

            var totalCount = await query.CountAsync(cancellationToken);
            var teams = await query
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var teamDtos = teams.Select(MapToTeamDto);

            return new PagedResult<TeamDto>
            {
                Items = teamDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei team.");
            throw;
        }
    }

    public async Task<IEnumerable<TeamDto>> GetTeamsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _context.Teams
                .Where(t => t.EventId == eventId && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            return teams.Select(MapToTeamDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei team per l'evento {EventId}.", eventId);
            throw;
        }
    }

    public async Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato.", id);
                return null;
            }

            return MapToTeamDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per dettagli.", id);
                return null;
            }

            return MapToTeamDetailDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei dettagli del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto createTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createTeamDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var eventExists = await EventExistsAsync(createTeamDto.EventId, cancellationToken);
            if (!eventExists)
                throw new ArgumentException($"Event with ID {createTeamDto.EventId} does not exist.", nameof(createTeamDto));

            var team = new Team
            {
                Name = createTeamDto.Name,
                ShortDescription = createTeamDto.ShortDescription,
                LongDescription = createTeamDto.LongDescription,
                Email = createTeamDto.Email,
                EventId = createTeamDto.EventId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(team, "Insert", currentUser, null, cancellationToken);

            var createdTeam = await _context.Teams
                .Include(t => t.Event)
                .Include(t => t.Members)
                .FirstAsync(t => t.Id == team.Id, cancellationToken);

            return MapToTeamDto(createdTeam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del team.");
            throw;
        }
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto updateTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            // Recupera i valori originali per l'audit (preferibilmente AsNoTracking)
            var originalTeam = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            team.Name = updateTeamDto.Name;
            team.ShortDescription = updateTeamDto.ShortDescription;
            team.LongDescription = updateTeamDto.LongDescription;
            team.Email = updateTeamDto.Email;
            team.ModifiedBy = currentUser;
            team.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(team, "Update", currentUser, originalTeam, cancellationToken);

            return MapToTeamDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteTeamAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            var originalTeam = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            team.IsDeleted = true;
            team.DeletedBy = currentUser;
            team.DeletedAt = DateTime.UtcNow;

            var members = await _context.TeamMembers
                .Where(m => m.TeamId == id && !m.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var member in members)
            {
                member.IsDeleted = true;
                member.DeletedBy = currentUser;
                member.DeletedAt = DateTime.UtcNow;

                // Audit log per ogni membro eliminato
                await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, null, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log per il team eliminato
            await _auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la cancellazione del team {TeamId}.", id);
            throw;
        }
    }

    // Team Member operations

    public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == teamId && !m.IsDeleted)
            .Include(m => m.Team)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return members.Select(MapToTeamMemberDto);
    }

    public async Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _context.TeamMembers
            .Where(m => m.Id == id && !m.IsDeleted)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            _logger.LogWarning("Team member con ID {MemberId} non trovato.", id);
            return null;
        }

        return MapToTeamMemberDto(member);
    }

    public async Task<TeamMemberDto> AddTeamMemberAsync(CreateTeamMemberDto createTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createTeamMemberDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var teamExists = await TeamExistsAsync(createTeamMemberDto.TeamId, cancellationToken);
            if (!teamExists)
                throw new ArgumentException($"Team with ID {createTeamMemberDto.TeamId} does not exist.", nameof(createTeamMemberDto));

            var member = new TeamMember
            {
                FirstName = createTeamMemberDto.FirstName,
                LastName = createTeamMemberDto.LastName,
                Email = createTeamMemberDto.Email,
                Role = createTeamMemberDto.Role,
                DateOfBirth = createTeamMemberDto.DateOfBirth,
                TeamId = createTeamMemberDto.TeamId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(member, "Insert", currentUser, null, cancellationToken);

            var createdMember = await _context.TeamMembers
                .Include(m => m.Team)
                .FirstAsync(m => m.Id == member.Id, cancellationToken);

            return MapToTeamMemberDto(createdMember);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta di un membro al team.");
            throw;
        }
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberDto updateTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamMemberDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var member = await _context.TeamMembers
                .Where(m => m.Id == id && !m.IsDeleted)
                .Include(m => m.Team)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Team member con ID {MemberId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            var originalMember = await _context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.FirstName = updateTeamMemberDto.FirstName;
            member.LastName = updateTeamMemberDto.LastName;
            member.Email = updateTeamMemberDto.Email;
            member.Role = updateTeamMemberDto.Role;
            member.DateOfBirth = updateTeamMemberDto.DateOfBirth;
            member.ModifiedBy = currentUser;
            member.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(member, "Update", currentUser, originalMember, cancellationToken);

            return MapToTeamMemberDto(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del membro {MemberId}.", id);
            throw;
        }
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var member = await _context.TeamMembers
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Team member con ID {MemberId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            var originalMember = await _context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.IsDeleted = true;
            member.DeletedBy = currentUser;
            member.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la rimozione del membro {MemberId}.", id);
            throw;
        }
    }

    public async Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AnyAsync(t => t.Id == teamId && !t.IsDeleted, cancellationToken);
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    // Private mapping methods

    private static TeamDto MapToTeamDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            MemberCount = team.Members.Count(m => !m.IsDeleted),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    private static TeamDetailDto MapToTeamDetailDto(Team team)
    {
        return new TeamDetailDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            Members = team.Members.Where(m => !m.IsDeleted).Select(MapToTeamMemberDto).ToList(),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    private static TeamMemberDto MapToTeamMemberDto(TeamMember member)
    {
        return new TeamMemberDto
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Role = member.Role,
            DateOfBirth = member.DateOfBirth,
            TeamId = member.TeamId,
            TeamName = member.Team?.Name,
            CreatedAt = member.CreatedAt,
            CreatedBy = member.CreatedBy,
            ModifiedAt = member.ModifiedAt,
            ModifiedBy = member.ModifiedBy
        };
    }
}