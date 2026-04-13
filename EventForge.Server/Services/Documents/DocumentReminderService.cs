using Prym.DTOs.Documents;
using EventForge.Server.Data.Entities.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document reminders.
/// </summary>
public class DocumentReminderService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<DocumentReminderService> logger) : IDocumentReminderService
{
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentReminderDto>> GetDocumentRemindersAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        var reminders = await context.DocumentReminders
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentHeaderId && !r.IsDeleted)
            .OrderBy(r => r.TargetDate)
            .ToListAsync(cancellationToken);

        return reminders.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentReminderDto?> GetDocumentReminderAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var reminder = await context.DocumentReminders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reminderId && !r.IsDeleted, cancellationToken);

        return reminder is not null ? MapToDto(reminder) : null;
    }

    /// <inheritdoc />
    public async Task<DocumentReminderDto> CreateDocumentReminderAsync(
        CreateDocumentReminderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var reminder = new DocumentReminder
        {
            Id = Guid.NewGuid(),
            DocumentHeaderId = createDto.DocumentHeaderId,
            ReminderType = createDto.ReminderType,
            Title = createDto.Title,
            Description = createDto.Description,
            TargetDate = createDto.TargetDate,
            Priority = createDto.Priority,
            Status = ReminderStatus.Active,
            IsRecurring = createDto.IsRecurring,
            RecurrencePattern = createDto.RecurrencePattern,
            RecurrenceInterval = createDto.RecurrenceInterval,
            RecurrenceEndDate = createDto.RecurrenceEndDate,
            LeadTimeHours = createDto.LeadTimeHours,
            EscalationEnabled = createDto.EscalationEnabled,
            NotifyUsers = createDto.NotifyUsers,
            NotifyRoles = createDto.NotifyRoles,
            NotificationMethods = createDto.NotificationMethods,
            NextNotificationAt = createDto.TargetDate.AddHours(-createDto.LeadTimeHours),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            TenantId = tenantContext.CurrentTenantId ?? Guid.Empty
        };

        _ = context.DocumentReminders.Add(reminder);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentReminder", reminder.Id, "CREATE", "CREATE",
            null, $"Created reminder '{reminder.Title}' for document {reminder.DocumentHeaderId}", currentUser);

        logger.LogInformation("Created DocumentReminder {ReminderId} for user {User}", reminder.Id, currentUser);
        return MapToDto(reminder);
    }

    /// <inheritdoc />
    public async Task<DocumentReminderDto?> UpdateDocumentReminderAsync(
        Guid reminderId,
        UpdateDocumentReminderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var reminder = await context.DocumentReminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && !r.IsDeleted, cancellationToken);

        if (reminder is null)
            return null;

        if (updateDto.Title is not null)
            reminder.Title = updateDto.Title;
        if (updateDto.Description is not null)
            reminder.Description = updateDto.Description;
        if (updateDto.TargetDate.HasValue)
        {
            reminder.TargetDate = updateDto.TargetDate.Value;
            reminder.NextNotificationAt = updateDto.TargetDate.Value.AddHours(updateDto.LeadTimeHours ?? reminder.LeadTimeHours);
        }
        if (updateDto.Priority.HasValue)
            reminder.Priority = updateDto.Priority.Value;
        if (updateDto.Status.HasValue)
            reminder.Status = updateDto.Status.Value;
        if (updateDto.LeadTimeHours.HasValue)
            reminder.LeadTimeHours = updateDto.LeadTimeHours.Value;
        if (updateDto.CompletionNotes is not null)
            reminder.CompletionNotes = updateDto.CompletionNotes;

        reminder.ModifiedAt = DateTime.UtcNow;
        reminder.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentReminder", reminder.Id, "UPDATE", "UPDATE",
            null, $"Updated reminder '{reminder.Title}'", currentUser);

        logger.LogInformation("Updated DocumentReminder {ReminderId} for user {User}", reminder.Id, currentUser);
        return MapToDto(reminder);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentReminderAsync(
        Guid reminderId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var reminder = await context.DocumentReminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && !r.IsDeleted, cancellationToken);

        if (reminder is null)
            return false;

        reminder.IsDeleted = true;
        reminder.ModifiedAt = DateTime.UtcNow;
        reminder.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentReminder", reminder.Id, "DELETE", "DELETE",
            null, $"Deleted reminder '{reminder.Title}'", currentUser);

        logger.LogInformation("Deleted DocumentReminder {ReminderId} for user {User}", reminder.Id, currentUser);
        return true;
    }

    /// <inheritdoc />
    public async Task<DocumentReminderDto?> CompleteReminderAsync(
        Guid reminderId,
        string? completionNotes,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var reminder = await context.DocumentReminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && !r.IsDeleted, cancellationToken);

        if (reminder is null)
            return null;

        reminder.Status = ReminderStatus.Completed;
        reminder.CompletedAt = DateTime.UtcNow;
        reminder.CompletedBy = currentUser;
        reminder.CompletionNotes = completionNotes;
        reminder.ModifiedAt = DateTime.UtcNow;
        reminder.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentReminder", reminder.Id, "COMPLETE", "COMPLETE",
            null, $"Completed reminder '{reminder.Title}'", currentUser);

        logger.LogInformation("Completed DocumentReminder {ReminderId} for user {User}", reminder.Id, currentUser);
        return MapToDto(reminder);
    }

    /// <inheritdoc />
    public async Task<DocumentReminderDto?> SnoozeReminderAsync(
        Guid reminderId,
        DateTime newTargetDate,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var reminder = await context.DocumentReminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && !r.IsDeleted, cancellationToken);

        if (reminder is null)
            return null;

        reminder.TargetDate = newTargetDate;
        reminder.NextNotificationAt = newTargetDate.AddHours(-reminder.LeadTimeHours);
        reminder.SnoozeCount++;
        reminder.Status = ReminderStatus.Active;
        reminder.ModifiedAt = DateTime.UtcNow;
        reminder.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentReminder", reminder.Id, "SNOOZE", "SNOOZE",
            null, $"Snoozed reminder '{reminder.Title}' to {newTargetDate:O}", currentUser);

        logger.LogInformation("Snoozed DocumentReminder {ReminderId} to {NewDate} for user {User}", reminder.Id, newTargetDate, currentUser);
        return MapToDto(reminder);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentReminderDto>> GetActiveRemindersAsync(
        DateTime? beforeDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.DocumentReminders
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == ReminderStatus.Active);

        if (beforeDate.HasValue)
            query = query.Where(r => r.TargetDate <= beforeDate.Value);

        var reminders = await query
            .OrderBy(r => r.TargetDate)
            .ToListAsync(cancellationToken);

        return reminders.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentReminderDto>> GetUserRemindersAsync(
        string userId,
        bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = context.DocumentReminders
            .AsNoTracking()
            .Where(r => !r.IsDeleted && (r.CreatedBy == userId || (r.NotifyUsers != null && r.NotifyUsers.Contains(userId))));

        if (!includeCompleted)
            query = query.Where(r => r.Status != ReminderStatus.Completed);

        var reminders = await query
            .OrderBy(r => r.TargetDate)
            .ToListAsync(cancellationToken);

        return reminders.Select(MapToDto);
    }

    private static DocumentReminderDto MapToDto(DocumentReminder reminder)
    {
        return new DocumentReminderDto
        {
            Id = reminder.Id,
            DocumentHeaderId = reminder.DocumentHeaderId,
            ReminderType = reminder.ReminderType,
            Title = reminder.Title,
            Description = reminder.Description,
            TargetDate = reminder.TargetDate,
            Priority = reminder.Priority,
            Status = reminder.Status,
            IsRecurring = reminder.IsRecurring,
            RecurrencePattern = reminder.RecurrencePattern,
            RecurrenceInterval = reminder.RecurrenceInterval,
            RecurrenceEndDate = reminder.RecurrenceEndDate,
            LeadTimeHours = reminder.LeadTimeHours,
            EscalationEnabled = reminder.EscalationEnabled,
            LastNotifiedAt = reminder.LastNotifiedAt,
            NextNotificationAt = reminder.NextNotificationAt,
            NotificationCount = reminder.NotificationCount,
            SnoozeCount = reminder.SnoozeCount,
            CompletedAt = reminder.CompletedAt,
            CompletedBy = reminder.CompletedBy,
            CompletionNotes = reminder.CompletionNotes,
            CreatedAt = reminder.CreatedAt,
            CreatedBy = reminder.CreatedBy
        };
    }
}
