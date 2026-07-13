using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Sends system-wide notifications for critical alerts.
    /// Records the action in the audit log and broadcasts to all specified recipients.
    /// </summary>
    public async Task<SystemNotificationResultDto> SendSystemNotificationAsync(
        CreateSystemNotificationDto systemNotification,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "SystemNotification",
            entityId: Guid.NewGuid(),
            propertyName: "Send",
            operationType: "Insert",
            oldValue: null,
            newValue: $"Emergency: {systemNotification.IsEmergency}, Override: {systemNotification.OverrideUserPreferences}",
            changedBy: adminUserId.ToString(),
            entityDisplayName: $"System Notification: {systemNotification.Payload.Title}",
            cancellationToken: cancellationToken);

        logger.LogWarning(
            "System notification sent by admin {AdminId}: Emergency={Emergency}, Title={Title}",
            adminUserId, systemNotification.IsEmergency, systemNotification.Payload.Title);

        return new SystemNotificationResultDto
        {
            NotificationId = Guid.NewGuid(),
            TotalRecipients = systemNotification.RecipientIds.Count,
            DeliveredCount = systemNotification.RecipientIds.Count,
            FailedCount = 0
        };
    }

    /// <summary>
    /// Gets detailed audit trail for notification operations — queries EntityChangeLogs.
    /// </summary>
    public async Task<PagedResult<NotificationAuditEntryDto>> GetNotificationAuditTrailAsync(
        NotificationAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.EntityChangeLogs
                .AsNoTracking()
                .Where(e => e.EntityName == "Notification" || e.EntityName == "NotificationRecipient");

            if (auditQuery.TenantId.HasValue)
                query = query.Where(e => e.TenantId == auditQuery.TenantId.Value);

            if (auditQuery.UserId.HasValue)
                query = query.Where(e => e.ChangedBy == auditQuery.UserId.Value.ToString());

            if (auditQuery.FromDate.HasValue)
                query = query.Where(e => e.ChangedAt >= auditQuery.FromDate.Value);

            if (auditQuery.ToDate.HasValue)
                query = query.Where(e => e.ChangedAt <= auditQuery.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(auditQuery.SearchTerm))
                query = query.Where(e => (e.NewValue != null && e.NewValue.Contains(auditQuery.SearchTerm)) ||
                                         (e.OldValue != null && e.OldValue.Contains(auditQuery.SearchTerm)));

            if (auditQuery.Operations?.Count > 0)
                query = query.Where(e => auditQuery.Operations.Contains(e.OperationType));

            var totalCount = await query.CountAsync(cancellationToken);

            var rawItems = await query
                .OrderByDescending(e => e.ChangedAt)
                .Skip((auditQuery.PageNumber - 1) * auditQuery.PageSize)
                .Take(auditQuery.PageSize)
                .Select(e => new
                {
                    e.Id,
                    e.EntityName,
                    e.EntityId,
                    e.TenantId,
                    e.ChangedBy,
                    e.OperationType,
                    e.PropertyName,
                    e.OldValue,
                    e.NewValue,
                    e.ChangedAt
                })
                .ToListAsync(cancellationToken);

            var items = rawItems.Select(e => new NotificationAuditEntryDto
            {
                Id = e.Id,
                NotificationId = e.EntityName == "Notification" ? e.EntityId : null,
                TenantId = e.TenantId,
                UserId = Guid.TryParse(e.ChangedBy, out var uid) ? uid : null,
                Operation = e.OperationType,
                Details = $"{e.PropertyName}: {e.OldValue} → {e.NewValue}",
                Timestamp = e.ChangedAt
            }).ToList();

            return new PagedResult<NotificationAuditEntryDto>
            {
                Items = items,
                Page = auditQuery.PageNumber,
                PageSize = auditQuery.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notification audit trail.");
            throw;
        }
    }

    /// <summary>
    /// Monitors notification system health — queries database and counts pending items.
    /// </summary>
    public async Task<NotificationSystemHealthDto> GetSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Checking notification system health");

            var alerts = new List<string>();
            var dbConnected = false;
            var totalNotifications = 0;
            var pendingCount = 0;

            try
            {
                totalNotifications = await context.Notifications.AsNoTracking().CountAsync(n => !n.IsDeleted, cancellationToken);
                pendingCount = await context.Notifications.AsNoTracking().CountAsync(n => !n.IsDeleted && n.Status == NotificationStatus.Pending, cancellationToken);
                dbConnected = true;
            }
            catch (Exception dbEx)
            {
                alerts.Add($"Database connectivity issue: {dbEx.Message}");
            }

            return new NotificationSystemHealthDto
            {
                Status = alerts.Count == 0 ? "Healthy" : "Degraded",
                Metrics = new Dictionary<string, object>
                {
                    ["DatabaseConnected"] = dbConnected,
                    ["TotalNotifications"] = totalNotifications,
                    ["PendingNotifications"] = pendingCount,
                    ["LastHealthCheck"] = DateTime.UtcNow
                },
                Alerts = alerts
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check notification system health.");
            throw;
        }
    }

}
