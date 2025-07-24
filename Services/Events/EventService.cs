using EventForge.Models.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EventForge.Services.Audit;

namespace EventForge.Services.Events;

/// <summary>
/// Service implementation for managing events.
/// </summary>
public class EventService : IEventService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<EventService> _logger;

    public EventService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<EventService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
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

            // Audit log
            await _auditLogService.TrackEntityChangesAsync(eventEntity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            var createdEvent = await _context.Events
                .Include(e => e.Teams)
                .FirstAsync(e => e.Id == eventEntity.Id, cancellationToken);

            return MapToEventDto(createdEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione dell'evento.");
            throw;
        }
    }

    public async Task<EventDto?> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateEventDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null) return null;

            // Copia originale per audit
            var originalEvent = new Event
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
                CreatedBy = eventEntity.CreatedBy,
                CreatedAt = eventEntity.CreatedAt,
                ModifiedBy = eventEntity.ModifiedBy,
                ModifiedAt = eventEntity.ModifiedAt,
                DeletedBy = eventEntity.DeletedBy,
                DeletedAt = eventEntity.DeletedAt,
                IsDeleted = eventEntity.IsDeleted,
                RowVersion = eventEntity.RowVersion
            };

            // Aggiorna i campi
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

            // Imposta la RowVersion ricevuta dal client
            eventEntity.RowVersion = updateEventDto.RowVersion;

            // Validate domain invariants
            eventEntity.CheckInvariants();

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflitto di concorrenza durante l'aggiornamento dell'evento {EventId}.", id);
                throw new InvalidOperationException("L'evento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            await _auditLogService.TrackEntityChangesAsync(eventEntity, "Update", currentUser, originalEvent, cancellationToken);

            return MapToEventDto(eventEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento dell'evento {EventId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null) return false;

            // Copia originale per audit
            var originalEvent = new Event
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
                CreatedBy = eventEntity.CreatedBy,
                CreatedAt = eventEntity.CreatedAt,
                ModifiedBy = eventEntity.ModifiedBy,
                ModifiedAt = eventEntity.ModifiedAt,
                DeletedBy = eventEntity.DeletedBy,
                DeletedAt = eventEntity.DeletedAt,
                IsDeleted = eventEntity.IsDeleted,
                RowVersion = eventEntity.RowVersion
            };

            // Soft delete
            eventEntity.IsDeleted = true;
            eventEntity.DeletedBy = currentUser;
            eventEntity.DeletedAt = DateTime.UtcNow;
            eventEntity.RowVersion = rowVersion; // <-- Gestione concorrenza

            // Soft delete all teams and members
            var teams = await _context.Teams
                .Where(t => t.EventId == id && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var team in teams)
            {
                // Copia originale per audit
                var originalTeam = new Team
                {
                    Id = team.Id,
                    Name = team.Name,
                    ShortDescription = team.ShortDescription,
                    LongDescription = team.LongDescription,
                    Email = team.Email,
                    Status = team.Status,
                    EventId = team.EventId,
                    CreatedBy = team.CreatedBy,
                    CreatedAt = team.CreatedAt,
                    ModifiedBy = team.ModifiedBy,
                    ModifiedAt = team.ModifiedAt,
                    DeletedBy = team.DeletedBy,
                    DeletedAt = team.DeletedAt,
                    IsDeleted = team.IsDeleted
                };

                team.IsDeleted = true;
                team.DeletedBy = currentUser;
                team.DeletedAt = DateTime.UtcNow;

                // Audit log per il team
                await _auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

                var members = await _context.TeamMembers
                    .Where(m => m.TeamId == team.Id && !m.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var member in members)
                {
                    // Copia originale per audit
                    var originalMember = new TeamMember
                    {
                        Id = member.Id,
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        Email = member.Email,
                        Role = member.Role,
                        DateOfBirth = member.DateOfBirth,
                        Status = member.Status,
                        TeamId = member.TeamId,
                        CreatedBy = member.CreatedBy,
                        CreatedAt = member.CreatedAt,
                        ModifiedBy = member.ModifiedBy,
                        ModifiedAt = member.ModifiedAt,
                        DeletedBy = member.DeletedBy,
                        DeletedAt = member.DeletedAt,
                        IsDeleted = member.IsDeleted
                    };

                    member.IsDeleted = true;
                    member.DeletedBy = currentUser;
                    member.DeletedAt = DateTime.UtcNow;

                    // Audit log per il membro
                    await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log per l'evento eliminato
            await _auditLogService.TrackEntityChangesAsync(eventEntity, "Delete", currentUser, originalEvent, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la cancellazione dell'evento {EventId}.", id);
            throw;
        }
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