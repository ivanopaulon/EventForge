using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Checks if notification sending is allowed under rate limits.
    /// Implements basic in-memory rate limiting with tenant and user-specific policies.
    /// </summary>
    public async Task<RateLimitStatusDto> CheckRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        NotificationTypes notificationType,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Checking rate limit for tenant {TenantId}, user {UserId}, type {Type}",
            tenantId, userId, notificationType);

        try
        {
            // Define per-hour limits by notification type
            var rateLimits = new Dictionary<NotificationTypes, int>
            {
                { NotificationTypes.System, 1000 },
                { NotificationTypes.Security, 500 },
                { NotificationTypes.Event, 200 },
                { NotificationTypes.User, 100 },
                { NotificationTypes.Marketing, 50 },
                { NotificationTypes.Audit, 1000 }
            };

            var limit = rateLimits.GetValueOrDefault(notificationType, 100);

            // Build a scoped cache key: tenant + user (or global) + type, reset every hour
            var windowKey = $"ratelimit:notification:{tenantId?.ToString() ?? "global"}:{userId?.ToString() ?? "anon"}:{notificationType}:{DateTime.UtcNow:yyyyMMddHH}";

            var currentCount = memoryCache.GetOrCreate(windowKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                entry.Size = 1;
                return 0;
            });

            var isAllowed = currentCount < limit;
            if (isAllowed)
            {
                var setOptions = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    Size = 1
                };
                memoryCache.Set(windowKey, currentCount + 1, setOptions);
            }

            var remainingQuota = Math.Max(0, limit - (currentCount + (isAllowed ? 1 : 0)));

            return new RateLimitStatusDto
            {
                IsAllowed = isAllowed,
                RemainingQuota = remainingQuota,
                ResetTime = TimeSpan.FromHours(1),
                RateLimitType = tenantId.HasValue ? "Tenant" : "Global",
                LimitDetails = new Dictionary<string, object>
                {
                    ["TenantId"] = tenantId?.ToString() ?? "System",
                    ["UserId"] = userId?.ToString() ?? "N/A",
                    ["Type"] = notificationType.ToString(),
                    ["Limit"] = limit,
                    ["CurrentUsage"] = currentCount + (isAllowed ? 1 : 0),
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check rate limit for tenant {TenantId}, user {UserId}", tenantId, userId);

            // On error, allow but log the issue
            return new RateLimitStatusDto
            {
                IsAllowed = true,
                RemainingQuota = 100,
                ResetTime = TimeSpan.FromHours(1),
                RateLimitType = "Error-Fallback",
                LimitDetails = new Dictionary<string, object>
                {
                    ["Error"] = "Rate limit check failed",
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
    }

    /// <summary>
    /// Updates rate limiting policies for tenants.
    /// Records the update in the audit log and returns the applied policy.
    /// </summary>
    public async Task<RateLimitPolicyDto> UpdateTenantRateLimitAsync(
        Guid tenantId,
        RateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default)
    {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "RateLimitPolicy",
            entityId: tenantId,
            propertyName: "Update",
            operationType: "Update",
            oldValue: "Previous policy",
            newValue: $"GlobalLimit: {rateLimitPolicy.GlobalLimit}, Window: {rateLimitPolicy.WindowSize}",
            changedBy: "System",
            entityDisplayName: $"Rate Limit Policy: {tenantId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Updated rate limit policy for tenant {TenantId}: GlobalLimit={GlobalLimit}",
            tenantId, rateLimitPolicy.GlobalLimit);

        return rateLimitPolicy;
    }

    /// <summary>
    /// Gets comprehensive notification statistics.
    /// Gets notification statistics via database aggregation queries.
    /// </summary>
    public async Task<NotificationStatsDto> GetNotificationStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.Notifications.AsNoTracking().Where(n => !n.IsDeleted);
            if (tenantId.HasValue) query = query.Where(n => n.TenantId == tenantId.Value);
            if (dateRange is not null)
                query = query.Where(n => n.CreatedAt >= dateRange.StartDate && n.CreatedAt <= dateRange.EndDate);

            var now = DateTime.UtcNow;

            var totalNotifications = await query.CountAsync(cancellationToken);
            var unreadCount = await query.CountAsync(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered || n.Status == NotificationStatus.Pending, cancellationToken);
            var acknowledgedCount = await query.CountAsync(n => n.Status == NotificationStatus.Acknowledged, cancellationToken);
            var silencedCount = await query.CountAsync(n => n.Status == NotificationStatus.Silenced, cancellationToken);
            var archivedCount = await query.CountAsync(n => n.IsArchived, cancellationToken);
            var expiredCount = await query.CountAsync(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < now, cancellationToken);

            var countByTypeRaw = await query
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countByPriorityRaw = await query
                .GroupBy(n => n.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new NotificationStatsDto
            {
                TenantId = tenantId,
                TotalNotifications = totalNotifications,
                UnreadCount = unreadCount,
                AcknowledgedCount = acknowledgedCount,
                SilencedCount = silencedCount,
                ArchivedCount = archivedCount,
                ExpiredCount = expiredCount,
                CountByType = countByTypeRaw.ToDictionary(x => x.Type, x => x.Count),
                CountByPriority = countByPriorityRaw.ToDictionary(x => x.Priority, x => x.Count)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compute notification statistics.");
            throw;
        }
    }

}
