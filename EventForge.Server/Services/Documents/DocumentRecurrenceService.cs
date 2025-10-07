using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document recurrence
/// </summary>
public class DocumentRecurrenceService : IDocumentRecurrenceService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentRecurrenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentRecurrenceService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="logger">Logger</param>
    public DocumentRecurrenceService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentRecurrenceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRecurrenceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .Where(dr => dr.IsActive)
                .OrderBy(dr => dr.Name)
                .ToListAsync(cancellationToken);

            return DocumentRecurrenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document recurrences.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentRecurrenceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            return entity == null ? null : DocumentRecurrenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document recurrence {RecurrenceId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRecurrenceDto>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .Where(dr => dr.TemplateId == templateId && dr.IsActive)
                .OrderBy(dr => dr.Name)
                .ToListAsync(cancellationToken);

            return DocumentRecurrenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document recurrences for template {TemplateId}.", templateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRecurrenceDto>> GetActiveSchedulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .Where(dr => dr.IsEnabled && dr.IsActive && dr.Status == RecurrenceStatus.Active)
                .OrderBy(dr => dr.NextExecutionDate)
                .ToListAsync(cancellationToken);

            return DocumentRecurrenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active document recurrence schedules.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRecurrenceDto>> GetDueForExecutionAsync(DateTime upToDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .Where(dr => dr.IsEnabled &&
                           dr.IsActive &&
                           dr.Status == RecurrenceStatus.Active &&
                           dr.NextExecutionDate.HasValue &&
                           dr.NextExecutionDate.Value <= upToDate)
                .OrderBy(dr => dr.NextExecutionDate)
                .ToListAsync(cancellationToken);

            return DocumentRecurrenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document recurrences due for execution up to {UpToDate}.", upToDate);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRecurrenceDto>> GetByStatusAsync(RecurrenceStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .Where(dr => dr.Status == status && dr.IsActive)
                .OrderBy(dr => dr.Name)
                .ToListAsync(cancellationToken);

            return DocumentRecurrenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document recurrences with status {Status}.", status);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentRecurrenceDto> CreateAsync(CreateDocumentRecurrenceDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = DocumentRecurrenceMapper.ToEntity(createDto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            // Calculate next execution date
            entity.NextExecutionDate = CalculateNextExecutionDate(entity);

            _ = _context.DocumentRecurrences.Add(entity);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync<DocumentRecurrence>(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await _context.Entry(entity)
                .Reference(dr => dr.Template)
                .LoadAsync(cancellationToken);

            _logger.LogInformation("Document recurrence {RecurrenceId} created by {User}.", entity.Id, currentUser);

            return DocumentRecurrenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document recurrence for user {User}.", currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentRecurrenceDto?> UpdateAsync(Guid id, UpdateDocumentRecurrenceDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentRecurrences
                .Include(dr => dr.Template)
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document recurrence {RecurrenceId} not found for update.", id);
                return null;
            }

            var originalValues = entity.ToString();

            DocumentRecurrenceMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            // Recalculate next execution date if pattern changed
            if (entity.Pattern != updateDto.Pattern || entity.Interval != updateDto.Interval)
            {
                entity.NextExecutionDate = CalculateNextExecutionDate(entity);
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync<DocumentRecurrence>(entity, "Update", currentUser, null, cancellationToken);

            _logger.LogInformation("Document recurrence {RecurrenceId} updated by {User}.", id, currentUser);

            return DocumentRecurrenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document recurrence {RecurrenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentRecurrences
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document recurrence {RecurrenceId} not found for deletion.", id);
                return false;
            }

            // Soft delete
            entity.IsActive = false;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync<DocumentRecurrence>(entity, "SoftDelete", currentUser, null, cancellationToken);

            _logger.LogInformation("Document recurrence {RecurrenceId} soft deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document recurrence {RecurrenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetEnabledStatusAsync(Guid id, bool isEnabled, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentRecurrences
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document recurrence {RecurrenceId} not found for status update.", id);
                return false;
            }

            entity.IsEnabled = isEnabled;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync<DocumentRecurrence>(entity, "StatusUpdate", currentUser, null, cancellationToken);

            _logger.LogInformation("Document recurrence {RecurrenceId} enabled status updated to {Status} by {User}.", id, isEnabled, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating enabled status for document recurrence {RecurrenceId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateExecutionTrackingAsync(Guid id, DateTime executionDate, bool success, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentRecurrences
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document recurrence {RecurrenceId} not found for execution tracking.", id);
                return false;
            }

            entity.LastExecutionDate = executionDate;
            entity.ExecutionCount++;

            if (success)
            {
                // Calculate next execution date
                entity.NextExecutionDate = CalculateNextExecutionDate(entity);

                // Check if we've reached max occurrences
                if (entity.MaxOccurrences.HasValue && entity.ExecutionCount >= entity.MaxOccurrences.Value)
                {
                    entity.Status = RecurrenceStatus.Completed;
                    entity.IsEnabled = false;
                }
            }
            else
            {
                entity.Status = RecurrenceStatus.Failed;
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document recurrence {RecurrenceId} execution tracking updated - Success: {Success}.", id, success);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating execution tracking for document recurrence {RecurrenceId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DateTime?> CalculateNextExecutionDateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentRecurrences
                .FirstOrDefaultAsync(dr => dr.Id == id && dr.IsActive, cancellationToken);

            return entity == null ? null : CalculateNextExecutionDate(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating next execution date for document recurrence {RecurrenceId}.", id);
            throw;
        }
    }

    /// <summary>
    /// Calculates the next execution date based on the recurrence pattern
    /// </summary>
    /// <param name="recurrence">The document recurrence entity</param>
    /// <returns>Next execution date or null if recurrence is complete</returns>
    private DateTime? CalculateNextExecutionDate(DocumentRecurrence recurrence)
    {
        if (!recurrence.IsEnabled || recurrence.Status != RecurrenceStatus.Active)
            return null;

        var baseDate = recurrence.LastExecutionDate ?? recurrence.StartDate;
        var nextDate = recurrence.Pattern switch
        {
            RecurrencePattern.Daily => (DateTime?)baseDate.AddDays(recurrence.Interval),
            RecurrencePattern.Weekly => (DateTime?)baseDate.AddDays(7 * recurrence.Interval),
            RecurrencePattern.Monthly => (DateTime?)baseDate.AddMonths(recurrence.Interval),
            RecurrencePattern.Quarterly => (DateTime?)baseDate.AddMonths(3 * recurrence.Interval),
            RecurrencePattern.Yearly => (DateTime?)baseDate.AddYears(recurrence.Interval),
            RecurrencePattern.Custom => null, // Custom patterns need special handling
            _ => null
        };

        // Apply lead time
        if (nextDate.HasValue && recurrence.LeadTimeDays > 0)
        {
            nextDate = nextDate.Value.AddDays(-recurrence.LeadTimeDays);
        }

        // Check end date
        if (nextDate.HasValue && recurrence.EndDate.HasValue && nextDate > recurrence.EndDate)
        {
            return null;
        }

        return nextDate;
    }
}