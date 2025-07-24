using Microsoft.EntityFrameworkCore;
using EventForge.Models.Events;
using EventForge.Models.Audit;
using EventForge.Data.Entities.Events;
using EventForge.Data.Entities.Teams;

namespace EventForge.Services.Events;

/// <summary>
/// Service implementation for managing events and related operations.
/// </summary>
public class EventService : IEventService
{
    private readonly EventForgeDbContext _context;

    public EventService(EventForgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets all events with optional filtering and pagination.
    /// </summary>
    public async Task<PagedResult<EventResponseDto>> GetEventsAsync(
        EventQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var query = _context.Events.AsQueryable().Where(e => !e.IsDeleted);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryParameters.Name))
        {
            query = query.Where(e => e.Name.Contains(queryParameters.Name));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Location))
        {
            query = query.Where(e => e.Location.Contains(queryParameters.Location));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(e => (int)e.Status == queryParameters.Status.Value);
        }

        if (queryParameters.StartDateFrom.HasValue)
        {
            query = query.Where(e => e.StartDate >= queryParameters.StartDateFrom.Value);
        }

        if (queryParameters.StartDateTo.HasValue)
        {
            query = query.Where(e => e.StartDate <= queryParameters.StartDateTo.Value);
        }

        if (queryParameters.EndDateFrom.HasValue)
        {
            query = query.Where(e => e.EndDate >= queryParameters.EndDateFrom.Value);
        }

        if (queryParameters.EndDateTo.HasValue)
        {
            query = query.Where(e => e.EndDate <= queryParameters.EndDateTo.Value);
        }

        if (queryParameters.MinCapacity.HasValue)
        {
            query = query.Where(e => e.Capacity >= queryParameters.MinCapacity.Value);
        }

        if (queryParameters.MaxCapacity.HasValue)
        {
            query = query.Where(e => e.Capacity <= queryParameters.MaxCapacity.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        // Apply pagination
        var events = await query
            .Skip(queryParameters.Skip)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        // Convert to DTOs
        var eventDtos = new List<EventResponseDto>();
        foreach (var eventEntity in events)
        {
            var dto = await MapToEventResponseDto(eventEntity, queryParameters.IncludeTeams, cancellationToken);
            eventDtos.Add(dto);
        }

        return new PagedResult<EventResponseDto>
        {
            Items = eventDtos,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    public async Task<EventResponseDto?> GetEventByIdAsync(
        Guid id,
        bool includeTeams = false,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (eventEntity == null)
            return null;

        return await MapToEventResponseDto(eventEntity, includeTeams, cancellationToken);
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    public async Task<EventResponseDto> CreateEventAsync(
        EventCreateDto eventCreateDto,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventCreateDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var eventEntity = new Event
        {
            Name = eventCreateDto.Name,
            ShortDescription = eventCreateDto.ShortDescription,
            LongDescription = eventCreateDto.LongDescription ?? string.Empty,
            Location = eventCreateDto.Location ?? string.Empty,
            StartDate = eventCreateDto.StartDate,
            EndDate = eventCreateDto.EndDate,
            Capacity = eventCreateDto.Capacity,
            Status = (EventStatus)eventCreateDto.Status,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // Check domain invariants
        eventEntity.CheckInvariants();

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapToEventResponseDto(eventEntity, false, cancellationToken);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    public async Task<EventResponseDto?> UpdateEventAsync(
        Guid id,
        EventUpdateDto eventUpdateDto,
        string modifiedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventUpdateDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(modifiedBy);

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (eventEntity == null)
            return null;

        // Update properties
        eventEntity.Name = eventUpdateDto.Name;
        eventEntity.ShortDescription = eventUpdateDto.ShortDescription;
        eventEntity.LongDescription = eventUpdateDto.LongDescription ?? string.Empty;
        eventEntity.Location = eventUpdateDto.Location ?? string.Empty;
        eventEntity.StartDate = eventUpdateDto.StartDate;
        eventEntity.EndDate = eventUpdateDto.EndDate;
        eventEntity.Capacity = eventUpdateDto.Capacity;
        eventEntity.Status = (EventStatus)eventUpdateDto.Status;
        eventEntity.ModifiedBy = modifiedBy;
        eventEntity.ModifiedAt = DateTime.UtcNow;

        // Check domain invariants
        eventEntity.CheckInvariants();

        await _context.SaveChangesAsync(cancellationToken);

        return await MapToEventResponseDto(eventEntity, false, cancellationToken);
    }

    /// <summary>
    /// Deletes an event (soft delete).
    /// </summary>
    public async Task<bool> DeleteEventAsync(
        Guid id,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (eventEntity == null)
            return false;

        // Soft delete
        eventEntity.IsDeleted = true;
        eventEntity.DeletedBy = deletedBy;
        eventEntity.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    public async Task<bool> EventExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Gets events by status.
    /// </summary>
    public async Task<IEnumerable<EventResponseDto>> GetEventsByStatusAsync(
        int status,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Where(e => (int)e.Status == status && !e.IsDeleted)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);

        var eventDtos = new List<EventResponseDto>();
        foreach (var eventEntity in events)
        {
            var dto = await MapToEventResponseDto(eventEntity, false, cancellationToken);
            eventDtos.Add(dto);
        }

        return eventDtos;
    }

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    public async Task<IEnumerable<EventResponseDto>> GetEventsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Where(e => e.StartDate >= fromDate && e.StartDate <= toDate && !e.IsDeleted)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);

        var eventDtos = new List<EventResponseDto>();
        foreach (var eventEntity in events)
        {
            var dto = await MapToEventResponseDto(eventEntity, false, cancellationToken);
            eventDtos.Add(dto);
        }

        return eventDtos;
    }

    /// <summary>
    /// Applies sorting to the query based on sort field and direction.
    /// </summary>
    private static IQueryable<Event> ApplySorting(
        IQueryable<Event> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isDescending 
                ? query.OrderByDescending(x => x.Name) 
                : query.OrderBy(x => x.Name),
            "location" => isDescending 
                ? query.OrderByDescending(x => x.Location) 
                : query.OrderBy(x => x.Location),
            "capacity" => isDescending 
                ? query.OrderByDescending(x => x.Capacity) 
                : query.OrderBy(x => x.Capacity),
            "status" => isDescending 
                ? query.OrderByDescending(x => x.Status) 
                : query.OrderBy(x => x.Status),
            "enddate" => isDescending 
                ? query.OrderByDescending(x => x.EndDate) 
                : query.OrderBy(x => x.EndDate),
            "createdat" => isDescending 
                ? query.OrderByDescending(x => x.CreatedAt) 
                : query.OrderBy(x => x.CreatedAt),
            "startdate" or _ => isDescending 
                ? query.OrderByDescending(x => x.StartDate) 
                : query.OrderBy(x => x.StartDate)
        };
    }

    /// <summary>
    /// Maps an Event entity to EventResponseDto.
    /// </summary>
    private async Task<EventResponseDto> MapToEventResponseDto(
        Event eventEntity,
        bool includeTeams,
        CancellationToken cancellationToken)
    {
        var dto = new EventResponseDto
        {
            Id = eventEntity.Id,
            Name = eventEntity.Name,
            ShortDescription = eventEntity.ShortDescription,
            LongDescription = eventEntity.LongDescription,
            Location = eventEntity.Location,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Capacity = eventEntity.Capacity,
            Status = (int)eventEntity.Status,
            StatusDescription = eventEntity.Status.ToString(),
            CreatedAt = eventEntity.CreatedAt,
            CreatedBy = eventEntity.CreatedBy,
            ModifiedAt = eventEntity.ModifiedAt,
            ModifiedBy = eventEntity.ModifiedBy
        };

        if (includeTeams)
        {
            var teams = await _context.Teams
                .Where(t => t.EventId == eventEntity.Id && !t.IsDeleted)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .ToListAsync(cancellationToken);

            dto.Teams = teams.Select(MapToTeamResponseDto).ToList();
            dto.TeamCount = teams.Count;
            dto.MemberCount = teams.Sum(t => t.Members.Count(m => !m.IsDeleted));
        }
        else
        {
            // Get counts without loading the data
            dto.TeamCount = await _context.Teams
                .CountAsync(t => t.EventId == eventEntity.Id && !t.IsDeleted, cancellationToken);
            dto.MemberCount = await _context.TeamMembers
                .Where(m => m.Team!.EventId == eventEntity.Id && !m.IsDeleted && !m.Team.IsDeleted)
                .CountAsync(cancellationToken);
        }

        return dto;
    }

    /// <summary>
    /// Maps a Team entity to TeamResponseDto.
    /// </summary>
    private static TeamResponseDto MapToTeamResponseDto(Team team)
    {
        return new TeamResponseDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            Status = (int)team.Status,
            StatusDescription = team.Status.ToString(),
            Members = team.Members.Where(m => !m.IsDeleted).Select(MapToTeamMemberResponseDto).ToList(),
            MemberCount = team.Members.Count(m => !m.IsDeleted)
        };
    }

    /// <summary>
    /// Maps a TeamMember entity to TeamMemberResponseDto.
    /// </summary>
    private static TeamMemberResponseDto MapToTeamMemberResponseDto(TeamMember member)
    {
        return new TeamMemberResponseDto
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            FullName = $"{member.FirstName} {member.LastName}".Trim(),
            Email = member.Email,
            Role = member.Role,
            DateOfBirth = member.DateOfBirth,
            Status = (int)member.Status,
            StatusDescription = member.Status.ToString()
        };
    }
}