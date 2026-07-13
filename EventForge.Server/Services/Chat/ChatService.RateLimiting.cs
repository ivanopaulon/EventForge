using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Checks if chat operations are allowed under rate limits.
    /// Implements basic in-memory rate limiting with operation-specific policies.
    /// </summary>
    public async Task<ChatRateLimitStatusDto> CheckChatRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        ChatOperationType operationType,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Checking chat rate limit for tenant {TenantId}, user {UserId}, operation {Operation}",
            tenantId, userId, operationType);

        try
        {
            // Define per-hour limits by chat operation type
            var rateLimits = new Dictionary<ChatOperationType, int>
            {
                { ChatOperationType.SendMessage, 1000 },
                { ChatOperationType.CreateChat, 50 },
                { ChatOperationType.UploadFile, 100 },
                { ChatOperationType.EditMessage, 200 },
                { ChatOperationType.DeleteMessage, 100 }
            };

            var limit = rateLimits.GetValueOrDefault(operationType, 100);

            // Build a scoped cache key: tenant + user + operation, reset every hour
            var windowKey = $"ratelimit:chat:{tenantId?.ToString() ?? "global"}:{userId?.ToString() ?? "anon"}:{operationType}:{DateTime.UtcNow:yyyyMMddHH}";

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

            return new ChatRateLimitStatusDto
            {
                IsAllowed = isAllowed,
                RemainingQuota = remainingQuota,
                ResetTime = TimeSpan.FromHours(1),
                OperationType = operationType,
                LimitDetails = new Dictionary<string, object>
                {
                    ["TenantId"] = tenantId?.ToString() ?? "System",
                    ["UserId"] = userId?.ToString() ?? "N/A",
                    ["Operation"] = operationType.ToString(),
                    ["Limit"] = limit,
                    ["CurrentUsage"] = currentCount + (isAllowed ? 1 : 0),
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check chat rate limit for tenant {TenantId}, user {UserId}", tenantId, userId);

            // On error, allow but log the issue
            return new ChatRateLimitStatusDto
            {
                IsAllowed = true,
                RemainingQuota = 100,
                ResetTime = TimeSpan.FromHours(1),
                OperationType = operationType,
                LimitDetails = new Dictionary<string, object>
                {
                    ["Error"] = "Rate limit check failed",
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
    }

    /// <summary>
    /// Updates chat rate limiting policies for tenants.
    /// Persists the policy via audit log and returns the updated policy.
    /// </summary>
    public async Task<ChatRateLimitPolicyDto> UpdateTenantChatRateLimitAsync(
        Guid tenantId,
        ChatRateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default)
    {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "ChatRateLimitPolicy",
            entityId: tenantId,
            propertyName: "Update",
            operationType: "Update",
            oldValue: "Previous policy",
            newValue: $"MessageLimit: {rateLimitPolicy.GlobalMessageLimit}, MaxFileSize: {rateLimitPolicy.MaxFileSize}",
            changedBy: "System",
            entityDisplayName: $"Chat Rate Limit Policy: {tenantId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Updated chat rate limit policy for tenant {TenantId}: MessageLimit={MessageLimit}",
            tenantId, rateLimitPolicy.GlobalMessageLimit);

        return rateLimitPolicy;
    }

    /// <summary>
    /// Gets comprehensive chat statistics via database aggregation queries.
    /// </summary>
    public async Task<ChatStatsDto> GetChatStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var threadQuery = context.ChatThreads.AsNoTracking().Where(ct => !ct.IsDeleted);
            if (tenantId.HasValue) threadQuery = threadQuery.Where(ct => ct.TenantId == tenantId.Value);

            var messageQuery = context.ChatMessages.AsNoTracking().Where(m => !m.IsDeleted);
            if (tenantId.HasValue) messageQuery = messageQuery.Where(m => m.TenantId == tenantId.Value);
            if (dateRange is not null)
            {
                messageQuery = messageQuery.Where(m => m.SentAt >= dateRange.StartDate && m.SentAt <= dateRange.EndDate);
            }

            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var totalChats = await threadQuery.CountAsync(cancellationToken);
            var activeChats = await threadQuery.CountAsync(ct => ct.IsActive, cancellationToken);
            var dmChats = await threadQuery.CountAsync(ct => ct.Type == ChatType.DirectMessage, cancellationToken);
            var groupChats = await threadQuery.CountAsync(ct => ct.Type == ChatType.Group, cancellationToken);
            var totalMessages = await messageQuery.CountAsync(cancellationToken);
            var messagesLastWeek = await messageQuery.CountAsync(m => m.SentAt >= weekAgo, cancellationToken);
            var messagesLastMonth = await messageQuery.CountAsync(m => m.SentAt >= monthAgo, cancellationToken);

            var mediaCountRaw = await context.MessageAttachments
                .AsNoTracking()
                .Where(a => !a.IsDeleted && (tenantId == null || a.TenantId == tenantId.Value))
                .GroupBy(a => a.MediaType)
                .Select(g => new { MediaType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new ChatStatsDto
            {
                TenantId = tenantId,
                TotalChats = totalChats,
                ActiveChats = activeChats,
                DirectMessageChats = dmChats,
                GroupChats = groupChats,
                TotalMessages = totalMessages,
                MessagesLastWeek = messagesLastWeek,
                MessagesLastMonth = messagesLastMonth,
                MediaCountByType = mediaCountRaw.ToDictionary(x => x.MediaType, x => x.Count)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compute chat statistics.");
            throw;
        }
    }

}
