using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
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
public partial class ChatService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<ChatService> logger,
    IHubContext<ChatHub> hubContext,
    IWebHostEnvironment environment,
    IMemoryCache memoryCache,
    IOnlineUserTracker onlineUserTracker,
    IHtmlSanitizerService htmlSanitizerService) : IChatService
{

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
    /// Validates rate limiting before chat operations using in-memory sliding-window counters.
    /// Throws <see cref="InvalidOperationException"/> when the per-hour quota is exceeded.
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
    /// Toggle message reaction (add or remove) — persisted in MetadataJson["Reactions"]
    /// and broadcast to all chat participants via SignalR.
    /// </summary>
    public async Task<MessageOperationResultDto> ToggleMessageReactionAsync(
        MessageReactionActionDto reactionDto,
        CancellationToken cancellationToken = default)
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

    /// <inheritdoc/>
    public async Task<MessageOperationResultDto> ReportMessageAsync(
        Guid messageId,
        ReportMessageDto dto,
        string reporterUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted, cancellationToken);

            if (message is null)
            {
                return new MessageOperationResultDto
                {
                    Success = false,
                    ErrorMessage = "Message not found.",
                    MessageId = messageId
                };
            }

            if (message.IsFlagged)
            {
                return new MessageOperationResultDto
                {
                    Success = true,
                    MessageId = messageId
                };
            }

            message.IsFlagged = true;
            message.FlaggedAt = DateTime.UtcNow;
            message.FlaggedBy = reporterUserId;
            message.FlagReason = dto.Reason?.Trim();
            message.ModifiedAt = DateTime.UtcNow;
            message.ModifiedBy = reporterUserId;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Message {MessageId} flagged by user {UserId}. Reason: {Reason}",
                messageId, reporterUserId, dto.Reason);

            return new MessageOperationResultDto
            {
                Success = true,
                MessageId = messageId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reporting message {MessageId}", messageId);
            throw;
        }
    }

    #endregion

}
