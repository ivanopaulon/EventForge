using Microsoft.EntityFrameworkCore;
using EventForge.Models.Audit;
using EventForge.Models.Events;
using EventForge.Models.Teams;

namespace EventForge.Services.Events;

/// <summary>
/// Service implementation for managing events.
/// </summary>
public class EventService : IEventService
{
    private readonly EventForgeDbContext _context;

    public EventService(EventForgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Event CRUD operations

    public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Where(e => !e.IsDeleted)
            .Include(e => e.Teams.Where(t => !t.IsDeleted));

        var totalCount = await query.CountAsync(cancellationToken);
        var events = await query
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var eventDtos = events.Select(MapToEventDto);

        return new PagedResult<EventDto>
        {
            Items = eventDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .Include(e => e.Teams.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return eventEntity != null ? MapToEventDto(eventEntity) : null;
    }

    public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .Include(e => e.Teams.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return eventEntity != null ? MapToEventDetailDto(eventEntity) : null;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createEventDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var eventEntity = new Event
        {
            Name = createEventDto.Name,
            ShortDescription = createEventDto.ShortDescription,
            LongDescription = createEventDto.LongDescription,
            Location = createEventDto.Location,
            StartDate = createEventDto.StartDate,
            EndDate = createEventDto.EndDate,
            Capacity = createEventDto.Capacity,
            Status = createEventDto.Status,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        // Validate domain invariants
        eventEntity.CheckInvariants();

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var createdEvent = await _context.Events
            .Include(e => e.Teams)
            .FirstAsync(e => e.Id == eventEntity.Id, cancellationToken);

        return MapToEventDto(createdEvent);
    }

    public async Task<EventDto?> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateEventDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var eventEntity = await _context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .Include(e => e.Teams.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (eventEntity == null) return null;

        eventEntity.Name = updateEventDto.Name;
        eventEntity.ShortDescription = updateEventDto.ShortDescription;
        eventEntity.LongDescription = updateEventDto.LongDescription;
        eventEntity.Location = updateEventDto.Location;
        eventEntity.StartDate = updateEventDto.StartDate;
        eventEntity.EndDate = updateEventDto.EndDate;
        eventEntity.Capacity = updateEventDto.Capacity;
        eventEntity.Status = updateEventDto.Status;
        eventEntity.ModifiedBy = currentUser;
        eventEntity.ModifiedAt = DateTime.UtcNow;

        // Validate domain invariants
        eventEntity.CheckInvariants();

        await _context.SaveChangesAsync(cancellationToken);
        return MapToEventDto(eventEntity);
    }

    public async Task<bool> DeleteEventAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var eventEntity = await _context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (eventEntity == null) return false;

        // Soft delete the event and all its teams and team members
        eventEntity.IsDeleted = true;
        eventEntity.DeletedBy = currentUser;
        eventEntity.DeletedAt = DateTime.UtcNow;

        // Also soft delete all teams for this event
        var teams = await _context.Teams
            .Where(t => t.EventId == id && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var team in teams)
        {
            team.IsDeleted = true;
            team.DeletedBy = currentUser;
            team.DeletedAt = DateTime.UtcNow;

            // Also soft delete all team members
            var members = await _context.TeamMembers
                .Where(m => m.TeamId == team.Id && !m.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var member in members)
            {
                member.IsDeleted = true;
                member.DeletedBy = currentUser;
                member.DeletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    // Private mapping methods

    private static EventDto MapToEventDto(Event eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,
            Name = eventEntity.Name,
            ShortDescription = eventEntity.ShortDescription,
            LongDescription = eventEntity.LongDescription,
            Location = eventEntity.Location,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Capacity = eventEntity.Capacity,
            Status = eventEntity.Status,
            TeamCount = eventEntity.Teams.Count(t => !t.IsDeleted),
            CreatedAt = eventEntity.CreatedAt,
            CreatedBy = eventEntity.CreatedBy,
            ModifiedAt = eventEntity.ModifiedAt,
            ModifiedBy = eventEntity.ModifiedBy
        };
    }

    private static EventDetailDto MapToEventDetailDto(Event eventEntity)
    {
        return new EventDetailDto
        {
            Id = eventEntity.Id,
            Name = eventEntity.Name,
            ShortDescription = eventEntity.ShortDescription,
            LongDescription = eventEntity.LongDescription,
            Location = eventEntity.Location,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Capacity = eventEntity.Capacity,
            Status = eventEntity.Status,
            Teams = eventEntity.Teams.Where(t => !t.IsDeleted).Select(MapToTeamDetailDto).ToList(),
            CreatedAt = eventEntity.CreatedAt,
            CreatedBy = eventEntity.CreatedBy,
            ModifiedAt = eventEntity.ModifiedAt,
            ModifiedBy = eventEntity.ModifiedBy
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