using Microsoft.EntityFrameworkCore;
using EventForge.Models.Audit;
using EventForge.Models.Teams;

namespace EventForge.Services.Teams;

/// <summary>
/// Service implementation for managing teams and team members.
/// </summary>
public class TeamService : ITeamService
{
    private readonly EventForgeDbContext _context;

    public TeamService(EventForgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Team CRUD operations

    public async Task<PagedResult<TeamDto>> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.Teams
            .Where(t => !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted));

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

    public async Task<IEnumerable<TeamDto>> GetTeamsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var teams = await _context.Teams
            .Where(t => t.EventId == eventId && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(MapToTeamDto);
    }

    public async Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await _context.Teams
            .Where(t => t.Id == id && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return team != null ? MapToTeamDto(team) : null;
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await _context.Teams
            .Where(t => t.Id == id && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return team != null ? MapToTeamDetailDto(team) : null;
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto createTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createTeamDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        // Verify that the event exists
        var eventExists = await EventExistsAsync(createTeamDto.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new ArgumentException($"Event with ID {createTeamDto.EventId} does not exist.", nameof(createTeamDto));
        }

        var team = new Team
        {
            Name = createTeamDto.Name,
            ShortDescription = createTeamDto.ShortDescription,
            LongDescription = createTeamDto.LongDescription,
            Email = createTeamDto.Email,
            Status = createTeamDto.Status,
            EventId = createTeamDto.EventId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var createdTeam = await _context.Teams
            .Include(t => t.Event)
            .Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id, cancellationToken);

        return MapToTeamDto(createdTeam);
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto updateTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateTeamDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var team = await _context.Teams
            .Where(t => t.Id == id && !t.IsDeleted)
            .Include(t => t.Event)
            .Include(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (team == null) return null;

        team.Name = updateTeamDto.Name;
        team.ShortDescription = updateTeamDto.ShortDescription;
        team.LongDescription = updateTeamDto.LongDescription;
        team.Email = updateTeamDto.Email;
        team.Status = updateTeamDto.Status;
        team.ModifiedBy = currentUser;
        team.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToTeamDto(team);
    }

    public async Task<bool> DeleteTeamAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var team = await _context.Teams
            .Where(t => t.Id == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (team == null) return false;

        // Soft delete the team and all its members
        team.IsDeleted = true;
        team.DeletedBy = currentUser;
        team.DeletedAt = DateTime.UtcNow;

        // Also soft delete all team members
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == id && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var member in members)
        {
            member.IsDeleted = true;
            member.DeletedBy = currentUser;
            member.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

        return member != null ? MapToTeamMemberDto(member) : null;
    }

    public async Task<TeamMemberDto> AddTeamMemberAsync(CreateTeamMemberDto createTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createTeamMemberDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        // Verify that the team exists
        var teamExists = await TeamExistsAsync(createTeamMemberDto.TeamId, cancellationToken);
        if (!teamExists)
        {
            throw new ArgumentException($"Team with ID {createTeamMemberDto.TeamId} does not exist.", nameof(createTeamMemberDto));
        }

        var member = new TeamMember
        {
            FirstName = createTeamMemberDto.FirstName,
            LastName = createTeamMemberDto.LastName,
            Email = createTeamMemberDto.Email,
            Role = createTeamMemberDto.Role,
            DateOfBirth = createTeamMemberDto.DateOfBirth,
            Status = createTeamMemberDto.Status,
            TeamId = createTeamMemberDto.TeamId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var createdMember = await _context.TeamMembers
            .Include(m => m.Team)
            .FirstAsync(m => m.Id == member.Id, cancellationToken);

        return MapToTeamMemberDto(createdMember);
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberDto updateTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateTeamMemberDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var member = await _context.TeamMembers
            .Where(m => m.Id == id && !m.IsDeleted)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null) return null;

        member.FirstName = updateTeamMemberDto.FirstName;
        member.LastName = updateTeamMemberDto.LastName;
        member.Email = updateTeamMemberDto.Email;
        member.Role = updateTeamMemberDto.Role;
        member.DateOfBirth = updateTeamMemberDto.DateOfBirth;
        member.Status = updateTeamMemberDto.Status;
        member.ModifiedBy = currentUser;
        member.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToTeamMemberDto(member);
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var member = await _context.TeamMembers
            .Where(m => m.Id == id && !m.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null) return false;

        member.IsDeleted = true;
        member.DeletedBy = currentUser;
        member.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
            Status = team.Status,
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
            Status = team.Status,
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
            Status = member.Status,
            TeamId = member.TeamId,
            TeamName = member.Team?.Name,
            CreatedAt = member.CreatedAt,
            CreatedBy = member.CreatedBy,
            ModifiedAt = member.ModifiedAt,
            ModifiedBy = member.ModifiedBy
        };
    }
}