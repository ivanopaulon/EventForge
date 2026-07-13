using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Returns a paginated activity feed for the user combining their notification history
    /// and entity change audit events scoped to the tenant.
    /// </summary>
    public async Task<PagedResult<ActivityFeedEntryDto>> GetActivityFeedAsync(
        Guid userId,
        Guid? tenantId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Notification-based entries via Recipients table
            var notifQuery =
                from r in context.NotificationRecipients.AsNoTracking()
                join n in context.Notifications.AsNoTracking() on r.NotificationId equals n.Id
                where r.UserId == userId && !n.IsDeleted
                select new { n.Id, n.TenantId, n.Title, n.Message, n.CreatedAt, StatusStr = n.Status.ToString(), PriorityStr = n.Priority.ToString(), TypeStr = n.Type.ToString() };

            if (tenantId.HasValue)
                notifQuery = notifQuery.Where(x => x.TenantId == tenantId.Value);

            var rawNotifEntries = await notifQuery
                .OrderByDescending(x => x.CreatedAt)
                .Take(pagination.PageSize * pagination.Page * 2)
                .ToListAsync(cancellationToken);

            var notifEntries = rawNotifEntries.Select(x => new ActivityFeedEntryDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                UserId = userId,
                ActivityType = "notification",
                Action = x.StatusStr,
                Title = x.Title,
                Description = x.Message,
                CreatedAt = x.CreatedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["priority"] = x.PriorityStr,
                    ["type"] = x.TypeStr
                }
            }).ToList();

            // Audit-log-based entries for the user's own actions
            var auditQuery = context.EntityChangeLogs
                .AsNoTracking()
                .Where(e => e.ChangedBy == userId.ToString());

            if (tenantId.HasValue)
                auditQuery = auditQuery.Where(e => e.TenantId == tenantId.Value);

            var rawAuditEntries = await auditQuery
                .OrderByDescending(e => e.ChangedAt)
                .Take(pagination.PageSize * pagination.Page * 2)
                .Select(e => new { e.Id, e.TenantId, e.OperationType, e.EntityDisplayName, e.EntityName, e.PropertyName, e.OldValue, e.NewValue, e.ChangedAt, e.EntityId })
                .ToListAsync(cancellationToken);

            var auditEntries = rawAuditEntries.Select(e => new ActivityFeedEntryDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                UserId = userId,
                ActivityType = "audit",
                Action = e.OperationType,
                Title = e.EntityDisplayName ?? e.EntityName,
                Description = $"{e.PropertyName}: {e.OldValue ?? "—"} → {e.NewValue ?? "—"}",
                CreatedAt = e.ChangedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["entityName"] = e.EntityName,
                    ["entityId"] = e.EntityId.ToString()
                }
            }).ToList();

            // Merge, sort, and paginate in memory
            var merged = notifEntries
                .Concat(auditEntries)
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            var totalCount = notifEntries.Count + auditEntries.Count;

            return new PagedResult<ActivityFeedEntryDto>
            {
                Items = merged,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build activity feed for user {UserId}.", userId);
            throw;
        }
    }

}
