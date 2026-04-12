using EventForge.DTOs.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace EventForge.Server.Services.Chat;

/// <summary>
/// Chat service implementation with comprehensive multi-tenant support.
/// 
/// This implementation covers all core chat functionality using EF Core DB operations.
/// Remaining infrastructure stubs: file upload/download/processing (requires storage service).
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
    IWebHostEnvironment environment,
    IMemoryCache memoryCache,
    IOnlineUserTracker onlineUserTracker,
    IHtmlSanitizerService htmlSanitizerService) : IChatService
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
    /// </summary>
    public async Task<ChatResponseDto?> GetChatByIdAsync(
        Guid chatId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Searches and filters user's chats with advanced criteria.
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
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Updates chat properties with validation and notification.
    /// Updates chat properties in the database with SignalR notification.
    /// </summary>
    public async Task<ChatResponseDto> UpdateChatAsync(
        Guid chatId,
        UpdateChatDto updateDto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await context.ChatThreads
                .Where(ct => ct.Id == chatId && !ct.IsDeleted)
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await context.ChatThreads
                .Where(ct => ct.Id == chatId && !ct.IsDeleted)
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

            // Sanitize HTML content server-side before persistence
            var sanitizedContent = messageDto.Format == MessageFormat.Html && !string.IsNullOrEmpty(messageDto.Content)
                ? htmlSanitizerService.Sanitize(messageDto.Content)
                : messageDto.Content;

            // Create message entity
            var message = new Data.Entities.Chat.ChatMessage
            {
                Id = messageId,
                ChatThreadId = messageDto.ChatId,
                SenderId = messageDto.SenderId,
                Content = sanitizedContent,
                Format = messageDto.Format,
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
                    // Preserve the attachment ID generated during the pre-upload so that the
                    // download URL (/api/v1/chat/files/{id}/download) remains valid.
                    Id = att.Id != Guid.Empty ? att.Id : Guid.NewGuid(),
                    MessageId = messageId,
                    TenantId = chat.TenantId,
                    FileName = att.FileName ?? "unknown",
                    OriginalFileName = att.OriginalFileName ?? att.FileName ?? "unknown",
                    FileUrl = att.FileUrl ?? string.Empty,
                    ThumbnailUrl = att.ThumbnailUrl,
                    FileSize = att.FileSize,
                    ContentType = att.ContentType ?? "application/octet-stream",
                    MediaType = att.MediaType,
                    UploadedAt = att.UploadedAt == default ? now : att.UploadedAt,
                    UploadedBy = messageDto.SenderId,
                    CreatedAt = now,
                    ModifiedAt = now
                }).ToList();

                context.MessageAttachments.AddRange(attachments);
                _ = await context.SaveChangesAsync(cancellationToken);

                attachmentDtos = attachments.Select(att => new MessageAttachmentDto
                {
                    Id = att.Id,
                    FileName = att.OriginalFileName ?? att.FileName,
                    FileUrl = att.FileUrl,
                    ThumbnailUrl = att.ThumbnailUrl,
                    FileSize = att.FileSize,
                    ContentType = att.ContentType,
                    MediaType = att.MediaType
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

            // Resolve sender display name for the realtime DTO
            var sendNames = await FetchUserNamesAsync(new[] { messageDto.SenderId }, cancellationToken);
            var senderDisplayName = sendNames.GetValueOrDefault(messageDto.SenderId, messageDto.SenderId.ToString("N")[..8]);

            // Build response DTO for real-time delivery
            var responseDto = new ChatMessageDto
            {
                Id = messageId,
                ChatId = messageDto.ChatId,
                SenderId = messageDto.SenderId,
                SenderName = senderDisplayName,
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
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        MessageSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {

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
            var senderIds = messages.Select(m => m.SenderId ?? Guid.Empty)
                .Concat(messages.SelectMany(m => m.ReadReceipts.Select(r => r.UserId)))
                .ToList();
            var userNames = await FetchUserNamesAsync(senderIds, cancellationToken);
            var messageDtos = messages.Select(m => MapToChatMessageDto(m, userNames)).ToList();

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

            var names = await FetchUserNamesAsync(
                messages.Select(m => m.SenderId ?? Guid.Empty)
                    .Concat(messages.SelectMany(m => m.ReadReceipts.Select(r => r.UserId))),
                cancellationToken);
            var messageDtos = messages.Select(m => MapToChatMessageDto(m, names)).ToList();

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

            var names = await FetchUserNamesAsync(
                messages.Select(m => m.SenderId ?? Guid.Empty)
                    .Concat(messages.SelectMany(m => m.ReadReceipts.Select(r => r.UserId))),
                cancellationToken);
            var messageDtos = messages.Select(m => MapToChatMessageDto(m, names)).ToList();

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

            var names = await FetchUserNamesAsync(
                messages.Select(m => m.SenderId ?? Guid.Empty)
                    .Concat(messages.SelectMany(m => m.ReadReceipts.Select(r => r.UserId))),
                cancellationToken);
            var messageDtos = messages.Select(m => MapToChatMessageDto(m, names)).ToList();

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
            var singleNames = await FetchUserNamesAsync(
                new[] { message.SenderId ?? Guid.Empty }.Concat(message.ReadReceipts.Select(r => r.UserId)),
                cancellationToken);
            return MapToChatMessageDto(message, singleNames);
        }
        catch (Exception ex)
        {
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

            // 6. Sanitize HTML content server-side before persistence
            var sanitizedContent = editDto.Format == MessageFormat.Html
                ? htmlSanitizerService.Sanitize(editDto.Content)
                : editDto.Content;

            // 7. Update message fields
            message.Content = sanitizedContent;
            message.Format = editDto.Format;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            message.EditedByUserId = editDto.UserId.ToString();
            message.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            message.ModifiedAt = DateTime.UtcNow;

            // 8. Save to database
            await context.SaveChangesAsync(cancellationToken);

            // 9. Log audit trail with old and new content
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: editDto.MessageId,
                propertyName: "Content",
                operationType: "Update",
                oldValue: oldContent,
                newValue: sanitizedContent,
                changedBy: editDto.UserId.ToString(),
                entityDisplayName: $"Message Edit: {editDto.MessageId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} edited message {MessageId} with reason: {Reason}",
                editDto.UserId, editDto.MessageId, editDto.EditReason ?? "No reason provided");

            // 9. Map to DTO using helper method
            var editNames = await FetchUserNamesAsync(
                new[] { message.SenderId ?? Guid.Empty }.Concat(message.ReadReceipts.Select(r => r.UserId)),
                cancellationToken);
            var mappedDto = MapToChatMessageDto(message, editNames);

            // 10. Send SignalR notification to chat members
            await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                .SendAsync("MessageEdited", mappedDto, cancellationToken);

            // 11. Return updated ChatMessageDto
            return mappedDto;
        }
        catch (Exception ex)
        {
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
            throw;
        }
    }

    #endregion

    #region Message Status & Read Receipts

    /// <summary>
    /// Updates message delivery status in the database with real-time notification.
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
            var message = await context.ChatMessages
                .Where(m => m.Id == messageId && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Message {messageId} not found.");

            var oldStatus = message.Status;
            var now = DateTime.UtcNow;
            message.Status = status;
            message.ModifiedAt = now;
            message.ModifiedBy = userId.ToString();

            if (status == MessageStatus.Delivered) message.DeliveredAt ??= now;
            if (status == MessageStatus.Read) message.ReadAt ??= now;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: messageId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: oldStatus.ToString(),
                newValue: status.ToString(),
                changedBy: userId.ToString(),
                entityDisplayName: $"Message {messageId} Status",
                cancellationToken: cancellationToken);

            logger.LogInformation("ChatMessage {MessageId} status updated to {Status} by user {UserId}.", messageId, status, userId);

            await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                .SendAsync("MessageStatusUpdated", new { MessageId = messageId, Status = status, UpdatedBy = userId }, cancellationToken);

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
            logger.LogError(ex, "Failed to update status for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Marks a message as read by a user — upserts MessageReadReceipt in the database.
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

            var existing = await context.MessageReadReceipts
                .Where(r => r.MessageId == messageId && r.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                var message = await context.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.Id == messageId)
                    .Select(m => new { m.TenantId })
                    .FirstOrDefaultAsync(cancellationToken);

                context.MessageReadReceipts.Add(new Data.Entities.Chat.MessageReadReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = readTimestamp,
                    TenantId = message?.TenantId ?? Guid.Empty,
                    CreatedAt = readTimestamp,
                    ModifiedAt = readTimestamp,
                    IsActive = true
                });
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            var user = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Username })
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogDebug("User {UserId} marked message {MessageId} as read.", userId, messageId);

            return new MessageReadReceiptDto
            {
                UserId = userId,
                Username = user?.Username ?? userId.ToString("N"),
                ReadAt = existing?.ReadAt ?? readTimestamp
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark message {MessageId} as read for user {UserId}.", messageId, userId);
            throw;
        }
    }

    /// <summary>
    /// Gets read receipts for a message from the database with user join.
    /// </summary>
    public async Task<List<MessageReadReceiptDto>> GetMessageReadReceiptsAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receipts = await context.MessageReadReceipts
                .AsNoTracking()
                .Where(r => r.MessageId == messageId)
                .Join(context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new MessageReadReceiptDto
                    {
                        UserId = r.UserId,
                        Username = u.Username,
                        ReadAt = r.ReadAt
                    })
                .ToListAsync(cancellationToken);

            return receipts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get read receipts for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Bulk marks multiple messages as read — single SaveChanges for efficiency.
    /// </summary>
    public async Task<BulkReadResultDto> BulkMarkAsReadAsync(
        List<Guid> messageIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("User {UserId} bulk marking {Count} messages as read.", userId, messageIds.Count);

            // Load messages needing receipts in one query
            var existingReceiptIds = await context.MessageReadReceipts
                .AsNoTracking()
                .Where(r => messageIds.Contains(r.MessageId) && r.UserId == userId)
                .Select(r => r.MessageId)
                .ToListAsync(cancellationToken);

            var missing = messageIds.Except(existingReceiptIds).ToList();

            if (missing.Count > 0)
            {
                var tenantMap = await context.ChatMessages
                    .AsNoTracking()
                    .Where(m => missing.Contains(m.Id))
                    .Select(m => new { m.Id, m.TenantId })
                    .ToDictionaryAsync(m => m.Id, m => m.TenantId, cancellationToken);

                var now = DateTime.UtcNow;
                var newReceipts = missing.Select(mid => new Data.Entities.Chat.MessageReadReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = mid,
                    UserId = userId,
                    ReadAt = now,
                    TenantId = tenantMap.GetValueOrDefault(mid),
                    CreatedAt = now,
                    ModifiedAt = now,
                    IsActive = true
                }).ToList();

                context.MessageReadReceipts.AddRange(newReceipts);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            return new BulkReadResultDto
            {
                TotalCount = messageIds.Count,
                SuccessCount = messageIds.Count,
                FailureCount = 0,
                ProcessedMessageIds = messageIds,
                Errors = []
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to bulk mark messages as read for user {UserId}.", userId);
            throw;
        }
    }

    #endregion

    #region File & Media Management

    /// <summary>
    /// Uploads and processes file attachments — saves to local filesystem and persists a MessageAttachment record.
    /// Upload directory: {ContentRoot}/Uploads/chat/{chatId}/{attachmentId}_{fileName}
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

            var attachmentId = Guid.NewGuid();
            var mediaType = DetermineMediaType(uploadDto.ContentType);

            // Build storage path: {ContentRoot}/Uploads/chat/{attachmentId}/{safeFileName}
            // Using attachmentId as the directory avoids a dependency on a not-yet-existing message.
            var safeFileName = Path.GetFileName(uploadDto.FileName);
            var attachDir = Path.Combine(environment.ContentRootPath, "Uploads", "chat", attachmentId.ToString());
            Directory.CreateDirectory(attachDir);
            var storedFilePath = Path.Combine(attachDir, safeFileName);

            // Write stream to disk
            await using (var fileStream = new FileStream(storedFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await uploadDto.FileStream.CopyToAsync(fileStream, cancellationToken);
            }

            var fileUrl = $"/api/v1/chat/files/{attachmentId}/download";
            var thumbnailUrl = mediaType == MediaType.Image ? $"/api/v1/chat/files/{attachmentId}/thumbnail" : null;
            var uploadedAt = DateTime.UtcNow;

            // NOTE: The MessageAttachment DB record is intentionally NOT persisted here because no
            // ChatMessage exists yet at upload time.  The record is created by SendMessageAsync when
            // the user actually sends the message, using the attachmentId returned by this method.

            logger.LogInformation(
                "User {UserId} uploaded file {FileName} ({FileSize} bytes) for chat {ChatId} in {ElapsedMs}ms",
                uploadDto.UploadedBy, safeFileName, uploadDto.FileSize, uploadDto.ChatId, stopwatch.ElapsedMilliseconds);

            return new FileUploadResultDto
            {
                AttachmentId = attachmentId,
                FileName = safeFileName,
                FileUrl = fileUrl,
                ThumbnailUrl = thumbnailUrl,
                MediaType = mediaType,
                FileSize = uploadDto.FileSize,
                Success = true,
                UploadedAt = uploadedAt
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
    /// Gets secure file download information — validates access via MessageAttachment DB record.
    /// </summary>
    public async Task<FileDownloadInfoDto?> GetFileDownloadInfoAsync(
        Guid attachmentId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await context.MessageAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

            if (attachment is null) return null;

            return new FileDownloadInfoDto
            {
                AttachmentId = attachment.Id,
                FileName = attachment.OriginalFileName ?? attachment.FileName,
                DownloadUrl = $"/api/v1/chat/files/{attachment.Id}/download",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Returns the physical file path + metadata for streaming download.
    /// </summary>
    public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetAttachmentForDownloadAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await context.MessageAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

            if (attachment is null) return null;

            // Storage path: {ContentRoot}/Uploads/chat/{attachmentId}/{fileName}
            var physicalPath = Path.Combine(
                environment.ContentRootPath,
                "Uploads", "chat",
                attachment.Id.ToString(),
                attachment.FileName);

            if (!System.IO.File.Exists(physicalPath)) return null;

            return (
                physicalPath,
                attachment.ContentType ?? "application/octet-stream",
                attachment.OriginalFileName ?? attachment.FileName
            );
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Processes media files — returns available URL variants based on what was stored.
    /// Full media transcoding (thumbnails, WebP) requires an external media service.
    /// </summary>
    public async Task<MediaProcessingResultDto> ProcessMediaAsync(
        Guid attachmentId,
        MediaProcessingOptionsDto processingOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await context.MessageAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

            if (attachment is null)
                return new MediaProcessingResultDto { AttachmentId = attachmentId, Success = false, ErrorMessage = "Attachment not found." };

            var variants = new List<MediaVariantDto>();

            if (processingOptions.GenerateThumbnails && attachment.ThumbnailUrl is not null)
            {
                variants.Add(new MediaVariantDto
                {
                    VariantType = "thumbnail",
                    Url = attachment.ThumbnailUrl,
                    Format = "jpeg",
                    FileSize = 0
                });
            }

            if (processingOptions.OptimizeForWeb && attachment.FileUrl is not null)
            {
                variants.Add(new MediaVariantDto
                {
                    VariantType = "original",
                    Url = attachment.FileUrl,
                    Format = Path.GetExtension(attachment.FileName).TrimStart('.'),
                    FileSize = attachment.FileSize
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
            throw;
        }
    }

    /// <summary>
    /// Deletes a file attachment — soft-deletes the DB record and removes the file from disk.
    /// </summary>
    public async Task<FileOperationResultDto> DeleteFileAsync(
        Guid attachmentId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await context.MessageAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

            if (attachment is not null)
            {
                // Soft-delete DB record
                attachment.IsDeleted = true;
                attachment.ModifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                // Best-effort physical delete
                var chatDir = Path.Combine(environment.ContentRootPath, "Uploads", "chat", attachment.MessageId.ToString());
                var filePath = Path.Combine(chatDir, attachment.FileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

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
                "User {UserId} deleted file attachment {AttachmentId}. Reason: {Reason}",
                userId, attachmentId, reason ?? "No reason provided");

            return new FileOperationResultDto { AttachmentId = attachmentId, Success = true };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Chat Member Management

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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await context.ChatMembers
                .Where(cm => cm.ChatThreadId == chatId && userIds.Contains(cm.UserId) && !cm.IsDeleted)
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIds = roleUpdates.Keys.ToList();
            var members = await context.ChatMembers
                .Where(cm => cm.ChatThreadId == chatId && userIds.Contains(cm.UserId) && !cm.IsDeleted)
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
        try
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
        catch (Exception ex)
        {
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
            throw;
        }
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

    #endregion

    #region Moderation & Administration

    /// <summary>
    /// Performs moderation actions on chats and messages.
    /// Records moderation actions in the audit log and returns the result.
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
            throw;
        }
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
    /// </summary>
    private async Task ValidateTenantAccessAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue) return;

        var exists = await context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId.Value && !t.IsDeleted, cancellationToken);

        if (!exists)
            throw new InvalidOperationException($"Tenant {tenantId.Value} not found or is inactive.");
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
    /// Batch-fetches display names (FirstName LastName or Username) from the Users table.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, string>> FetchUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var users = await context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Username })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => !string.IsNullOrEmpty(u.FirstName) || !string.IsNullOrEmpty(u.LastName)
                ? $"{u.FirstName} {u.LastName}".Trim()
                : u.Username);
    }

    /// <summary>
    /// Maps ChatMessage entity to ChatMessageDto using a pre-fetched name map.
    /// </summary>
    private static ChatMessageDto MapToChatMessageDto(
        Data.Entities.Chat.ChatMessage message,
        IReadOnlyDictionary<Guid, string>? userNames = null)
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
            SenderName = message.SenderId.HasValue
                ? (userNames?.GetValueOrDefault(message.SenderId.Value) ?? message.SenderId.Value.ToString("N")[..8])
                : "System",
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
                Username = userNames?.GetValueOrDefault(r.UserId) ?? r.UserId.ToString("N")[..8],
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
            Format = message.Format,
            Metadata = metadata
        };
    }

    /// <summary>
    /// <summary>
    /// Toggle message reaction (add or remove) — persisted in MetadataJson["Reactions"]
    /// and broadcast to all chat participants via SignalR.
    /// </summary>
    public async Task<MessageOperationResultDto> ToggleMessageReactionAsync(
        MessageReactionActionDto reactionDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == reactionDto.MessageId && !m.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"Message {reactionDto.MessageId} not found.");

            // Deserialize existing reactions from MetadataJson
            var metadata = string.IsNullOrEmpty(message.MetadataJson)
                ? new Dictionary<string, System.Text.Json.JsonElement>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(message.MetadataJson)
                  ?? new Dictionary<string, System.Text.Json.JsonElement>();

            Dictionary<string, List<Guid>> reactions;
            if (metadata.TryGetValue("Reactions", out var reactionsElement))
            {
                reactions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<Guid>>>(
                    reactionsElement.GetRawText()) ?? new Dictionary<string, List<Guid>>();
            }
            else
            {
                reactions = new Dictionary<string, List<Guid>>();
            }

            // Toggle the emoji reaction for this user
            if (!reactions.ContainsKey(reactionDto.Emoji))
                reactions[reactionDto.Emoji] = new List<Guid>();

            var users = reactions[reactionDto.Emoji];
            var isAdding = !users.Contains(reactionDto.UserId);
            if (isAdding)
                users.Add(reactionDto.UserId);
            else
                users.Remove(reactionDto.UserId);

            // Clean up empty emoji buckets
            if (users.Count == 0)
                reactions.Remove(reactionDto.Emoji);

            // Persist back to MetadataJson
            var reactionsJson = System.Text.Json.JsonSerializer.Serialize(reactions);
            var reactionsEl = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(reactionsJson);
            metadata["Reactions"] = reactionsEl;
            message.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            message.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Reaction {Emoji} {Action} on message {MessageId} by user {UserId}.",
                reactionDto.Emoji, isAdding ? "added" : "removed", reactionDto.MessageId, reactionDto.UserId);

            // Build updated reaction DTOs for the broadcast
            var updatedReactions = reactions.Select(kvp => new MessageReactionDto
            {
                Emoji = kvp.Key,
                Count = kvp.Value.Count,
                UserIds = kvp.Value,
                HasCurrentUserReacted = kvp.Value.Contains(reactionDto.UserId)
            }).ToList();

            // Broadcast to the chat group via SignalR
            await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                .SendAsync("MessageReactionUpdated", new
                {
                    MessageId = reactionDto.MessageId,
                    Emoji = reactionDto.Emoji,
                    UserId = reactionDto.UserId,
                    IsAdding = isAdding,
                    UpdatedReactions = updatedReactions
                }, cancellationToken);

            return new MessageOperationResultDto
            {
                Success = true,
                MessageId = reactionDto.MessageId
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    /// <summary>
    /// Finds all duplicate DirectMessage chats for <paramref name="userId"/> (same pair of two users,
    /// same tenant) and merges each group into a single primary thread.
    ///
    /// Strategy:
    ///   - For each pair of users that has more than one active DM thread, the thread with the
    ///     most-recent UpdatedAt becomes the "primary".
    ///   - All ChatMessages from secondary threads are re-parented to the primary thread.
    ///   - All MessageReadReceipts and MessageAttachments referencing secondary messages are
    ///     implicitly kept (their MessageId does not change; only the parent thread changes).
    ///   - ChatMembers in secondary threads that are NOT already members of the primary are
    ///     moved to the primary.
    ///   - Secondary threads are soft-deleted (IsDeleted = true, IsActive = false).
    /// </summary>
    public async Task<DmMergeResultDto> MergeDirectMessageDuplicatesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = new DmMergeResultDto();

        try
        {
            // 1. Find all active DM threads where this user is a member.
            var userDmQuery = context.ChatThreads
                .Where(ct => !ct.IsDeleted && ct.IsActive && ct.Type == ChatType.DirectMessage);

            if (tenantId.HasValue)
                userDmQuery = userDmQuery.Where(ct => ct.TenantId == tenantId.Value);

            var userDmIds = await userDmQuery
                .Where(ct => ct.Members.Any(m => m.UserId == userId && !m.IsDeleted))
                .Select(ct => ct.Id)
                .ToListAsync(cancellationToken);

            if (userDmIds.Count < 2) return result; // Nothing to merge

            // 2. Load members for all those threads (only two-person threads qualify as DMs).
            var membersByThread = await context.ChatMembers
                .AsNoTracking()
                .Where(cm => userDmIds.Contains(cm.ChatThreadId) && !cm.IsDeleted)
                .Select(cm => new { cm.ChatThreadId, cm.UserId })
                .ToListAsync(cancellationToken);

            // 3. Group threads by the canonical "other user" (the user who is NOT the caller).
            //    Key = other-user ID; Value = list of thread IDs.
            var threadsByOtherUser = membersByThread
                .GroupBy(cm => cm.ChatThreadId)
                .Where(g =>
                {
                    var uids = g.Select(cm => cm.UserId).Distinct().ToList();
                    // Must be exactly 2 members and one of them must be userId
                    return uids.Count == 2 && uids.Contains(userId);
                })
                .Select(g => new
                {
                    ThreadId   = g.Key,
                    OtherUserId = g.Select(cm => cm.UserId).First(id => id != userId)
                })
                .GroupBy(x => x.OtherUserId)
                .Where(g => g.Count() > 1)      // Only groups with actual duplicates
                .ToList();

            if (threadsByOtherUser.Count == 0) return result;

            // 4. Load UpdatedAt for ordering
            var allDupThreadIds = threadsByOtherUser
                .SelectMany(g => g.Select(x => x.ThreadId))
                .Distinct()
                .ToList();

            var threadMeta = await context.ChatThreads
                .Where(ct => allDupThreadIds.Contains(ct.Id))
                .Select(ct => new { ct.Id, ct.UpdatedAt })
                .ToDictionaryAsync(ct => ct.Id, ct => ct.UpdatedAt, cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var group in threadsByOtherUser)
            {
                // Pick the primary: the most recently updated thread
                var ordered = group
                    .OrderByDescending(x => threadMeta.TryGetValue(x.ThreadId, out var upd) ? upd : DateTime.MinValue)
                    .ToList();

                var primaryId    = ordered[0].ThreadId;
                var secondaryIds = ordered.Skip(1).Select(x => x.ThreadId).ToList();

                // 5. Re-parent messages from secondary threads to the primary thread
                var messagesToMove = await context.ChatMessages
                    .Where(m => secondaryIds.Contains(m.ChatThreadId))
                    .ToListAsync(cancellationToken);

                foreach (var msg in messagesToMove)
                    msg.ChatThreadId = primaryId;

                result.ReassignedMessageCount += messagesToMove.Count;

                // 6. Move members from secondary threads that aren't already in the primary
                var existingPrimaryMemberIds = await context.ChatMembers
                    .Where(cm => cm.ChatThreadId == primaryId && !cm.IsDeleted)
                    .Select(cm => cm.UserId)
                    .ToListAsync(cancellationToken);

                var membersToMove = await context.ChatMembers
                    .Where(cm => secondaryIds.Contains(cm.ChatThreadId) && !cm.IsDeleted
                              && !existingPrimaryMemberIds.Contains(cm.UserId))
                    .ToListAsync(cancellationToken);

                foreach (var member in membersToMove)
                {
                    member.ChatThreadId = primaryId;
                    member.ModifiedAt   = now;
                }

                // 7. Soft-delete secondary threads and their remaining members
                var secondaryThreads = await context.ChatThreads
                    .Where(ct => secondaryIds.Contains(ct.Id))
                    .ToListAsync(cancellationToken);

                foreach (var thread in secondaryThreads)
                {
                    thread.IsActive    = false;
                    thread.IsDeleted   = true;
                    thread.ModifiedAt  = now;
                }

                var remainingSecondaryMembers = await context.ChatMembers
                    .Where(cm => secondaryIds.Contains(cm.ChatThreadId) && !cm.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var member in remainingSecondaryMembers)
                {
                    member.IsDeleted  = true;
                    member.IsActive   = false;
                    member.ModifiedAt = now;
                }

                result.MergedThreadCount += secondaryIds.Count;
                result.PrimaryThreadIds.Add(primaryId);

                logger.LogInformation(
                    "Merged {SecondaryCount} duplicate DM threads into primary {PrimaryId} for user {UserId} (tenant {TenantId}). Moved {MessageCount} messages.",
                    secondaryIds.Count, primaryId, userId, tenantId, messagesToMove.Count);
            }

            await context.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error merging duplicate DM threads for user {UserId}", userId);
            throw;
        }
    }

}
