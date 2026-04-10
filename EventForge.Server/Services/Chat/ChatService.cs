using EventForge.DTOs.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.Services.Chat;

/// <summary>
/// Chat service implementation with comprehensive multi-tenant support.
/// 
/// This implementation provides stub methods for all chat functionality
/// while establishing the foundation for future full implementation.
/// 
/// Key architectural patterns:
/// - Multi-tenant data isolation with tenant-aware queries
/// - Comprehensive audit logging for all chat operations
/// - Rate limiting with tenant and user-specific policies
/// - File/media management with security and optimization
/// - Real-time delivery status tracking and read receipts
/// - Localization support with culture-aware content
/// - Extensible design for future enhancements
/// 
/// Future implementation areas:
/// - End-to-end encryption for secure messaging
/// - AI-powered content moderation and translation
/// - Voice and video calling integration
/// - Advanced media processing and analysis
/// - Bot framework and automation capabilities
/// - External chat platform integrations
/// - Advanced search and analytics
/// </summary>
public class ChatService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<ChatService> logger,
    IHubContext<ChatHub> hubContext,
    IOnlineUserTracker onlineUserTracker) : IChatService
{

    #region Chat Thread Management

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
    /// STUB IMPLEMENTATION - Returns null (not found).
    /// </summary>
    public async Task<ChatResponseDto?> GetChatByIdAsync(
        Guid chatId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Retrieving chat {ChatId} for user {UserId} in tenant {TenantId}",
                chatId, userId, tenantId);

            var thread = await context.ChatThreads
                .AsNoTracking()
                .Where(ct => ct.Id == chatId
                          && !ct.IsDeleted
                          && ct.IsActive
                          && (tenantId == null || ct.TenantId == tenantId.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (thread is null)
                return null;

            // Verify the requesting user is a member (skip check when userId is empty – server-side call)
            if (userId != Guid.Empty)
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Searches and filters user's chats with advanced criteria.
    /// STUB IMPLEMENTATION - Returns empty paginated results.
    /// </summary>
    public async Task<PagedResult<ChatResponseDto>> SearchChatsAsync(
        ChatSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
        // Base query: threads where the user is an active member
        var query = context.ChatThreads
            .AsNoTracking()
            .Where(ct => !ct.IsDeleted && ct.IsActive);

        if (searchDto.TenantId.HasValue)
            query = query.Where(ct => ct.TenantId == searchDto.TenantId.Value);

        if (searchDto.UserId.HasValue)
            query = query.Where(ct =>
                context.ChatMembers.Any(cm =>
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SearchChatsAsync.");
            throw;
        }
    }

    /// <summary>
    /// Updates chat properties with validation and notification.
    /// STUB IMPLEMENTATION - Logs update and returns mock response.
    /// </summary>
    public async Task<ChatResponseDto> UpdateChatAsync(
        Guid chatId,
        UpdateChatDto updateDto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "ChatThread",
            entityId: chatId,
            propertyName: "Update",
            operationType: "Update",
            oldValue: "Previous values",
            newValue: $"Name: {updateDto.Name}, Description: {updateDto.Description}",
            changedBy: userId.ToString(),
            entityDisplayName: $"Chat Update: {chatId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} updated chat {ChatId}: Name={Name}",
            userId, chatId, updateDto.Name);

        // Return stub response
        return new ChatResponseDto
        {
            Id = chatId,
            Name = updateDto.Name,
            Description = updateDto.Description,
            IsPrivate = updateDto.IsPrivate ?? true,
            PreferredLocale = updateDto.PreferredLocale,
            UpdatedAt = DateTime.UtcNow
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateChatAsync for chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Archives or deletes a chat with cleanup.
    /// STUB IMPLEMENTATION - Logs deletion and returns success result.
    /// </summary>
    public async Task<ChatOperationResultDto> DeleteChatAsync(
        Guid chatId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
            "User {UserId} {DeleteType} chat {ChatId} with reason: {Reason}",
            userId, softDelete ? "soft deleted" : "hard deleted", chatId, reason ?? "No reason provided");

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
            logger.LogError(ex, "Error in DeleteChatAsync for chat {ChatId}.", chatId);
            throw;
        }
    }

    #endregion

    #region Message Management

    /// <summary>
    /// Sends a message with comprehensive validation and delivery tracking.
    /// Implements complete database persistence and real-time delivery.
    /// </summary>
    public async Task<ChatMessageDto> SendMessageAsync(
        SendMessageDto messageDto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate rate limits
            await ValidateChatRateLimitAsync(null, messageDto.SenderId, ChatOperationType.SendMessage, cancellationToken);

            // Generate message ID and prepare entity
            var messageId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Create message entity
            var message = new Data.Entities.Chat.ChatMessage
            {
                Id = messageId,
                ChatThreadId = messageDto.ChatId,
                SenderId = messageDto.SenderId,
                Content = messageDto.Content,
                ReplyToMessageId = messageDto.ReplyToMessageId,
                Status = MessageStatus.Pending,
                SentAt = now,
                Locale = messageDto.Locale,
                MetadataJson = messageDto.Metadata is not null
                    ? System.Text.Json.JsonSerializer.Serialize(messageDto.Metadata)
                    : null,
                CreatedAt = now,
                ModifiedAt = now
            };

            // Get tenant from chat
            var chat = await context.ChatThreads
                .Where(ct => ct.Id == messageDto.ChatId)
                .FirstOrDefaultAsync(cancellationToken);

            if (chat is null)
            {
                throw new InvalidOperationException($"Chat {messageDto.ChatId} not found");
            }

            message.TenantId = chat.TenantId;

            // Save message to database
            _ = context.ChatMessages.Add(message);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Update status to sent
            message.Status = MessageStatus.Sent;
            message.ModifiedAt = DateTime.UtcNow;
            _ = await context.SaveChangesAsync(cancellationToken);

            // Create attachments if provided
            var attachmentDtos = new List<MessageAttachmentDto>();
            if (messageDto.Attachments?.Any() == true)
            {
                var attachments = messageDto.Attachments.Select(att => new Data.Entities.Chat.MessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    TenantId = chat.TenantId,
                    FileName = att.FileName ?? "unknown",
                    FileUrl = att.FileUrl ?? string.Empty,
                    FileSize = att.FileSize,
                    ContentType = att.ContentType ?? "application/octet-stream",
                    CreatedAt = now,
                    ModifiedAt = now
                }).ToList();

                context.MessageAttachments.AddRange(attachments);
                _ = await context.SaveChangesAsync(cancellationToken);

                attachmentDtos = attachments.Select(att => new MessageAttachmentDto
                {
                    Id = att.Id,
                    FileName = att.FileName,
                    FileUrl = att.FileUrl,
                    FileSize = att.FileSize,
                    ContentType = att.ContentType
                }).ToList();
            }

            // Log audit trail for message sending
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: messageId,
                propertyName: "Send",
                operationType: "Insert",
                oldValue: null,
                newValue: $"Chat: {messageDto.ChatId}, Content Length: {messageDto.Content?.Length ?? 0}, Attachments: {messageDto.Attachments?.Count ?? 0}",
                changedBy: messageDto.SenderId.ToString(),
                entityDisplayName: $"Message: {messageId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} sent message {MessageId} in chat {ChatId} with {AttachmentCount} attachments in {ElapsedMs}ms",
                messageDto.SenderId, messageId, messageDto.ChatId, messageDto.Attachments?.Count ?? 0, stopwatch.ElapsedMilliseconds);

            // Build response DTO for real-time delivery
            var responseDto = new ChatMessageDto
            {
                Id = messageId,
                ChatId = messageDto.ChatId,
                SenderId = messageDto.SenderId,
                SenderName = "System", // TODO: Resolve sender name from user service
                Content = messageDto.Content,
                ReplyToMessageId = messageDto.ReplyToMessageId,
                Attachments = attachmentDtos,
                Status = MessageStatus.Sent,
                SentAt = now,
                IsEdited = false,
                IsDeleted = false,
                Locale = messageDto.Locale,
                Metadata = messageDto.Metadata
            };

            // Send real-time message via SignalR to all chat participants
            await hubContext.Clients.Group($"chat_{messageDto.ChatId}")
                .SendAsync("MessageReceived", responseDto);

            // Update chat's last activity
            await hubContext.Clients.Group($"chat_{messageDto.ChatId}")
                .SendAsync("ChatUpdated", new { ChatId = messageDto.ChatId, LastActivity = now });

            // Return response DTO
            return responseDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message in chat {ChatId}", messageDto.ChatId);
            throw new InvalidOperationException("Failed to send message", ex);
        }
    }

    /// <summary>
    /// Retrieves chat messages with filtering and pagination.
    /// STUB IMPLEMENTATION - Returns empty paginated results.
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        MessageSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Retrieving messages for chat {ChatId} from {FromDate} to {ToDate} - Page {Page}",
                searchDto.ChatId, searchDto.FromDate, searchDto.ToDate, searchDto.PageNumber);

            // 1. Build query from ChatMessages DbSet
            var query = context.ChatMessages
                .AsNoTracking()
                .Where(m => !m.IsDeleted || searchDto.IncludeDeleted)
                .AsQueryable();

            // 2. Apply filters from searchDto
            if (searchDto.ChatId.HasValue)
            {
                query = query.Where(m => m.ChatThreadId == searchDto.ChatId.Value);
            }

            if (searchDto.TenantId.HasValue)
            {
                query = query.Where(m => m.TenantId == searchDto.TenantId.Value);
            }

            if (searchDto.SenderId.HasValue)
            {
                query = query.Where(m => m.SenderId == searchDto.SenderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                query = query.Where(m => m.Content != null && m.Content.Contains(searchDto.SearchTerm));
            }

            if (searchDto.FromDate.HasValue)
            {
                query = query.Where(m => m.SentAt >= searchDto.FromDate.Value);
            }

            if (searchDto.ToDate.HasValue)
            {
                query = query.Where(m => m.SentAt <= searchDto.ToDate.Value);
            }

            if (searchDto.HasMediaType.HasValue)
            {
                query = query.Where(m => m.Attachments.Any(a => a.MediaType == searchDto.HasMediaType.Value));
            }

            // 3. Include related entities
            query = query
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts)
                .Include(m => m.ReplyToMessage);

            // 4. Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // 5. Apply sorting
            query = searchDto.SortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(m => m.SentAt)
                : query.OrderByDescending(m => m.SentAt);

            // 6. Apply pagination
            query = query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize);

            // 7. Execute query and map to DTOs
            var messages = await query.ToListAsync(cancellationToken);
            var messageDtos = messages.Select(MapToChatMessageDto).ToList();

            // 8. Return PagedResult
            return new PagedResult<ChatMessageDto>
            {
                Items = messageDtos,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all chat messages with pagination.
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrieving all messages - Page {Page}", pagination.Page);

            var query = context.ChatMessages
                .AsNoTracking()
                .Where(m => !m.IsDeleted)
                .Include(m => m.ChatThread)
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts);

            var totalCount = await query.CountAsync(cancellationToken);

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var messageDtos = messages.Select(MapToChatMessageDto).ToList();

            return new PagedResult<ChatMessageDto>
            {
                Items = messageDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves messages for a specific conversation with pagination.
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesByConversationAsync(
        Guid conversationId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Retrieving messages for conversation {ConversationId} - Page {Page}",
                conversationId, pagination.Page);

            var query = context.ChatMessages
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.ChatThreadId == conversationId)
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts);

            var totalCount = await query.CountAsync(cancellationToken);

            var messages = await query
                .OrderBy(m => m.SentAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var messageDtos = messages.Select(MapToChatMessageDto).ToList();

            return new PagedResult<ChatMessageDto>
            {
                Items = messageDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}.", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves unread messages for the current user with pagination.
    /// NOTE: Requires current user context to be passed from controller
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetUnreadMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrieving unread messages - Page {Page}", pagination.Page);

            // NOTE: This is a simplified implementation.
            // In a full implementation, you would need to:
            // 1. Get current user ID from context (e.g., IHttpContextAccessor or parameter)
            // 2. Query messages where ReadReceipts don't contain current user's read receipt
            // For now, we'll return messages with no read receipts

            var query = context.ChatMessages
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.ReadAt == null)
                .Include(m => m.ChatThread)
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts);

            var totalCount = await query.CountAsync(cancellationToken);

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var messageDtos = messages.Select(MapToChatMessageDto).ToList();

            return new PagedResult<ChatMessageDto>
            {
                Items = messageDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unread messages.");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific message by ID with access validation.
    /// </summary>
    public async Task<ChatMessageDto?> GetMessageByIdAsync(
        Guid messageId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Retrieving message {MessageId} for user {UserId} in tenant {TenantId}",
                messageId, userId, tenantId);

            // 1. Query ChatMessages by messageId with related entities
            var message = await context.ChatMessages
                .AsNoTracking()
                .Include(m => m.ChatThread)
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts)
                .Include(m => m.ReplyToMessage)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message is null)
            {
                logger.LogWarning("Message {MessageId} not found", messageId);
                return null;
            }

            // 2. Validate tenant access
            if (tenantId.HasValue && message.TenantId != tenantId.Value)
            {
                logger.LogWarning(
                    "Access denied: Message {MessageId} belongs to tenant {MessageTenantId}, but requested by tenant {RequestTenantId}",
                    messageId, message.TenantId, tenantId.Value);
                return null;
            }

            // 3. Validate user is member of chat
            var isMember = await context.ChatMembers
                .AsNoTracking()
                .AnyAsync(cm => cm.ChatThreadId == message.ChatThreadId && cm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                logger.LogWarning(
                    "Access denied: User {UserId} is not a member of chat {ChatId}",
                    userId, message.ChatThreadId);
                return null;
            }

            // 4. Check if message is deleted
            if (message.IsDeleted)
            {
                logger.LogWarning("Message {MessageId} is deleted", messageId);
                return null;
            }

            // 5. Map to ChatMessageDto
            return MapToChatMessageDto(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Edits an existing message with validation and change tracking.
    /// </summary>
    public async Task<ChatMessageDto> EditMessageAsync(
        EditMessageDto editDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find message by editDto.MessageId
            var message = await context.ChatMessages
                .Include(m => m.Attachments)
                .Include(m => m.ReadReceipts)
                .Include(m => m.ReplyToMessage)
                .FirstOrDefaultAsync(m => m.Id == editDto.MessageId, cancellationToken);

            if (message is null)
            {
                throw new InvalidOperationException($"Message {editDto.MessageId} not found");
            }

            // 2. Validate user is sender
            if (message.SenderId != editDto.UserId)
            {
                throw new UnauthorizedAccessException($"User {editDto.UserId} is not the sender of message {editDto.MessageId}");
            }

            // 3. Validate message is not deleted
            if (message.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot edit deleted message {editDto.MessageId}");
            }

            // 4. Store original content in metadata (if first edit)
            var metadata = string.IsNullOrEmpty(message.MetadataJson)
                ? new Dictionary<string, object>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message.MetadataJson) ?? new Dictionary<string, object>();

            var oldContent = message.Content ?? string.Empty;

            if (!message.IsEdited && !metadata.ContainsKey("OriginalContent"))
            {
                metadata["OriginalContent"] = oldContent;
            }

            // 5. Store edit reason in metadata
            if (!string.IsNullOrEmpty(editDto.EditReason))
            {
                metadata["EditReason"] = editDto.EditReason;
            }
            metadata["EditedBy"] = editDto.UserId.ToString();
            metadata["LastEditedAt"] = DateTime.UtcNow.ToString("O");

            // 6. Update message fields
            message.Content = editDto.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            message.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            message.ModifiedAt = DateTime.UtcNow;

            // 7. Save to database
            await context.SaveChangesAsync(cancellationToken);

            // 8. Log audit trail with old and new content
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: editDto.MessageId,
                propertyName: "Content",
                operationType: "Update",
                oldValue: oldContent,
                newValue: editDto.Content,
                changedBy: editDto.UserId.ToString(),
                entityDisplayName: $"Message Edit: {editDto.MessageId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} edited message {MessageId} with reason: {Reason}",
                editDto.UserId, editDto.MessageId, editDto.EditReason ?? "No reason provided");

            // 9. Map to DTO using helper method
            var mappedDto = MapToChatMessageDto(message);

            // 10. Send SignalR notification to chat members
            await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                .SendAsync("MessageEdited", mappedDto, cancellationToken);

            // 11. Return updated ChatMessageDto
            return mappedDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error editing message {MessageId}.", editDto.MessageId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a message with soft/hard delete options.
    /// </summary>
    public async Task<MessageOperationResultDto> DeleteMessageAsync(
        Guid messageId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find message by messageId
            var message = await context.ChatMessages
                .Include(m => m.ChatThread)
                    .ThenInclude(ct => ct.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message is null)
            {
                throw new InvalidOperationException($"Message {messageId} not found");
            }

            // 2. Validate user is sender OR chat owner/admin
            var isSender = message.SenderId == userId;
            var isOwnerOrAdmin = await context.ChatMembers
                .AsNoTracking()
                .AnyAsync(cm =>
                    cm.ChatThreadId == message.ChatThreadId &&
                    cm.UserId == userId &&
                    (cm.Role == ChatMemberRole.Owner || cm.Role == ChatMemberRole.Admin),
                    cancellationToken);

            if (!isSender && !isOwnerOrAdmin)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have permission to delete message {messageId}");
            }

            var oldStatus = message.IsDeleted ? "Deleted" : "Active";

            if (softDelete)
            {
                // 3. Soft delete: Set IsDeleted = true
                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;
                message.Status = MessageStatus.Deleted;
                message.ModifiedAt = DateTime.UtcNow;

                // Store delete reason in metadata
                var metadata = string.IsNullOrEmpty(message.MetadataJson)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message.MetadataJson) ?? new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(reason))
                {
                    metadata["DeleteReason"] = reason;
                }
                metadata["DeletedBy"] = userId.ToString();
                metadata["DeletedAt"] = DateTime.UtcNow.ToString("O");

                message.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            }
            else
            {
                // 4. Hard delete: Remove from database (only SuperAdmin should do this - validation should be done at controller level)
                context.ChatMessages.Remove(message);
            }

            // 5. Save changes
            await context.SaveChangesAsync(cancellationToken);

            // 6. Log audit trail
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: messageId,
                propertyName: softDelete ? "SoftDelete" : "HardDelete",
                operationType: "Delete",
                oldValue: oldStatus,
                newValue: softDelete ? "Deleted" : "Permanently Deleted",
                changedBy: userId.ToString(),
                entityDisplayName: $"Message Deletion: {messageId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} {DeleteType} message {MessageId} with reason: {Reason}",
                userId, softDelete ? "soft deleted" : "hard deleted", messageId, reason ?? "No reason provided");

            // 7. Send SignalR notification
            if (softDelete)
            {
                await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                    .SendAsync("MessageDeleted", new { MessageId = messageId, DeletedAt = DateTime.UtcNow }, cancellationToken);
            }

            // 8. Return MessageOperationResultDto with success status
            return new MessageOperationResultDto
            {
                MessageId = messageId,
                Success = true,
                NewStatus = MessageStatus.Deleted,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting message {MessageId}.", messageId);
            throw;
        }
    }

    #endregion

    #region Message Status & Read Receipts

    /// <summary>
    /// Updates message delivery status with real-time notification.
    /// STUB IMPLEMENTATION - Logs status update and returns mock result.
    /// </summary>
    public async Task<MessageStatusUpdateResultDto> UpdateMessageStatusAsync(
        Guid messageId,
        MessageStatus status,
        Guid userId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: messageId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: "Previous status",
                newValue: status.ToString(),
                changedBy: userId.ToString(),
                entityDisplayName: $"Message Status Update: {messageId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Message {MessageId} status updated to {Status} by user {UserId}",
                messageId, status, userId);

            return new MessageStatusUpdateResultDto
            {
                MessageId = messageId,
                Status = status,
                UserId = userId,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Marks a message as read by a user with timestamp tracking.
    /// STUB IMPLEMENTATION - Logs read action and returns mock receipt.
    /// </summary>
    public async Task<MessageReadReceiptDto> MarkMessageAsReadAsync(
        Guid messageId,
        Guid userId,
        DateTime? readAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var readTimestamp = readAt ?? DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "MessageReadReceipt",
                entityId: messageId,
                propertyName: "ReadAt",
                operationType: "Insert",
                oldValue: null,
                newValue: readTimestamp.ToString(),
                changedBy: userId.ToString(),
                entityDisplayName: $"Message Read: {messageId}",
                cancellationToken: cancellationToken);

            logger.LogDebug(
                "User {UserId} marked message {MessageId} as read at {ReadAt}",
                userId, messageId, readTimestamp);

            return new MessageReadReceiptDto
            {
                UserId = userId,
                Username = $"User_{userId:N}", // TODO: Resolve username from user service
                ReadAt = readTimestamp
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking message {MessageId} as read.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive read receipt information for a message.
    /// STUB IMPLEMENTATION - Returns empty list.
    /// </summary>
    public async Task<List<MessageReadReceiptDto>> GetMessageReadReceiptsAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug(
                "Retrieving read receipts for message {MessageId} requested by user {UserId}",
                messageId, requestingUserId);

            // TODO: Implement database query for read receipts
            await Task.Delay(5, cancellationToken); // Simulate async operation

            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving read receipts for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Bulk marks multiple messages as read for efficient processing.
    /// STUB IMPLEMENTATION - Processes each message individually.
    /// </summary>
    public async Task<BulkReadResultDto> BulkMarkAsReadAsync(
        List<Guid> messageIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var successCount = 0;
            var processedIds = new List<Guid>();
            var errors = new List<string>();

            logger.LogInformation(
                "User {UserId} bulk marking {Count} messages as read",
                userId, messageIds.Count);

            foreach (var messageId in messageIds)
            {
                try
                {
                    _ = await MarkMessageAsReadAsync(messageId, userId, null, cancellationToken);
                    processedIds.Add(messageId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to mark message {messageId} as read: {ex.Message}");
                    logger.LogWarning(ex, "Failed to mark message {MessageId} as read for user {UserId}", messageId, userId);
                }
            }

            return new BulkReadResultDto
            {
                TotalCount = messageIds.Count,
                SuccessCount = successCount,
                FailureCount = messageIds.Count - successCount,
                ProcessedMessageIds = processedIds,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk marking messages as read for user {UserId}.", userId);
            throw;
        }
    }

    #endregion

    #region File & Media Management

    /// <summary>
    /// Uploads and processes file attachments with validation.
    /// STUB IMPLEMENTATION - Returns mock upload result.
    /// </summary>
    public async Task<FileUploadResultDto> UploadFileAsync(
        FileUploadDto uploadDto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate rate limits for file uploads
            await ValidateChatRateLimitAsync(null, uploadDto.UploadedBy, ChatOperationType.UploadFile, cancellationToken);

            // Generate attachment ID
            var attachmentId = Guid.NewGuid();

            // TODO: Implement actual file upload logic:
            // - Save file to storage (local, cloud, etc.)
            // - Virus scanning and validation
            // - Thumbnail generation for images/videos
            // - File optimization and compression
            // - Metadata extraction

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "MessageAttachment",
                entityId: attachmentId,
                propertyName: "Upload",
                operationType: "Insert",
                oldValue: null,
                newValue: $"File: {uploadDto.FileName}, Size: {uploadDto.FileSize}, Type: {uploadDto.ContentType}",
                changedBy: uploadDto.UploadedBy.ToString(),
                entityDisplayName: $"File Upload: {uploadDto.FileName}",
                cancellationToken: cancellationToken);

            // Simulate file processing delay
            await Task.Delay(100, cancellationToken);

            logger.LogInformation(
                "User {UserId} uploaded file {FileName} ({FileSize} bytes) to chat {ChatId} in {ElapsedMs}ms",
                uploadDto.UploadedBy, uploadDto.FileName, uploadDto.FileSize, uploadDto.ChatId, stopwatch.ElapsedMilliseconds);

            // Determine media type from content type
            var mediaType = DetermineMediaType(uploadDto.ContentType);

            return new FileUploadResultDto
            {
                AttachmentId = attachmentId,
                FileName = uploadDto.FileName,
                FileUrl = $"/api/files/{attachmentId}/download", // TODO: Generate actual URL
                ThumbnailUrl = mediaType == MediaType.Image ? $"/api/files/{attachmentId}/thumbnail" : null,
                MediaType = mediaType,
                FileSize = uploadDto.FileSize,
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", uploadDto.FileName, uploadDto.UploadedBy);

            return new FileUploadResultDto
            {
                FileName = uploadDto.FileName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets secure file download information with access validation.
    /// STUB IMPLEMENTATION - Returns mock download info.
    /// </summary>
    public async Task<FileDownloadInfoDto?> GetFileDownloadInfoAsync(
        Guid attachmentId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "User {UserId} requesting download info for attachment {AttachmentId} in tenant {TenantId}",
                userId, attachmentId, tenantId);

            // TODO: Implement access validation and secure URL generation
            await Task.Delay(5, cancellationToken); // Simulate async operation

            // Return mock download info (in real implementation, validate access first)
            return new FileDownloadInfoDto
            {
                AttachmentId = attachmentId,
                FileName = "sample-file.pdf",
                DownloadUrl = $"/api/files/{attachmentId}/secure-download?token=mock-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                FileSize = 1024 * 1024, // 1MB
                ContentType = "application/pdf"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving download info for attachment {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <summary>
    /// Processes media files for optimization and thumbnail generation.
    /// STUB IMPLEMENTATION - Returns mock processing result.
    /// </summary>
    public async Task<MediaProcessingResultDto> ProcessMediaAsync(
        Guid attachmentId,
        MediaProcessingOptionsDto processingOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
        logger.LogInformation(
            "Processing media {AttachmentId} with options: Thumbnails={GenerateThumbnails}, Optimize={OptimizeForWeb}",
            attachmentId, processingOptions.GenerateThumbnails, processingOptions.OptimizeForWeb);

        // TODO: Implement actual media processing
        await Task.Delay(500, cancellationToken); // Simulate processing time

        var variants = new List<MediaVariantDto>();

        if (processingOptions.GenerateThumbnails)
        {
            variants.Add(new MediaVariantDto
            {
                VariantType = "thumbnail",
                Url = $"/api/files/{attachmentId}/thumbnail",
                Format = "jpeg",
                FileSize = 1024 * 10, // 10KB
                Properties = new Dictionary<string, object>
                {
                    ["width"] = 150,
                    ["height"] = 150
                }
            });
        }

        if (processingOptions.OptimizeForWeb)
        {
            variants.Add(new MediaVariantDto
            {
                VariantType = "optimized",
                Url = $"/api/files/{attachmentId}/optimized",
                Format = "webp",
                FileSize = 1024 * 500, // 500KB
                Properties = new Dictionary<string, object>
                {
                    ["width"] = 1920,
                    ["height"] = 1080,
                    ["quality"] = 85
                }
            });
        }

        return new MediaProcessingResultDto
        {
            AttachmentId = attachmentId,
            Success = true,
            GeneratedVariants = variants
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ProcessMediaAsync for attachment {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <summary>
    /// Deletes file attachments with cleanup.
    /// STUB IMPLEMENTATION - Logs deletion and returns success result.
    /// </summary>
    public async Task<FileOperationResultDto> DeleteFileAsync(
        Guid attachmentId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "MessageAttachment",
            entityId: attachmentId,
            propertyName: "Delete",
            operationType: "Delete",
            oldValue: "Active",
            newValue: "Deleted",
            changedBy: userId.ToString(),
            entityDisplayName: $"File Deletion: {attachmentId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted file attachment {AttachmentId} with reason: {Reason}",
            userId, attachmentId, reason ?? "No reason provided");

        // TODO: Implement actual file deletion from storage
        await Task.Delay(50, cancellationToken);

        return new FileOperationResultDto
        {
            AttachmentId = attachmentId,
            Success = true
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteFileAsync for attachment {AttachmentId}.", attachmentId);
            throw;
        }
    }

    #endregion

    #region Chat Member Management

    /// <summary>
    /// Adds new members to a chat with validation and notification.
    /// STUB IMPLEMENTATION - Logs addition and returns mock result.
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
        var results = new List<MemberOperationDetail>();
        var successCount = 0;

        logger.LogInformation(
            "User {AddedBy} adding {Count} members to chat {ChatId} with role {Role}",
            addedBy, userIds.Count, chatId, defaultRole);

        foreach (var userId in userIds)
        {
            try
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    entityName: "ChatMember",
                    entityId: chatId,
                    propertyName: "AddMember",
                    operationType: "Insert",
                    oldValue: null,
                    newValue: $"User: {userId}, Role: {defaultRole}",
                    changedBy: addedBy.ToString(),
                    entityDisplayName: $"Member Addition: {chatId}",
                    cancellationToken: cancellationToken);

                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = true,
                    AssignedRole = defaultRole
                });
                successCount++;

                logger.LogDebug("Added user {UserId} to chat {ChatId} with role {Role}", userId, chatId, defaultRole);
            }
            catch (Exception ex)
            {
                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                logger.LogWarning(ex, "Failed to add user {UserId} to chat {ChatId}", userId, chatId);
            }
        }

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
            logger.LogError(ex, "Error in AddMembersAsync for chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Removes members from a chat with notification and cleanup.
    /// STUB IMPLEMENTATION - Logs removal and returns mock result.
    /// </summary>
    public async Task<MemberOperationResultDto> RemoveMembersAsync(
        Guid chatId,
        List<Guid> userIds,
        Guid removedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
        var results = new List<MemberOperationDetail>();
        var successCount = 0;

        logger.LogInformation(
            "User {RemovedBy} removing {Count} members from chat {ChatId} with reason: {Reason}",
            removedBy, userIds.Count, chatId, reason ?? "No reason provided");

        foreach (var userId in userIds)
        {
            try
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    entityName: "ChatMember",
                    entityId: chatId,
                    propertyName: "RemoveMember",
                    operationType: "Delete",
                    oldValue: $"User: {userId}",
                    newValue: null,
                    changedBy: removedBy.ToString(),
                    entityDisplayName: $"Member Removal: {chatId}",
                    cancellationToken: cancellationToken);

                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = true
                });
                successCount++;

                logger.LogDebug("Removed user {UserId} from chat {ChatId}", userId, chatId);
            }
            catch (Exception ex)
            {
                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                logger.LogWarning(ex, "Failed to remove user {UserId} from chat {ChatId}", userId, chatId);
            }
        }

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
            logger.LogError(ex, "Error in RemoveMembersAsync for chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Updates member roles and permissions with validation.
    /// STUB IMPLEMENTATION - Logs updates and returns mock result.
    /// </summary>
    public async Task<MemberOperationResultDto> UpdateMemberRolesAsync(
        Guid chatId,
        Dictionary<Guid, ChatMemberRole> roleUpdates,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
        var results = new List<MemberOperationDetail>();
        var successCount = 0;

        logger.LogInformation(
            "User {UpdatedBy} updating roles for {Count} members in chat {ChatId}",
            updatedBy, roleUpdates.Count, chatId);

        foreach (var (userId, newRole) in roleUpdates)
        {
            try
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    entityName: "ChatMember",
                    entityId: chatId,
                    propertyName: "Role",
                    operationType: "Update",
                    oldValue: "Previous role",
                    newValue: newRole.ToString(),
                    changedBy: updatedBy.ToString(),
                    entityDisplayName: $"Role Update: {chatId}",
                    cancellationToken: cancellationToken);

                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = true,
                    AssignedRole = newRole
                });
                successCount++;

                logger.LogDebug("Updated role for user {UserId} in chat {ChatId} to {Role}", userId, chatId, newRole);
            }
            catch (Exception ex)
            {
                results.Add(new MemberOperationDetail
                {
                    UserId = userId,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                logger.LogWarning(ex, "Failed to update role for user {UserId} in chat {ChatId}", userId, chatId);
            }
        }

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
            logger.LogError(ex, "Error in UpdateMemberRolesAsync for chat {ChatId}.", chatId);
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
        try
        {
            logger.LogInformation(
                "User {UserId} requesting members for chat {ChatId}",
                requestingUserId, chatId);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving members for chat {ChatId}.", chatId);
            throw;
        }
    }

    /// <summary>
    /// Returns all active users in the tenant for new-chat recipient selection.
    /// </summary>
    public async Task<List<ChatAvailableUserDto>> GetAvailableUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting available chat users for tenant {TenantId}", tenantId);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available users for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    #endregion

    #region Rate Limiting & Multi-Tenant Management

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
            // Simple rate limiting implementation
            // In a production environment, this should use Redis or distributed cache

            // Define rate limits by operation type
            var rateLimits = new Dictionary<ChatOperationType, int>
            {
                { ChatOperationType.SendMessage, 1000 },
                { ChatOperationType.CreateChat, 50 },
                { ChatOperationType.UploadFile, 100 },
                { ChatOperationType.EditMessage, 200 },
                { ChatOperationType.DeleteMessage, 100 }
            };

            var limit = rateLimits.GetValueOrDefault(operationType, 100);
            var remainingQuota = limit - 1; // Simplified - in reality, track actual usage

            // Check if limit would be exceeded (simplified logic)
            var isAllowed = remainingQuota > 0;

            await Task.Delay(2, cancellationToken); // Simulate async operation

            return new ChatRateLimitStatusDto
            {
                IsAllowed = isAllowed,
                RemainingQuota = Math.Max(0, remainingQuota),
                ResetTime = TimeSpan.FromHours(1),
                OperationType = operationType,
                LimitDetails = new Dictionary<string, object>
                {
                    ["TenantId"] = tenantId?.ToString() ?? "System",
                    ["UserId"] = userId?.ToString() ?? "N/A",
                    ["Operation"] = operationType.ToString(),
                    ["Limit"] = limit,
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
    /// STUB IMPLEMENTATION - Logs update and returns policy.
    /// </summary>
    public async Task<ChatRateLimitPolicyDto> UpdateTenantChatRateLimitAsync(
        Guid tenantId,
        ChatRateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating chat rate limit policy for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive chat statistics and analytics.
    /// STUB IMPLEMENTATION - Returns empty statistics.
    /// </summary>
    public async Task<ChatStatsDto> GetChatStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Retrieving chat statistics for tenant {TenantId} from {StartDate} to {EndDate}",
                tenantId?.ToString() ?? "ALL",
                dateRange?.StartDate.ToString("yyyy-MM-dd") ?? "N/A",
                dateRange?.EndDate.ToString("yyyy-MM-dd") ?? "N/A");

            // TODO: Implement database aggregation queries
            await Task.Delay(30, cancellationToken);

            return new ChatStatsDto
            {
                TenantId = tenantId,
                TotalChats = 0,
                ActiveChats = 0,
                DirectMessageChats = 0,
                GroupChats = 0,
                TotalMessages = 0,
                MessagesLastWeek = 0,
                MessagesLastMonth = 0,
                MediaCountByType = new Dictionary<MediaType, int>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving chat statistics for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    #endregion

    #region Moderation & Administration

    /// <summary>
    /// Performs moderation actions on chats and messages.
    /// STUB IMPLEMENTATION - Logs action and returns success result.
    /// </summary>
    public async Task<ModerationResultDto> ModerateChatAsync(
        ChatModerationActionDto moderationAction,
        CancellationToken cancellationToken = default)
    {
        try
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

            // TODO: Implement actual moderation logic
            await Task.Delay(50, cancellationToken);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error moderating chat {ChatId}.", moderationAction.ChatId);
            throw;
        }
    }

    /// <summary>
    /// Gets detailed audit trail for chat operations.
    /// STUB IMPLEMENTATION - Returns empty paginated results.
    /// </summary>
    public async Task<PagedResult<ChatAuditEntryDto>> GetChatAuditTrailAsync(
        ChatAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default)
    {
        try
        {
        logger.LogInformation(
            "Retrieving chat audit trail for tenant {TenantId} from {FromDate} to {ToDate}",
            auditQuery.TenantId, auditQuery.FromDate, auditQuery.ToDate);

        // TODO: Query audit log entries from database
        await Task.Delay(25, cancellationToken);

        return new PagedResult<ChatAuditEntryDto>
        {
            Items = [],
            Page = auditQuery.PageNumber,
            PageSize = auditQuery.PageSize,
            TotalCount = 0
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetChatAuditTrailAsync.");
            throw;
        }
    }

    /// <summary>
    /// Monitors chat system health with real-time alerting.
    /// STUB IMPLEMENTATION - Returns healthy status.
    /// </summary>
    public async Task<ChatSystemHealthDto> GetChatSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
        logger.LogDebug("Checking chat system health");

        // TODO: Implement health checks
        await Task.Delay(10, cancellationToken);

        return new ChatSystemHealthDto
        {
            Status = "Healthy",
            Metrics = new Dictionary<string, object>
            {
                ["DatabaseConnected"] = true,
                ["FileStorageConnected"] = true,
                ["SignalRConnected"] = true,
                ["ActiveConnections"] = 0,
                ["MessageProcessingRate"] = "0 messages/sec",
                ["FileProcessingQueue"] = 0,
                ["LastHealthCheck"] = DateTime.UtcNow
            },
            Alerts = new List<string>()
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetChatSystemHealthAsync.");
            throw;
        }
    }

    #endregion

    #region Localization & Accessibility

    /// <summary>
    /// Localizes chat content based on user preferences.
    /// </summary>
    public async Task<ChatMessageDto> LocalizeChatMessageAsync(
        ChatMessageDto message,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
        logger.LogInformation(
            "Localizing message {MessageId} to locale {Locale} for user {UserId}",
            message.Id, targetLocale, userId);

        // 1. Check if message.Locale == targetLocale (already localized)
        if (message.Locale == targetLocale)
        {
            logger.LogDebug("Message {MessageId} is already in locale {Locale}", message.Id, targetLocale);
            return message;
        }

        // 2. Update message.Locale = targetLocale
        message.Locale = targetLocale;

        // 3. Store localized content in metadata
        // For Phase 1: Simple implementation - just update Locale field
        // Future: Integrate with translation service for actual content translation
        if (message.Metadata is null)
        {
            message.Metadata = new Dictionary<string, object>();
        }

        message.Metadata["LocalizedTo"] = targetLocale;
        message.Metadata["LocalizedAt"] = DateTime.UtcNow.ToString("O");
        if (userId.HasValue)
        {
            message.Metadata["LocalizedBy"] = userId.Value.ToString();
        }

        // Note: Localization is transient (client-side only) and not persisted to database
        await Task.CompletedTask; // Satisfy async signature

        // 4. Return localized ChatMessageDto
        return message;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in LocalizeChatMessageAsync for message {MessageId}.", message.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates chat localization preferences for users.
    /// </summary>
    public async Task<ChatLocalizationPreferencesDto> UpdateChatLocalizationAsync(
        Guid userId,
        ChatLocalizationPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find user
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            // 2. Store preferences in User's PreferredLanguage field
            // For Phase 1: Simple implementation using existing User fields
            var oldLocale = user.PreferredLanguage;
            user.PreferredLanguage = preferences.PreferredLocale;
            user.ModifiedAt = DateTime.UtcNow;

            // 3. Save to database
            await context.SaveChangesAsync(cancellationToken);

            // 4. Log audit trail
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatLocalizationPreferences",
                entityId: userId,
                propertyName: "PreferredLocale",
                operationType: "Update",
                oldValue: oldLocale,
                newValue: preferences.PreferredLocale,
                changedBy: userId.ToString(),
                entityDisplayName: $"Chat Localization: {userId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Updated chat localization preferences for user {UserId}: Locale={Locale}, AutoTranslate={AutoTranslate}",
                userId, preferences.PreferredLocale, preferences.AutoTranslate);

            // 5. Return updated preferences
            preferences.UserId = userId;
            return preferences;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating chat localization preferences for user {UserId}.", userId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds ChatMemberDto list for a given chat by joining ChatMembers with Users.
    /// </summary>
    private async Task<List<ChatMemberDto>> BuildMemberDtosAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var membersWithUsers = await context.ChatMembers
            .AsNoTracking()
            .Where(cm => cm.ChatThreadId == chatId && !cm.IsDeleted)
            .Join(context.Users,
                cm => cm.UserId,
                u => u.Id,
                (cm, u) => new
                {
                    cm.UserId,
                    u.Username,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    cm.Role,
                    cm.JoinedAt,
                    cm.LastSeenAt,
                    cm.IsMuted
                })
            .ToListAsync(cancellationToken);

        return membersWithUsers.Select(m => new ChatMemberDto
        {
            UserId = m.UserId,
            Username = m.Username,
            DisplayName = m.FullName.Length > 0 ? m.FullName : m.Username,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
            LastSeenAt = m.LastSeenAt,
            IsOnline = onlineUserTracker.IsOnline(m.UserId),
            IsMuted = m.IsMuted
        }).ToList();
    }

    /// <summary>
    /// Maps a ChatThread entity to ChatResponseDto.
    /// </summary>
    private static ChatResponseDto MapToChatResponseDto(
        Data.Entities.Chat.ChatThread thread,
        List<ChatMemberDto> members,
        ChatMessageDto? lastMessage,
        int unreadCount)
    {
        return new ChatResponseDto
        {
            Id = thread.Id,
            TenantId = thread.TenantId,
            Type = thread.Type,
            Name = thread.Name,
            Description = thread.Description,
            IsPrivate = thread.IsPrivate,
            PreferredLocale = thread.PreferredLocale,
            CreatedAt = thread.CreatedAt,
            UpdatedAt = thread.UpdatedAt,
            IsActive = thread.IsActive,
            Members = members,
            LastMessage = lastMessage,
            UnreadCount = unreadCount
        };
    }

    /// <summary>
    /// Validates tenant access for multi-tenant operations.
    /// TODO: Implement actual tenant validation logic.
    /// </summary>
    private async Task ValidateTenantAccessAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId.HasValue)
        {
            logger.LogDebug("Validating tenant access for {TenantId}", tenantId.Value);
            // TODO: Validate tenant exists and is active
        }
        await Task.Delay(1, cancellationToken);
    }

    /// <summary>
    /// Validates rate limiting before chat operations.
    /// TODO: Implement actual rate limiting validation.
    /// </summary>
    private async Task ValidateChatRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        ChatOperationType operationType,
        CancellationToken cancellationToken)
    {
        var rateLimitStatus = await CheckChatRateLimitAsync(tenantId, userId, operationType, cancellationToken);

        if (!rateLimitStatus.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Rate limit exceeded for tenant {tenantId}, user {userId}, operation {operationType}. " +
                $"Quota resets in {rateLimitStatus.ResetTime}");
        }
    }

    /// <summary>
    /// Determines media type from content type.
    /// </summary>
    private static MediaType DetermineMediaType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            var ct when ct.StartsWith("image/") => MediaType.Image,
            var ct when ct.StartsWith("video/") => MediaType.Video,
            var ct when ct.StartsWith("audio/") => MediaType.Audio,
            var ct when ct.Contains("zip") || ct.Contains("rar") || ct.Contains("7z") => MediaType.Archive,
            _ => MediaType.Document
        };
    }

    /// <summary>
    /// Maps ChatMessage entity to ChatMessageDto.
    /// </summary>
    private static ChatMessageDto MapToChatMessageDto(Data.Entities.Chat.ChatMessage message)
    {
        Dictionary<string, object> metadata;
        try
        {
            metadata = string.IsNullOrEmpty(message.MetadataJson)
                ? new Dictionary<string, object>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message.MetadataJson) ?? new Dictionary<string, object>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Log error and return empty dictionary for corrupted metadata
            metadata = new Dictionary<string, object>
            {
                ["_DeserializationError"] = $"Failed to deserialize metadata: {ex.Message}"
            };
        }

        return new ChatMessageDto
        {
            Id = message.Id,
            ChatId = message.ChatThreadId,
            SenderId = message.SenderId ?? Guid.Empty,
            SenderName = $"User_{message.SenderId:N}", // TODO: Resolve from user service
            Content = message.Content,
            ReplyToMessageId = message.ReplyToMessageId,
            ReplyToMessage = message.ReplyToMessage is not null ? new ChatMessageDto
            {
                Id = message.ReplyToMessage.Id,
                ChatId = message.ReplyToMessage.ChatThreadId,
                SenderId = message.ReplyToMessage.SenderId ?? Guid.Empty,
                Content = message.ReplyToMessage.Content,
                SentAt = message.ReplyToMessage.SentAt
            } : null,
            Attachments = message.Attachments.Select(a => new MessageAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                OriginalFileName = a.OriginalFileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                MediaType = a.MediaType,
                FileUrl = a.FileUrl,
                ThumbnailUrl = a.ThumbnailUrl,
                UploadedAt = a.UploadedAt,
                UploadedBy = a.UploadedBy
            }).ToList(),
            ReadReceipts = message.ReadReceipts.Select(r => new MessageReadReceiptDto
            {
                UserId = r.UserId,
                Username = $"User_{r.UserId:N}", // TODO: Resolve from user service
                ReadAt = r.ReadAt
            }).ToList(),
            Status = message.Status,
            SentAt = message.SentAt,
            DeliveredAt = message.DeliveredAt,
            ReadAt = message.ReadAt,
            EditedAt = message.EditedAt,
            DeletedAt = message.DeletedAt,
            IsEdited = message.IsEdited,
            IsDeleted = message.IsDeleted,
            Locale = message.Locale,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Toggle message reaction (add or remove).
    /// TODO: Implement actual reaction persistence and real-time updates.
    /// </summary>
    public async Task<MessageOperationResultDto> ToggleMessageReactionAsync(
        MessageReactionActionDto reactionDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
        logger.LogInformation(
            "Toggling reaction {Emoji} on message {MessageId} by user {UserId}",
            reactionDto.Emoji, reactionDto.MessageId, reactionDto.UserId);

        // TODO: Implement actual reaction logic
        // 1. Check if user has permission to react to this message
        // 2. Check if reaction already exists
        // 3. Add or remove reaction from database
        // 4. Send real-time update via SignalR
        // 5. Update message reaction counts

        await Task.Delay(100, cancellationToken); // Placeholder

        return new MessageOperationResultDto
        {
            Success = true,
            MessageId = reactionDto.MessageId
        };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ToggleMessageReactionAsync for message {MessageId}.", reactionDto.MessageId);
            throw;
        }
    }

    #endregion

}
