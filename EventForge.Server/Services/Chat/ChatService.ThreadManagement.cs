using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Creates a new chat thread with comprehensive validation and member setup.
    /// Implements complete database persistence and member management.
    /// </summary>
    public async Task<ChatResponseDto> CreateChatAsync(
        CreateChatDto createChatDto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate tenant access and rate limits
            await ValidateTenantAccessAsync(createChatDto.TenantId, cancellationToken);
            await ValidateChatRateLimitAsync(createChatDto.TenantId, createChatDto.CreatedBy, ChatOperationType.CreateChat, cancellationToken);

            // For DirectMessage: return the existing chat if one already exists between these two users
            if (createChatDto.Type == ChatType.DirectMessage && createChatDto.ParticipantIds.Count > 0)
            {
                var otherUserId = createChatDto.ParticipantIds.FirstOrDefault(id => id != createChatDto.CreatedBy);
                if (otherUserId != Guid.Empty)
                {
                    var existing = await context.ChatThreads
                        .AsNoTracking()
                        .Where(ct => ct.TenantId == createChatDto.TenantId
                                  && ct.Type == ChatType.DirectMessage
                                  && ct.IsActive
                                  && ct.Members.Any(m => m.UserId == createChatDto.CreatedBy && m.IsActive)
                                  && ct.Members.Any(m => m.UserId == otherUserId && m.IsActive))
                        .OrderByDescending(ct => ct.UpdatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existing != null)
                    {
                        var existingDto = await GetChatByIdAsync(existing.Id, createChatDto.CreatedBy, createChatDto.TenantId, cancellationToken);
                        if (existingDto != null) return existingDto;
                    }
                }
            }

            // Generate chat ID and prepare entity
            var chatId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Generate chat name if not provided (for DMs)
            var chatName = createChatDto.Name;
            if (string.IsNullOrEmpty(chatName) && createChatDto.Type == ChatType.DirectMessage)
            {
                chatName = $"DM_{createChatDto.CreatedBy}_{createChatDto.ParticipantIds.FirstOrDefault()}";
            }

            // Create chat thread entity
            var chatThread = new Data.Entities.Chat.ChatThread
            {
                Id = chatId,
                TenantId = createChatDto.TenantId,
                Type = createChatDto.Type,
                Name = chatName ?? "New Chat",
                Description = createChatDto.Description,
                IsPrivate = createChatDto.IsPrivate,
                PreferredLocale = createChatDto.PreferredLocale,
                CreatedBy = createChatDto.CreatedBy.ToString(), // Convert to string
                CreatedAt = now,
                ModifiedAt = now,
                IsActive = true,
                UpdatedAt = now
            };

            // Create chat members
            var members = new List<Data.Entities.Chat.ChatMember>();

            // Add creator as owner
            members.Add(new Data.Entities.Chat.ChatMember
            {
                Id = Guid.NewGuid(),
                ChatThreadId = chatId,
                UserId = createChatDto.CreatedBy,
                TenantId = createChatDto.TenantId,
                Role = ChatMemberRole.Owner,
                JoinedAt = now,
                CreatedAt = now,
                ModifiedAt = now,
                IsActive = true
            });

            // Add other participants as members
            foreach (var participantId in createChatDto.ParticipantIds.Where(id => id != createChatDto.CreatedBy))
            {
                members.Add(new Data.Entities.Chat.ChatMember
                {
                    Id = Guid.NewGuid(),
                    ChatThreadId = chatId,
                    UserId = participantId,
                    TenantId = createChatDto.TenantId,
                    Role = ChatMemberRole.Member,
                    JoinedAt = now,
                    CreatedAt = now,
                    ModifiedAt = now,
                    IsActive = true
                });
            }

            // Save to database
            _ = context.ChatThreads.Add(chatThread);
            context.ChatMembers.AddRange(members);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail for chat creation
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatThread",
                entityId: chatId,
                propertyName: "Create",
                operationType: "Insert",
                oldValue: null,
                newValue: $"Type: {createChatDto.Type}, Name: {chatName}, Members: {createChatDto.ParticipantIds.Count}",
                changedBy: createChatDto.CreatedBy.ToString(),
                entityDisplayName: $"Chat: {chatName}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Chat {ChatId} created by user {UserId} for tenant {TenantId} with {MemberCount} participants in {ElapsedMs}ms",
                chatId, createChatDto.CreatedBy, createChatDto.TenantId, createChatDto.ParticipantIds.Count, stopwatch.ElapsedMilliseconds);

            // Resolve user display names from the database
            var memberUserIds = members.Select(m => m.UserId).ToList();
            var memberUsers = await context.Users
                .AsNoTracking()
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, FullName = u.FirstName + " " + u.LastName })
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            // Create member DTOs
            var memberDtos = members.Select(member =>
            {
                memberUsers.TryGetValue(member.UserId, out var userInfo);
                return new ChatMemberDto
                {
                    UserId = member.UserId,
                    Username = userInfo?.Username ?? member.UserId.ToString("N"),
                    DisplayName = userInfo?.FullName?.Trim() is { Length: > 0 } fn
                        ? fn
                        : userInfo?.Username ?? member.UserId.ToString("N"),
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    IsOnline = onlineUserTracker.IsOnline(member.UserId),
                    IsMuted = false
                };
            }).ToList();

            // Resolve creator display name
            memberUsers.TryGetValue(createChatDto.CreatedBy, out var creatorInfo);
            var createdByName = creatorInfo?.FullName?.Trim() is { Length: > 0 } n
                ? n
                : creatorInfo?.Username ?? "System";

            // Create response DTO
            var responseDto = new ChatResponseDto
            {
                Id = chatId,
                TenantId = createChatDto.TenantId,
                Type = createChatDto.Type,
                Name = chatName,
                Description = createChatDto.Description,
                IsPrivate = createChatDto.IsPrivate,
                PreferredLocale = createChatDto.PreferredLocale,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = createChatDto.CreatedBy,
                CreatedByName = createdByName,
                Members = memberDtos,
                UnreadCount = 0,
                IsActive = true
            };

            // Send real-time notifications to all participants via SignalR
            foreach (var participantId in createChatDto.ParticipantIds)
            {
                await hubContext.Clients.Group($"user_{participantId}")
                    .SendAsync("ChatCreated", responseDto);
            }

            // Return response DTO
            return responseDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create chat for tenant {TenantId}", createChatDto.TenantId);
            throw new InvalidOperationException("Failed to create chat", ex);
        }
    }

    /// <summary>
    /// Gets detailed chat information with access validation.
    /// </summary>
    public async Task<ChatResponseDto?> GetChatByIdAsync(
        Guid chatId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {

        var thread = await context.ChatThreads
            .AsNoTracking()
            .Where(ct => ct.Id == chatId
                      && !ct.IsDeleted
                      && ct.IsActive
                      && (tenantId == null || ct.TenantId == tenantId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (thread is null)
            return null;

        // Verify the requesting user is a member (skip check when userId is empty – server-side call).
        // WhatsApp chats are tenant-scoped external conversations with no ChatMember records;
        // access is already enforced by the tenant filter above, so no member check is needed.
        if (userId != Guid.Empty && thread.Type != ChatType.WhatsApp)
        {
            var isMember = await context.ChatMembers
                .AsNoTracking()
                .AnyAsync(cm => cm.ChatThreadId == chatId
                             && cm.UserId == userId
                             && !cm.IsDeleted, cancellationToken);

            if (!isMember)
                return null;
        }

        var members = await BuildMemberDtosAsync(chatId, cancellationToken);

        var lastMessage = await context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatThreadId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Join(context.Users,
                m => m.SenderId,
                u => u.Id,
                (m, u) => new
                {
                    m.Id,
                    m.ChatThreadId,
                    m.SenderId,
                    SenderFullName = (u.FirstName + " " + u.LastName).Trim(),
                    u.Username,
                    m.Content,
                    m.SentAt
                })
            .Select(x => new ChatMessageDto
            {
                Id = x.Id,
                ChatId = x.ChatThreadId,
                SenderId = x.SenderId ?? Guid.Empty,
                SenderName = x.SenderFullName.Length > 0 ? x.SenderFullName : x.Username,
                Content = x.Content,
                SentAt = x.SentAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var unreadCount = userId != Guid.Empty
            ? await context.ChatMessages
                .AsNoTracking()
                .CountAsync(m => m.ChatThreadId == chatId
                              && !m.IsDeleted
                              && m.SenderId != userId
                              && !context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.UserId == userId),
                             cancellationToken)
            : 0;

        return MapToChatResponseDto(thread, members, lastMessage, unreadCount);
    }

    /// <summary>
    /// Searches and filters user's chats with advanced criteria.
    /// </summary>
    public async Task<PagedResult<ChatResponseDto>> SearchChatsAsync(
        ChatSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        // Base query: threads where the user is an active member
        var query = context.ChatThreads
            .AsNoTracking()
            .Where(ct => !ct.IsDeleted && ct.IsActive);

        if (searchDto.TenantId.HasValue)
            query = query.Where(ct => ct.TenantId == searchDto.TenantId.Value);

        if (searchDto.UserId.HasValue)
            query = query.Where(ct =>
                // WhatsApp chats are tenant-scoped with no ChatMember records; any tenant user may access them.
                ct.Type == ChatType.WhatsApp
                || context.ChatMembers.Any(cm =>
                    cm.ChatThreadId == ct.Id
                    && cm.UserId == searchDto.UserId.Value
                    && !cm.IsDeleted));

        if (searchDto.Types?.Count > 0)
            query = query.Where(ct => searchDto.Types.Contains(ct.Type));

        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            query = query.Where(ct => ct.Name != null && ct.Name.Contains(searchDto.SearchTerm));

        if (searchDto.LastActivityAfter.HasValue)
            query = query.Where(ct => ct.UpdatedAt >= searchDto.LastActivityAfter.Value);

        if (searchDto.LastActivityBefore.HasValue)
            query = query.Where(ct => ct.UpdatedAt <= searchDto.LastActivityBefore.Value);

        // Unread filter – at least one message not read by the requesting user
        if (searchDto.HasUnreadMessages == true && searchDto.UserId.HasValue)
        {
            var uid = searchDto.UserId.Value;
            query = query.Where(ct =>
                context.ChatMessages.Any(m =>
                    m.ChatThreadId == ct.Id
                    && !m.IsDeleted
                    && m.SenderId != uid
                    && !context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.UserId == uid)));
        }

        // Sorting
        query = searchDto.SortBy?.ToLower() switch
        {
            "createdat" => searchDto.SortOrder == "asc"
                ? query.OrderBy(ct => ct.CreatedAt)
                : query.OrderByDescending(ct => ct.CreatedAt),
            _ => searchDto.SortOrder == "asc"
                ? query.OrderBy(ct => ct.UpdatedAt)
                : query.OrderByDescending(ct => ct.UpdatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, searchDto.PageNumber);
        var pageSize = Math.Clamp(searchDto.PageSize, 1, 100);

        var threads = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (threads.Count == 0)
        {
            return new PagedResult<ChatResponseDto>
            {
                Items = [],
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        var threadIds = threads.Select(t => t.Id).ToList();

        // ── Batch load members for all threads in one query ──────────────────
        var rawMembers = await context.ChatMembers
            .AsNoTracking()
            .Where(cm => threadIds.Contains(cm.ChatThreadId) && !cm.IsDeleted)
            .Join(context.Users,
                cm => cm.UserId,
                u => u.Id,
                (cm, u) => new
                {
                    cm.ChatThreadId,
                    cm.UserId,
                    u.Username,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    cm.Role,
                    cm.JoinedAt,
                    cm.LastSeenAt,
                    cm.IsMuted
                })
            .ToListAsync(cancellationToken);

        var membersByThread = rawMembers
            .GroupBy(m => m.ChatThreadId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new ChatMemberDto
                {
                    UserId = m.UserId,
                    Username = m.Username,
                    DisplayName = m.FullName.Length > 0 ? m.FullName : m.Username,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    LastSeenAt = m.LastSeenAt,
                    IsOnline = onlineUserTracker.IsOnline(m.UserId),
                    IsMuted = m.IsMuted
                }).ToList());

        // ── Batch load last message per thread ───────────────────────────────
        // Load all messages for these threads (ordered), group in memory
        var allMessages = await context.ChatMessages
            .AsNoTracking()
            .Where(m => threadIds.Contains(m.ChatThreadId) && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Join(context.Users,
                m => m.SenderId,
                u => u.Id,
                (m, u) => new
                {
                    m.ChatThreadId,
                    m.Id,
                    m.SenderId,
                    SenderName = (u.FirstName + " " + u.LastName).Trim().Length > 0
                        ? (u.FirstName + " " + u.LastName).Trim()
                        : u.Username,
                    m.Content,
                    m.SentAt
                })
            .ToListAsync(cancellationToken);

        var lastMessageByThread = allMessages
            .GroupBy(m => m.ChatThreadId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.First();
                    return new ChatMessageDto
                    {
                        Id = first.Id,
                        ChatId = first.ChatThreadId,
                        SenderId = first.SenderId ?? Guid.Empty,
                        SenderName = first.SenderName,
                        Content = first.Content,
                        SentAt = first.SentAt
                    };
                });

        // ── Batch load unread counts for all threads ─────────────────────────
        var unreadByThread = new Dictionary<Guid, int>();
        if (searchDto.UserId.HasValue)
        {
            var uid = searchDto.UserId.Value;
            var unreadCounts = await context.ChatMessages
                .AsNoTracking()
                .Where(m => threadIds.Contains(m.ChatThreadId)
                          && !m.IsDeleted
                          && m.SenderId != uid)
                .Select(m => new
                {
                    m.ChatThreadId,
                    IsRead = context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.UserId == uid)
                })
                .GroupBy(m => m.ChatThreadId)
                .Select(g => new { ChatId = g.Key, UnreadCount = g.Count(m => !m.IsRead) })
                .ToListAsync(cancellationToken);

            unreadByThread = unreadCounts.ToDictionary(x => x.ChatId, x => x.UnreadCount);
        }

        // ── Assemble result DTOs ─────────────────────────────────────────────
        var items = threads.Select(thread => MapToChatResponseDto(
            thread,
            membersByThread.GetValueOrDefault(thread.Id, []),
            lastMessageByThread.GetValueOrDefault(thread.Id),
            unreadByThread.GetValueOrDefault(thread.Id, 0)
        )).ToList();

        return new PagedResult<ChatResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Updates chat properties with validation and notification.
    /// Updates chat properties in the database with SignalR notification.
    /// </summary>
    public async Task<ChatResponseDto> UpdateChatAsync(
        Guid chatId,
        UpdateChatDto updateDto,
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await context.ChatThreads
                .Where(ct => ct.Id == chatId && (tenantId == null || ct.TenantId == tenantId.Value) && !ct.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Chat {chatId} not found.");

            var oldName = thread.Name;
            var now = DateTime.UtcNow;

            if (updateDto.Name is not null) thread.Name = updateDto.Name;
            if (updateDto.Description is not null) thread.Description = updateDto.Description;
            if (updateDto.IsPrivate.HasValue) thread.IsPrivate = updateDto.IsPrivate.Value;
            if (updateDto.PreferredLocale is not null) thread.PreferredLocale = updateDto.PreferredLocale;
            thread.UpdatedAt = now;
            thread.ModifiedAt = now;
            thread.ModifiedBy = userId.ToString();

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatThread",
                entityId: chatId,
                propertyName: "Update",
                operationType: "Update",
                oldValue: $"Name: {oldName}",
                newValue: $"Name: {thread.Name}, Description: {thread.Description}",
                changedBy: userId.ToString(),
                entityDisplayName: $"Chat: {thread.Name}",
                cancellationToken: cancellationToken);

            logger.LogInformation("Chat {ChatId} updated by user {UserId}.", chatId, userId);

            var members = await BuildMemberDtosAsync(chatId, cancellationToken);
            var responseDto = MapToChatResponseDto(thread, members, null, 0);

            await hubContext.Clients.Group($"chat_{chatId}")
                .SendAsync("ChatUpdated", responseDto, cancellationToken);

            return responseDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update chat {ChatId} by user {UserId}.", chatId, userId);
            throw;
        }
    }

    /// <summary>
    /// Archives or deletes a chat with cleanup.
    /// Archives or deletes a chat with database persistence and SignalR notification.
    /// </summary>
    public async Task<ChatOperationResultDto> DeleteChatAsync(
        Guid chatId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await context.ChatThreads
                .Where(ct => ct.Id == chatId && (tenantId == null || ct.TenantId == tenantId.Value) && !ct.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Chat {chatId} not found.");

            var now = DateTime.UtcNow;

            if (softDelete)
            {
                thread.IsDeleted = true;
                thread.IsActive = false;
                thread.ModifiedAt = now;
                thread.ModifiedBy = userId.ToString();
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                context.ChatThreads.Remove(thread);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatThread",
                entityId: chatId,
                propertyName: softDelete ? "SoftDelete" : "HardDelete",
                operationType: "Delete",
                oldValue: "Active",
                newValue: softDelete ? "Deleted" : "Permanently Deleted",
                changedBy: userId.ToString(),
                entityDisplayName: $"Chat Deletion: {chatId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Chat {ChatId} {DeleteType} by user {UserId}.",
                chatId, softDelete ? "soft-deleted" : "hard-deleted", userId);

            await hubContext.Clients.Group($"chat_{chatId}")
                .SendAsync("ChatDeleted", new { ChatId = chatId, DeletedBy = userId, Reason = reason, SoftDelete = softDelete }, cancellationToken);

            return new ChatOperationResultDto
            {
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["ChatId"] = chatId,
                    ["DeletedBy"] = userId,
                    ["SoftDelete"] = softDelete,
                    ["Reason"] = reason ?? "No reason provided"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete chat {ChatId} by user {UserId}.", chatId, userId);
            throw;
        }
    }

}
