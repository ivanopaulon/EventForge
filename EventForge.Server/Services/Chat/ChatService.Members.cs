using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Adds new members to a chat — upserts ChatMember entities in the database.
    /// </summary>
    public async Task<MemberOperationResultDto> AddMembersAsync(
        Guid chatId,
        List<Guid> userIds,
        Guid addedBy,
        ChatMemberRole defaultRole = ChatMemberRole.Member,
        string? welcomeMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load existing members for this chat in a single query
            var existingMemberIds = await context.ChatMembers
                .AsNoTracking()
                .Where(cm => cm.ChatThreadId == chatId && !cm.IsDeleted)
                .Select(cm => cm.UserId)
                .ToListAsync(cancellationToken);

            var thread = await context.ChatThreads
                .AsNoTracking()
                .Where(ct => ct.Id == chatId)
                .Select(ct => new { ct.TenantId })
                .FirstOrDefaultAsync(cancellationToken);

            var results = new List<MemberOperationDetail>();
            var now = DateTime.UtcNow;

            foreach (var userId in userIds)
            {
                try
                {
                    if (existingMemberIds.Contains(userId))
                    {
                        // Already a member — treat as success (idempotent)
                        results.Add(new MemberOperationDetail { UserId = userId, Success = true, AssignedRole = defaultRole });
                        continue;
                    }

                    context.ChatMembers.Add(new Data.Entities.Chat.ChatMember
                    {
                        Id = Guid.NewGuid(),
                        ChatThreadId = chatId,
                        UserId = userId,
                        Role = defaultRole,
                        JoinedAt = now,
                        TenantId = thread?.TenantId ?? Guid.Empty,
                        CreatedAt = now,
                        ModifiedAt = now,
                        CreatedBy = addedBy.ToString(),
                        IsActive = true
                    });

                    results.Add(new MemberOperationDetail { UserId = userId, Success = true, AssignedRole = defaultRole });
                }
                catch (Exception ex)
                {
                    results.Add(new MemberOperationDetail { UserId = userId, Success = false, ErrorMessage = ex.Message });
                    logger.LogWarning(ex, "Failed to prepare member add for user {UserId} in chat {ChatId}.", userId, chatId);
                }
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            var successCount = results.Count(r => r.Success);
            logger.LogInformation("User {AddedBy} added {Count} member(s) to chat {ChatId}.", addedBy, successCount, chatId);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMember",
                entityId: chatId,
                propertyName: "AddMembers",
                operationType: "Insert",
                oldValue: null,
                newValue: $"Added {successCount} member(s) with role {defaultRole}",
                changedBy: addedBy.ToString(),
                entityDisplayName: $"Chat Members: {chatId}",
                cancellationToken: cancellationToken);

            await hubContext.Clients.Group($"chat_{chatId}")
                .SendAsync("MembersAdded", new { ChatId = chatId, AddedBy = addedBy, UserIds = results.Where(r => r.Success).Select(r => r.UserId) }, cancellationToken);

            return new MemberOperationResultDto
            {
                ChatId = chatId,
                TotalCount = userIds.Count,
                SuccessCount = successCount,
                FailureCount = userIds.Count - successCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add members to chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Removes members from a chat — soft-deletes ChatMember entities in the database.
    /// </summary>
    public async Task<MemberOperationResultDto> RemoveMembersAsync(
        Guid chatId,
        List<Guid> userIds,
        Guid removedBy,
        string? reason = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await context.ChatMembers
                .Where(cm => cm.ChatThreadId == chatId && userIds.Contains(cm.UserId) && (tenantId == null || cm.TenantId == tenantId.Value) && !cm.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var results = new List<MemberOperationDetail>();

            foreach (var userId in userIds)
            {
                var member = members.FirstOrDefault(m => m.UserId == userId);
                if (member is null)
                {
                    results.Add(new MemberOperationDetail { UserId = userId, Success = false, ErrorMessage = "Member not found." });
                    continue;
                }

                member.IsDeleted = true;
                member.IsActive = false;
                member.ModifiedAt = now;
                member.ModifiedBy = removedBy.ToString();
                results.Add(new MemberOperationDetail { UserId = userId, Success = true });
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            var successCount = results.Count(r => r.Success);
            logger.LogInformation("User {RemovedBy} removed {Count} member(s) from chat {ChatId}.", removedBy, successCount, chatId);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMember",
                entityId: chatId,
                propertyName: "RemoveMembers",
                operationType: "Delete",
                oldValue: $"Removed: {string.Join(", ", userIds)}",
                newValue: null,
                changedBy: removedBy.ToString(),
                entityDisplayName: $"Chat Members: {chatId}",
                cancellationToken: cancellationToken);

            await hubContext.Clients.Group($"chat_{chatId}")
                .SendAsync("MembersRemoved", new { ChatId = chatId, RemovedBy = removedBy, UserIds = results.Where(r => r.Success).Select(r => r.UserId), Reason = reason }, cancellationToken);

            return new MemberOperationResultDto
            {
                ChatId = chatId,
                TotalCount = userIds.Count,
                SuccessCount = successCount,
                FailureCount = userIds.Count - successCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove members from chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Updates member roles — updates ChatMember.Role in the database.
    /// </summary>
    public async Task<MemberOperationResultDto> UpdateMemberRolesAsync(
        Guid chatId,
        Dictionary<Guid, ChatMemberRole> roleUpdates,
        Guid updatedBy,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIds = roleUpdates.Keys.ToList();
            var members = await context.ChatMembers
                .Where(cm => cm.ChatThreadId == chatId && userIds.Contains(cm.UserId) && (tenantId == null || cm.TenantId == tenantId.Value) && !cm.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var results = new List<MemberOperationDetail>();

            foreach (var (userId, newRole) in roleUpdates)
            {
                var member = members.FirstOrDefault(m => m.UserId == userId);
                if (member is null)
                {
                    results.Add(new MemberOperationDetail { UserId = userId, Success = false, ErrorMessage = "Member not found." });
                    continue;
                }

                member.Role = newRole;
                member.ModifiedAt = now;
                member.ModifiedBy = updatedBy.ToString();
                results.Add(new MemberOperationDetail { UserId = userId, Success = true, AssignedRole = newRole });
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            var successCount = results.Count(r => r.Success);
            logger.LogInformation("User {UpdatedBy} updated roles for {Count} member(s) in chat {ChatId}.", updatedBy, successCount, chatId);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMember",
                entityId: chatId,
                propertyName: "UpdateRoles",
                operationType: "Update",
                oldValue: null,
                newValue: $"Updated roles for {successCount} member(s)",
                changedBy: updatedBy.ToString(),
                entityDisplayName: $"Chat Member Roles: {chatId}",
                cancellationToken: cancellationToken);

            await hubContext.Clients.Group($"chat_{chatId}")
                .SendAsync("MemberRolesUpdated", new { ChatId = chatId, UpdatedBy = updatedBy, RoleUpdates = results.Where(r => r.Success).Select(r => new { r.UserId, r.AssignedRole }) }, cancellationToken);

            return new MemberOperationResultDto
            {
                ChatId = chatId,
                TotalCount = roleUpdates.Count,
                SuccessCount = successCount,
                FailureCount = roleUpdates.Count - successCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update member roles in chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive member information for a chat.
    /// </summary>
    public async Task<List<ChatMemberDto>> GetChatMembersAsync(
        Guid chatId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {

        // Verify the requesting user is a member (unless it is a server-side call)
        if (requestingUserId != Guid.Empty)
        {
            var isMember = await context.ChatMembers
                .AsNoTracking()
                .AnyAsync(cm => cm.ChatThreadId == chatId
                             && cm.UserId == requestingUserId
                             && !cm.IsDeleted, cancellationToken);

            if (!isMember)
                return [];
        }

        return await BuildMemberDtosAsync(chatId, cancellationToken);
    }

    /// <summary>
    /// Returns all active users in the tenant for new-chat recipient selection.
    /// </summary>
    public async Task<List<ChatAvailableUserDto>> GetAvailableUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {

        var users = await context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.TenantId == tenantId)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new
            {
                u.Id,
                u.Username,
                FullName = (u.FirstName + " " + u.LastName).Trim()
            })
            .ToListAsync(cancellationToken);

        return users.Select(u => new ChatAvailableUserDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.FullName.Length > 0 ? u.FullName : u.Username,
            IsOnline = onlineUserTracker.IsOnline(u.Id)
        }).ToList();
    }

}
