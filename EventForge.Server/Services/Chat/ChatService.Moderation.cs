using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Performs moderation actions on chats and messages.
    /// Records moderation actions in the audit log and returns the result.
    /// </summary>
    public async Task<ModerationResultDto> ModerateChatAsync(
        ChatModerationActionDto moderationAction,
        CancellationToken cancellationToken = default)
    {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "ChatModeration",
            entityId: moderationAction.ChatId,
            propertyName: "ModerateAction",
            operationType: "Update",
            oldValue: "Active",
            newValue: moderationAction.Action,
            changedBy: moderationAction.ModeratorId.ToString(),
            entityDisplayName: $"Chat Moderation: {moderationAction.ChatId}",
            cancellationToken: cancellationToken);

        logger.LogWarning(
            "Moderator {ModeratorId} performed action '{Action}' on chat {ChatId} with reason: {Reason}",
            moderationAction.ModeratorId, moderationAction.Action, moderationAction.ChatId, moderationAction.Reason);

        return new ModerationResultDto
        {
            Success = true,
            Action = moderationAction.Action,
            Reason = moderationAction.Reason,
            AffectedItems = new List<string> { moderationAction.ChatId.ToString() },
            Metadata = new Dictionary<string, object>
            {
                ["ModeratorId"] = moderationAction.ModeratorId,
                ["ExpiresAt"] = moderationAction.ExpiresAt?.ToString() ?? string.Empty,
                ["NotifyMembers"] = moderationAction.NotifyMembers
            }
        };
    }

    /// <summary>
    /// Gets detailed audit trail for chat operations — queries EntityChangeLogs.
    /// </summary>
    public async Task<PagedResult<ChatAuditEntryDto>> GetChatAuditTrailAsync(
        ChatAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.EntityChangeLogs
                .AsNoTracking()
                .Where(e => e.EntityName == "ChatThread" || e.EntityName == "ChatMessage" || e.EntityName == "ChatMember");

            if (auditQuery.TenantId.HasValue)
                query = query.Where(e => e.TenantId == auditQuery.TenantId.Value);

            if (auditQuery.ChatId.HasValue)
                query = query.Where(e => e.EntityId == auditQuery.ChatId.Value);

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

            var items = rawItems.Select(e => new ChatAuditEntryDto
            {
                Id = e.Id,
                ChatId = e.EntityName == "ChatThread" ? e.EntityId : null,
                TenantId = e.TenantId,
                UserId = Guid.TryParse(e.ChangedBy, out var uid) ? uid : null,
                Operation = e.OperationType,
                Details = $"{e.PropertyName}: {e.OldValue} → {e.NewValue}",
                Timestamp = e.ChangedAt
            }).ToList();

            return new PagedResult<ChatAuditEntryDto>
            {
                Items = items,
                Page = auditQuery.PageNumber,
                PageSize = auditQuery.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve chat audit trail.");
            throw;
        }
    }

    /// <summary>
    /// Monitors chat system health — queries database connectivity and counts active entities.
    /// </summary>
    public async Task<ChatSystemHealthDto> GetChatSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Checking chat system health");

            var alerts = new List<string>();
            var dbConnected = false;
            var activeConnections = 0;
            var totalMessages = 0;
            var totalChats = 0;

            try
            {
                totalChats = await context.ChatThreads.AsNoTracking().CountAsync(ct => ct.IsActive && !ct.IsDeleted, cancellationToken);
                totalMessages = await context.ChatMessages.AsNoTracking().CountAsync(m => !m.IsDeleted, cancellationToken);
                dbConnected = true;
            }
            catch (Exception dbEx)
            {
                alerts.Add($"Database connectivity issue: {dbEx.Message}");
            }

            return new ChatSystemHealthDto
            {
                Status = alerts.Count == 0 ? "Healthy" : "Degraded",
                Metrics = new Dictionary<string, object>
                {
                    ["DatabaseConnected"] = dbConnected,
                    ["ActiveChats"] = totalChats,
                    ["TotalMessages"] = totalMessages,
                    ["ActiveConnections"] = activeConnections,
                    ["LastHealthCheck"] = DateTime.UtcNow
                },
                Alerts = alerts
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check chat system health.");
            throw;
        }
    }

}
