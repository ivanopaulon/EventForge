using EventForge.DTOs.Teams;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Events;

public class EventService : IEventService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EventService> _logger;

    public EventService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<EventService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var query = _context.Events
                .WhereActiveTenant(currentTenantId.Value)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value));

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero degli eventi.");
            throw;
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Evento con ID {EventId} non trovato.", id);
                return null;
            }

            return MapToEventDto(eventEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dell'evento {EventId}.", id);
            throw;
        }
    }

    public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                    .ThenInclude(t => t.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Evento con ID {EventId} non trovato (dettaglio).", id);
                return null;
            }

            return MapToEventDetailDto(eventEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del dettaglio evento {EventId}.", id);
            throw;
        }
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createEventDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = new Event
            {
                TenantId = currentTenantId.Value,
                Name = createEventDto.Name,
                ShortDescription = createEventDto.ShortDescription,
                LongDescription = createEventDto.LongDescription,
                Location = createEventDto.Location,
                StartDate = createEventDto.StartDate,
                EndDate = createEventDto.EndDate,
                Capacity = createEventDto.Capacity,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            eventEntity.CheckInvariants();

            _ = _context.Events.Add(eventEntity);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(eventEntity, "Insert", currentUser, null, cancellationToken);

            var createdEvent = await _context.Events
                .Include(e => e.Teams)
                .FirstAsync(e => e.Id == eventEntity.Id, cancellationToken);

            _logger.LogInformation("Evento {EventId} creato da {User}.", eventEntity.Id, currentUser);

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

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Evento con ID {EventId} non trovato per aggiornamento.", id);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = _context.Entry(eventEntity).CurrentValues.Clone();
            var originalEvent = (Event)originalValues.ToObject();

            eventEntity.Name = updateEventDto.Name;
            eventEntity.ShortDescription = updateEventDto.ShortDescription;
            eventEntity.LongDescription = updateEventDto.LongDescription;
            eventEntity.Location = updateEventDto.Location;
            eventEntity.StartDate = updateEventDto.StartDate;
            eventEntity.EndDate = updateEventDto.EndDate;
            eventEntity.Capacity = updateEventDto.Capacity;
            eventEntity.ModifiedBy = currentUser;
            eventEntity.ModifiedAt = DateTime.UtcNow;
            eventEntity.RowVersion = updateEventDto.RowVersion;

            eventEntity.CheckInvariants();

            try
            {
                _ = await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflitto di concorrenza durante l'aggiornamento dell'evento {EventId}.", id);
                throw new InvalidOperationException("L'evento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await _auditLogService.TrackEntityChangesAsync(eventEntity, "Update", currentUser, originalEvent, cancellationToken);

            _logger.LogInformation("Evento {EventId} aggiornato da {User}.", id, currentUser);

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

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await _context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Evento con ID {EventId} non trovato per cancellazione.", id);
                return false;
            }

            // Create snapshot of original event state
            var originalEventValues = _context.Entry(eventEntity).CurrentValues.Clone();
            var originalEvent = (Event)originalEventValues.ToObject();

            eventEntity.IsDeleted = true;
            eventEntity.DeletedBy = currentUser;
            eventEntity.DeletedAt = DateTime.UtcNow;
            eventEntity.RowVersion = rowVersion;

            var teams = await _context.Teams
                .Where(t => t.EventId == id && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            // Create snapshots of all teams BEFORE modifying them
            var originalTeams = teams.ToDictionary(
                t => t.Id,
                t => {
                    var originalValues = _context.Entry(t).CurrentValues.Clone();
                    return (Team)originalValues.ToObject();
                }
            );

            foreach (var team in teams)
            {
                var originalTeam = originalTeams[team.Id];

                team.IsDeleted = true;
                team.DeletedBy = currentUser;
                team.DeletedAt = DateTime.UtcNow;

                _ = await _auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

                var members = await _context.TeamMembers
                    .Where(m => m.TeamId == team.Id && !m.IsDeleted)
                    .ToListAsync(cancellationToken);

                // Create snapshots of all members BEFORE modifying them
                var originalMembers = members.ToDictionary(
                    m => m.Id,
                    m => {
                        var originalValues = _context.Entry(m).CurrentValues.Clone();
                        return (TeamMember)originalValues.ToObject();
                    }
                );

                foreach (var member in members)
                {
                    var originalMember = originalMembers[member.Id];

                    member.IsDeleted = true;
                    member.DeletedBy = currentUser;
                    member.DeletedAt = DateTime.UtcNow;

                    _ = await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);
                }
            }

            try
            {
                _ = await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflitto di concorrenza durante la cancellazione dell'evento {EventId}.", id);
                throw new InvalidOperationException("L'evento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await _auditLogService.TrackEntityChangesAsync(eventEntity, "Delete", currentUser, originalEvent, cancellationToken);

            _logger.LogInformation("Evento {EventId} cancellato da {User}.", id, currentUser);

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
        try
        {
            return await _context.Events
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il controllo esistenza evento {EventId}.", eventId);
            throw;
        }
    }

    // --- Mapping methods (come in BankService) ---

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
            TeamCount = eventEntity.Teams?.Count(t => !t.IsDeleted) ?? 0,
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
            Teams = eventEntity.Teams?.Where(t => !t.IsDeleted).Select(MapToTeamDetailDto).ToList() ?? new List<TeamDetailDto>(),
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
            EventId = team.EventId,
            EventName = team.Event?.Name,
            Members = team.Members?.Where(m => !m.IsDeleted).Select(MapToTeamMemberDto).ToList() ?? new List<TeamMemberDto>(),
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