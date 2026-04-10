using EventForge.DTOs.Teams;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Events;

public class EventService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<EventService> logger) : IEventService
{

    public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var query = context.Events
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder));

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
            throw;
        }
    }

    public async Task<PagedResult<EventDto>> GetEventsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var query = context.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.TenantId == currentTenantId.Value)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder));

            var totalCount = await query.CountAsync(cancellationToken);

            var events = await query
                .OrderBy(e => e.StartDate)
                .ThenBy(e => e.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var eventDtos = events.Select(MapToEventDto);

            return new PagedResult<EventDto>
            {
                Items = eventDtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<EventDto>> GetEventsByDateAsync(
        DateTime startDate,
        DateTime? endDate,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var end = endDate ?? startDate.AddYears(1);

            var query = context.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted
                    && e.TenantId == currentTenantId.Value
                    && e.StartDate >= startDate
                    && e.StartDate <= end)
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder));

            var totalCount = await query.CountAsync(cancellationToken);

            var events = await query
                .OrderBy(e => e.StartDate)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var eventDtos = events.Select(MapToEventDto);

            return new PagedResult<EventDto>
            {
                Items = eventDtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<EventDto>> GetUpcomingEventsAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var now = DateTime.UtcNow;

            var query = context.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted
                    && e.TenantId == currentTenantId.Value
                    && e.StartDate >= now)
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder));

            var totalCount = await query.CountAsync(cancellationToken);

            var events = await query
                .OrderBy(e => e.StartDate)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var eventDtos = events.Select(MapToEventDto);

            return new PagedResult<EventDto>
            {
                Items = eventDtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await context.Events
                .AsNoTracking()
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity is null)
            {
                logger.LogWarning("Evento con ID {EventId} non trovato.", id);
                return null;
            }

            return MapToEventDto(eventEntity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await context.Events
                .AsNoTracking()
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                    .ThenInclude(t => t.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value))
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder))
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity is null)
            {
                logger.LogWarning("Evento con ID {EventId} non trovato (dettaglio).", id);
                return null;
            }

            return MapToEventDetailDto(eventEntity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createEventDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
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
                Status = (EventForge.Server.Data.Entities.Events.EventStatus)(int)createEventDto.Status,
                Color = createEventDto.Color,
                AssignedToUserId = createEventDto.AssignedToUserId,
                Visibility = createEventDto.Visibility,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            // Add time slots provided in the DTO (sorted by SortOrder, then by position in list)
            for (var i = 0; i < createEventDto.TimeSlots.Count; i++)
            {
                var slotDto = createEventDto.TimeSlots[i];
                eventEntity.TimeSlots.Add(new EventTimeSlot
                {
                    EventId = eventEntity.Id,
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    Label = slotDto.Label,
                    SortOrder = slotDto.SortOrder == 0 ? i : slotDto.SortOrder
                });
            }

            eventEntity.CheckInvariants();

            _ = context.Events.Add(eventEntity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(eventEntity, "Insert", currentUser, null, cancellationToken);

            var createdEvent = await context.Events
                .Include(e => e.Teams)
                .Include(e => e.TimeSlots.OrderBy(s => s.SortOrder))
                .FirstAsync(e => e.Id == eventEntity.Id, cancellationToken);

            logger.LogInformation("Evento {EventId} creato da {User}.", eventEntity.Id, currentUser);

            return MapToEventDto(createdEvent);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<EventDto?> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateEventDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .Include(e => e.Teams.Where(t => !t.IsDeleted && t.TenantId == currentTenantId.Value))
                .Include(e => e.TimeSlots)
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity is null)
            {
                logger.LogWarning("Evento con ID {EventId} non trovato per aggiornamento.", id);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(eventEntity).CurrentValues.Clone();
            var originalEvent = (Event)originalValues.ToObject();

            eventEntity.Name = updateEventDto.Name;
            eventEntity.ShortDescription = updateEventDto.ShortDescription;
            eventEntity.LongDescription = updateEventDto.LongDescription;
            eventEntity.Location = updateEventDto.Location;
            eventEntity.StartDate = updateEventDto.StartDate;
            eventEntity.EndDate = updateEventDto.EndDate;
            eventEntity.Capacity = updateEventDto.Capacity;
            eventEntity.Status = (EventForge.Server.Data.Entities.Events.EventStatus)(int)updateEventDto.Status;
            eventEntity.Color = updateEventDto.Color;
            eventEntity.AssignedToUserId = updateEventDto.AssignedToUserId;
            eventEntity.Visibility = updateEventDto.Visibility;
            eventEntity.ModifiedBy = currentUser;
            eventEntity.ModifiedAt = DateTime.UtcNow;

            // Apply optimistic concurrency: if client provided a RowVersion, use it as the
            // expected original value so EF Core detects concurrent modifications.
            if (updateEventDto.RowVersion is not null && updateEventDto.RowVersion.Length > 0)
                context.Entry(eventEntity).Property(e => e.RowVersion).OriginalValue = updateEventDto.RowVersion;

            // Replace all time slots: remove existing ones, add new ones from DTO
            context.RemoveRange(eventEntity.TimeSlots);
            eventEntity.TimeSlots.Clear();
            for (var i = 0; i < updateEventDto.TimeSlots.Count; i++)
            {
                var slotDto = updateEventDto.TimeSlots[i];
                eventEntity.TimeSlots.Add(new EventTimeSlot
                {
                    EventId = eventEntity.Id,
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    Label = slotDto.Label,
                    SortOrder = slotDto.SortOrder == 0 ? i : slotDto.SortOrder
                });
            }

            eventEntity.CheckInvariants();

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Conflitto di concorrenza durante l'aggiornamento dell'evento {EventId}.", id);
                throw new InvalidOperationException("L'evento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(eventEntity, "Update", currentUser, originalEvent, cancellationToken);

            logger.LogInformation("Evento {EventId} aggiornato da {User}.", id, currentUser);

            return MapToEventDto(eventEntity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for event operations.");
            }

            var eventEntity = await context.Events
                .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (eventEntity is null)
            {
                logger.LogWarning("Evento con ID {EventId} non trovato per cancellazione.", id);
                return false;
            }

            // Create snapshot of original event state
            var originalEventValues = context.Entry(eventEntity).CurrentValues.Clone();
            var originalEvent = (Event)originalEventValues.ToObject();

            eventEntity.IsDeleted = true;
            eventEntity.DeletedBy = currentUser;
            eventEntity.DeletedAt = DateTime.UtcNow;

            // Apply optimistic concurrency for delete as well
            if (rowVersion is not null && rowVersion.Length > 0)
                context.Entry(eventEntity).Property(e => e.RowVersion).OriginalValue = rowVersion;

            var teams = await context.Teams
                .Where(t => t.EventId == id && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            // Create snapshots of all teams BEFORE modifying them
            var originalTeams = teams.ToDictionary(
                t => t.Id,
                t =>
                {
                    var originalValues = context.Entry(t).CurrentValues.Clone();
                    return (Team)originalValues.ToObject();
                }
            );

            // Batch load all team members upfront to avoid N+1 per-team query
            var teamIds = teams.Select(t => t.Id).ToList();
            var allMembers = await context.TeamMembers
                .Where(m => teamIds.Contains(m.TeamId) && !m.IsDeleted)
                .ToListAsync(cancellationToken);
            var membersByTeamId = allMembers.ToLookup(m => m.TeamId);

            foreach (var team in teams)
            {
                var originalTeam = originalTeams[team.Id];

                team.IsDeleted = true;
                team.DeletedBy = currentUser;
                team.DeletedAt = DateTime.UtcNow;

                _ = await auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

                var members = membersByTeamId[team.Id].ToList();

                // Create snapshots of all members BEFORE modifying them
                var originalMembers = members.ToDictionary(
                    m => m.Id,
                    m =>
                    {
                        var originalValues = context.Entry(m).CurrentValues.Clone();
                        return (TeamMember)originalValues.ToObject();
                    }
                );

                foreach (var member in members)
                {
                    var originalMember = originalMembers[member.Id];

                    member.IsDeleted = true;
                    member.DeletedBy = currentUser;
                    member.DeletedAt = DateTime.UtcNow;

                    _ = await auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);
                }
            }

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Conflitto di concorrenza durante la cancellazione dell'evento {EventId}.", id);
                throw new InvalidOperationException("L'evento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(eventEntity, "Delete", currentUser, originalEvent, cancellationToken);

            logger.LogInformation("Evento {EventId} cancellato da {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
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
            Status = (EventForge.DTOs.Common.EventStatus)(int)eventEntity.Status,
            Color = eventEntity.Color,
            AssignedToUserId = eventEntity.AssignedToUserId,
            Visibility = eventEntity.Visibility,
            TimeSlots = eventEntity.TimeSlots?
                .OrderBy(s => s.SortOrder)
                .Select(s => new EventTimeSlotDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Label = s.Label,
                    SortOrder = s.SortOrder
                })
                .ToList() ?? [],
            TeamCount = eventEntity.Teams?.Count(t => !t.IsDeleted) ?? 0,
            CreatedAt = eventEntity.CreatedAt,
            CreatedBy = eventEntity.CreatedBy,
            ModifiedAt = eventEntity.ModifiedAt,
            ModifiedBy = eventEntity.ModifiedBy,
            RowVersion = eventEntity.RowVersion
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
            Status = (EventForge.DTOs.Common.EventStatus)(int)eventEntity.Status,
            Color = eventEntity.Color,
            AssignedToUserId = eventEntity.AssignedToUserId,
            Visibility = eventEntity.Visibility,
            TimeSlots = eventEntity.TimeSlots?
                .OrderBy(s => s.SortOrder)
                .Select(s => new EventTimeSlotDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Label = s.Label,
                    SortOrder = s.SortOrder
                })
                .ToList() ?? [],
            Teams = eventEntity.Teams?.Where(t => !t.IsDeleted).Select(MapToTeamDetailDto).ToList() ?? [],
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
            Members = team.Members?.Where(m => !m.IsDeleted).Select(MapToTeamMemberDto).ToList() ?? [],
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
