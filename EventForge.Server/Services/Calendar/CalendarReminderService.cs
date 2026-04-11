using EventForge.Server.Data.Entities.Calendar;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Calendar;

/// <summary>
/// Service for managing calendar reminders and tasks with multi-tenant support.
/// </summary>
public class CalendarReminderService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<CalendarReminderService> logger) : ICalendarReminderService
{

    public async Task<PagedResult<CalendarReminderDto>> GetCalendarRemindersAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var query = context.CalendarReminders
                .AsNoTracking()
                .Where(r => r.TenantId == currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Title)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<CalendarReminderDto>
            {
                Items = items.Select(MapToDto).ToList(),
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

    public async Task<IEnumerable<CalendarReminderDto>> GetCalendarRemindersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var items = await context.CalendarReminders
                .AsNoTracking()
                .Where(r => r.TenantId == currentTenantId.Value
                            && r.DueDate >= startDate && r.DueDate <= endDate)
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Title)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<CalendarReminderDto>> GetActiveRemindersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var items = await context.CalendarReminders
                .AsNoTracking()
                .Where(r => r.TenantId == currentTenantId.Value
                            && r.Status == DTOs.Common.ReminderStatus.Active
                            && !r.IsCompleted)
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Priority)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CalendarReminderDto?> GetCalendarReminderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await context.CalendarReminders
                .AsNoTracking()
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Calendar reminder with ID {ReminderId} not found.", id);
                return null;
            }

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CalendarReminderDto> CreateCalendarReminderAsync(CreateCalendarReminderDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = new CalendarReminder
            {
                TenantId = currentTenantId.Value,
                Title = createDto.Title,
                Description = createDto.Description,
                DueDate = createDto.DueDate,
                IsAllDay = createDto.IsAllDay,
                ItemType = createDto.ItemType,
                Priority = createDto.Priority,
                Status = createDto.Status,
                EventId = createDto.EventId,
                IsRecurring = createDto.IsRecurring,
                RecurrencePattern = createDto.RecurrencePattern,
                RecurrenceInterval = createDto.RecurrenceInterval,
                RecurrenceEndDate = createDto.RecurrenceEndDate,
                Color = createDto.Color,
                AssignedToUserId = createDto.AssignedToUserId,
                Visibility = createDto.Visibility,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = context.CalendarReminders.Add(entity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Calendar reminder {ReminderId} created by {User}.", entity.Id, currentUser);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CalendarReminderDto?> UpdateCalendarReminderAsync(Guid id, UpdateCalendarReminderDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Calendar reminder with ID {ReminderId} not found for update.", id);
                return null;
            }

            var originalValues = context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (CalendarReminder)originalValues.ToObject();

            entity.Title = updateDto.Title;
            entity.Description = updateDto.Description;
            entity.DueDate = updateDto.DueDate;
            entity.IsAllDay = updateDto.IsAllDay;
            entity.ItemType = updateDto.ItemType;
            entity.Priority = updateDto.Priority;
            entity.Status = updateDto.Status;
            entity.EventId = updateDto.EventId;
            entity.IsRecurring = updateDto.IsRecurring;
            entity.RecurrencePattern = updateDto.RecurrencePattern;
            entity.RecurrenceInterval = updateDto.RecurrenceInterval;
            entity.RecurrenceEndDate = updateDto.RecurrenceEndDate;
            entity.CompletionNotes = updateDto.CompletionNotes;
            entity.Color = updateDto.Color;
            entity.AssignedToUserId = updateDto.AssignedToUserId;
            entity.Visibility = updateDto.Visibility;
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = DateTime.UtcNow;

            if (updateDto.RowVersion is not null)
                context.Entry(entity).Property(e => e.RowVersion).OriginalValue = updateDto.RowVersion;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating calendar reminder {ReminderId}.", id);
                throw new InvalidOperationException("The calendar reminder was modified by another user. Reload the record and try again.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            logger.LogInformation("Calendar reminder {ReminderId} updated by {User}.", id, currentUser);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CalendarReminderDto?> CompleteCalendarReminderAsync(Guid id, string? completionNotes, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Calendar reminder with ID {ReminderId} not found for completion.", id);
                return null;
            }

            var originalValues = context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (CalendarReminder)originalValues.ToObject();

            entity.IsCompleted = true;
            entity.Status = DTOs.Common.ReminderStatus.Completed;
            entity.CompletedAt = DateTime.UtcNow;
            entity.CompletedBy = currentUser;
            entity.CompletionNotes = completionNotes;
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict completing calendar reminder {ReminderId}.", id);
                throw new InvalidOperationException("Il promemoria è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            logger.LogInformation("Calendar reminder {ReminderId} completed by {User}.", id, currentUser);

            return MapToDto(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteCalendarReminderAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Calendar reminder with ID {ReminderId} not found for deletion.", id);
                return false;
            }

            var originalValues = context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (CalendarReminder)originalValues.ToObject();

            entity.IsDeleted = true;
            entity.DeletedBy = currentUser;
            entity.DeletedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting calendar reminder {ReminderId}.", id);
                throw new InvalidOperationException("Il promemoria è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            logger.LogInformation("Calendar reminder {ReminderId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private static CalendarReminderDto MapToDto(CalendarReminder entity)
    {
        return new CalendarReminderDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            DueDate = entity.DueDate,
            IsAllDay = entity.IsAllDay,
            ItemType = entity.ItemType,
            Priority = entity.Priority,
            Status = entity.Status,
            IsCompleted = entity.IsCompleted,
            CompletedAt = entity.CompletedAt,
            CompletedBy = entity.CompletedBy,
            CompletionNotes = entity.CompletionNotes,
            EventId = entity.EventId,
            IsRecurring = entity.IsRecurring,
            RecurrencePattern = entity.RecurrencePattern,
            RecurrenceInterval = entity.RecurrenceInterval,
            RecurrenceEndDate = entity.RecurrenceEndDate,
            Color = entity.Color,
            AssignedToUserId = entity.AssignedToUserId,
            Visibility = entity.Visibility,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

}
