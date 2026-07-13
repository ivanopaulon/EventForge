using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

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

    /// <summary>
    /// Retrieves all chat messages with pagination.
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Retrieves messages for a specific conversation with pagination.
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetMessagesByConversationAsync(
        Guid conversationId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Retrieves unread messages for the current user with pagination.
    /// NOTE: Requires current user context to be passed from controller
    /// </summary>
    public async Task<PagedResult<ChatMessageDto>> GetUnreadMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Gets a specific message by ID with access validation.
    /// </summary>
    public async Task<ChatMessageDto?> GetMessageByIdAsync(
        Guid messageId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Edits an existing message with validation and change tracking.
    /// </summary>
    public async Task<ChatMessageDto> EditMessageAsync(
        EditMessageDto editDto,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Deletes a message with soft/hard delete options.
    /// </summary>
    public async Task<MessageOperationResultDto> DeleteMessageAsync(
        Guid messageId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Find message by messageId
        var message = await context.ChatMessages
            .Include(m => m.ChatThread)
                .ThenInclude(ct => ct.Members)
            .FirstOrDefaultAsync(m => m.Id == messageId && (tenantId == null || m.TenantId == tenantId.Value), cancellationToken);

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

}
