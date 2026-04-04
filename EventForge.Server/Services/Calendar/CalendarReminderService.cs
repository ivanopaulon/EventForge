using EventForge.DTOs.Calendar;
using EventForge.DTOs.Common;
using EventForge.Server.Data.Entities.Calendar;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Calendar;

/// <summary>
/// Service for managing calendar reminders and tasks with multi-tenant support.
/// </summary>
public class CalendarReminderService : ICalendarReminderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CalendarReminderService> _logger;

    public CalendarReminderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<CalendarReminderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<CalendarReminderDto>> GetCalendarRemindersAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var query = _context.CalendarReminders
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value);

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
            _logger.LogError(ex, "Error retrieving paginated calendar reminders.");
            throw;
        }
    }

    public async Task<IEnumerable<CalendarReminderDto>> GetCalendarRemindersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var items = await _context.CalendarReminders
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value
                            && r.DueDate >= startDate && r.DueDate <= endDate)
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Title)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar reminders by date range {StartDate} - {EndDate}.", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<CalendarReminderDto>> GetActiveRemindersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var items = await _context.CalendarReminders
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value
                            && r.Status == DTOs.Common.ReminderStatus.Active
                            && !r.IsCompleted)
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Priority)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active calendar reminders.");
            throw;
        }
    }

    public async Task<CalendarReminderDto?> GetCalendarReminderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await _context.CalendarReminders
                .AsNoTracking()
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Calendar reminder with ID {ReminderId} not found.", id);
                return null;
            }

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar reminder {ReminderId}.", id);
            throw;
        }
    }

    public async Task<CalendarReminderDto> CreateCalendarReminderAsync(CreateCalendarReminderDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
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
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.CalendarReminders.Add(entity);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Calendar reminder {ReminderId} created by {User}.", entity.Id, currentUser);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar reminder.");
            throw;
        }
    }

    public async Task<CalendarReminderDto?> UpdateCalendarReminderAsync(Guid id, UpdateCalendarReminderDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await _context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Calendar reminder with ID {ReminderId} not found for update.", id);
                return null;
            }

            var originalValues = _context.Entry(entity).CurrentValues.Clone();
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
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = DateTime.UtcNow;

            if (updateDto.RowVersion != null)
                _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = updateDto.RowVersion;

            try
            {
                _ = await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating calendar reminder {ReminderId}.", id);
                throw new InvalidOperationException("The calendar reminder was modified by another user. Reload the record and try again.", ex);
            }

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Calendar reminder {ReminderId} updated by {User}.", id, currentUser);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar reminder {ReminderId}.", id);
            throw;
        }
    }

    public async Task<CalendarReminderDto?> CompleteCalendarReminderAsync(Guid id, string? completionNotes, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await _context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Calendar reminder with ID {ReminderId} not found for completion.", id);
                return null;
            }

            var originalValues = _context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (CalendarReminder)originalValues.ToObject();

            entity.IsCompleted = true;
            entity.Status = DTOs.Common.ReminderStatus.Completed;
            entity.CompletedAt = DateTime.UtcNow;
            entity.CompletedBy = currentUser;
            entity.CompletionNotes = completionNotes;
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Calendar reminder {ReminderId} completed by {User}.", id, currentUser);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing calendar reminder {ReminderId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteCalendarReminderAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for calendar reminder operations.");

            var entity = await _context.CalendarReminders
                .Where(r => r.Id == id && r.TenantId == currentTenantId.Value && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Calendar reminder with ID {ReminderId} not found for deletion.", id);
                return false;
            }

            var originalValues = _context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (CalendarReminder)originalValues.ToObject();

            entity.IsDeleted = true;
            entity.DeletedBy = currentUser;
            entity.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Calendar reminder {ReminderId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar reminder {ReminderId}.", id);
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
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }
}
