using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Chat
{
    /// <summary>
    /// Enumeration of chat types.
    /// </summary>
    public enum ChatType
    {
        DirectMessage = 0,  // 1:1 chat
        Group = 1,          // Group chat
        Channel = 2         // Public channel (future extension)
    }

    /// <summary>
    /// Enumeration of message status.
    /// </summary>
    public enum MessageStatus
    {
        Pending = 0,
        Sent = 1,
        Delivered = 2,
        Read = 3,
        Failed = 4,
        Deleted = 5
    }

    /// <summary>
    /// Enumeration of chat member roles.
    /// </summary>
    public enum ChatMemberRole
    {
        Member = 0,
        Admin = 1,
        Moderator = 2,
        Owner = 3
    }

    /// <summary>
    /// Enumeration of media/file types.
    /// </summary>
    public enum MediaType
    {
        Document = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Archive = 4,
        Other = 99
    }

    /// <summary>
    /// Message formatting types.
    /// </summary>
    public enum MessageFormat
    {
        Plain = 0,
        Markdown = 1,
        Html = 2
    }

    /// <summary>
    /// Enumeration of chat operation types for rate limiting.
    /// </summary>
    public enum ChatOperationType
    {
        SendMessage = 0,
        CreateChat = 1,
        EditMessage = 2,
        DeleteMessage = 3,
        UploadFile = 4,
        AddMember = 5,
        RemoveMember = 6,
        UpdateChat = 7,
        JoinChat = 8,
        LeaveChat = 9
    }

    /// <summary>
    /// DTO for creating a new chat thread/conversation.
    /// </summary>
    public class CreateChatDto
    {
        /// <summary>
        /// Tenant ID for multi-tenant isolation.
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Type of chat (DM, Group, Channel).
        /// </summary>
        [Required]
        public ChatType Type { get; set; }

        /// <summary>
        /// Optional name for group chats. Auto-generated for DMs.
        /// </summary>
        [MaxLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// Optional description for groups/channels.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// List of initial participant user IDs.
        /// </summary>
        [Required]
        public List<Guid> ParticipantIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Creator/owner user ID.
        /// </summary>
        [Required]
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Whether the chat is private or discoverable.
        /// </summary>
        public bool IsPrivate { get; set; } = true;

        /// <summary>
        /// Preferred locale for this chat.
        /// </summary>
        [MaxLength(10)]
        public string? PreferredLocale { get; set; }
    }

    /// <summary>
    /// DTO for chat thread/conversation response.
    /// </summary>
    public class ChatResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public ChatType Type { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public string? PreferredLocale { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public List<ChatMemberDto> Members { get; set; } = new List<ChatMemberDto>();
        public ChatMessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for chat member information.
    /// </summary>
    public class ChatMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public ChatMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsOnline { get; set; }
        public bool IsMuted { get; set; }
    }

    /// <summary>
    /// DTO for sending a new message with enhanced features.
    /// </summary>
    public class SendMessageDto
    {
        /// <summary>
        /// Chat thread ID.
        /// </summary>
        [Required]
        public Guid ChatId { get; set; }

        /// <summary>
        /// Sender user ID.
        /// </summary>
        [Required]
        public Guid SenderId { get; set; }

        /// <summary>
        /// Message content (text).
        /// </summary>
        [MaxLength(4000)]
        public string? Content { get; set; }

        /// <summary>
        /// Optional message this is replying to (for threading).
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// Optional file/media attachments.
        /// </summary>
        public List<MessageAttachmentDto>? Attachments { get; set; }

        /// <summary>
        /// List of mentioned user IDs in this message.
        /// </summary>
        public List<Guid>? MentionedUserIds { get; set; }

        /// <summary>
        /// List of emojis/reactions used in this message.
        /// </summary>
        public List<string>? EmojiTags { get; set; }

        /// <summary>
        /// Message formatting type (plain, markdown, html).
        /// </summary>
        public MessageFormat Format { get; set; } = MessageFormat.Plain;

        /// <summary>
        /// Message locale for localization.
        /// </summary>
        [MaxLength(10)]
        public string? Locale { get; set; }

        /// <summary>
        /// Additional metadata for extensions.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for message attachment/media.
    /// </summary>
    public class MessageAttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public MediaType MediaType { get; set; }
        public string? FileUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid UploadedBy { get; set; }

        /// <summary>
        /// Additional metadata for media files (duration, dimensions, etc.)
        /// </summary>
        public Dictionary<string, object>? MediaMetadata { get; set; }
    }

    /// <summary>
    /// DTO for chat message response with enhanced features.
    /// </summary>
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatarUrl { get; set; }
        public string? Content { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public ChatMessageDto? ReplyToMessage { get; set; }
        public List<MessageAttachmentDto>? Attachments { get; set; }
        public MessageStatus Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public string? Locale { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// List of mentioned user IDs in this message.
        /// </summary>
        public List<Guid>? MentionedUserIds { get; set; }

        /// <summary>
        /// List of mentioned users with names for display.
        /// </summary>
        public List<MentionedUserDto>? MentionedUsers { get; set; }

        /// <summary>
        /// List of emojis/reactions used in this message.
        /// </summary>
        public List<string>? EmojiTags { get; set; }

        /// <summary>
        /// Message reactions from users.
        /// </summary>
        public List<MessageReactionDto>? Reactions { get; set; }

        /// <summary>
        /// Message formatting type.
        /// </summary>
        public MessageFormat Format { get; set; } = MessageFormat.Plain;

        /// <summary>
        /// List of users who have read this message (for group chats).
        /// </summary>
        public List<MessageReadReceiptDto>? ReadReceipts { get; set; }

        /// <summary>
        /// Count of thread replies to this message.
        /// </summary>
        public int ThreadReplyCount { get; set; }

        /// <summary>
        /// Latest reply in thread (if any).
        /// </summary>
        public ChatMessageDto? LatestThreadReply { get; set; }
    }

    /// <summary>
    /// DTO for mentioned users in messages.
    /// </summary>
    public class MentionedUserDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// DTO for message reactions.
    /// </summary>
    public class MessageReactionDto
    {
        public string Emoji { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        public bool HasCurrentUserReacted { get; set; }
    }

    /// <summary>
    /// DTO for adding/removing message reactions.
    /// </summary>
    public class MessageReactionActionDto
    {
        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Emoji { get; set; } = string.Empty;

        public bool IsAdding { get; set; } = true; // true = add, false = remove
    }

    /// <summary>
    /// DTO for message read receipts.
    /// </summary>
    public class MessageReadReceiptDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public DateTime ReadAt { get; set; }
    }

    /// <summary>
    /// DTO for user search/mention suggestions.
    /// </summary>
    public class UserMentionSuggestionDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Email { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActive { get; set; }
    }

    /// <summary>
    /// DTO for updating message status.
    /// </summary>
    public class UpdateMessageStatusDto
    {
        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public MessageStatus Status { get; set; }

        public Guid? UserId { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// DTO for editing a message.
    /// </summary>
    public class EditMessageDto
    {
        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public string? EditReason { get; set; }
    }

    /// <summary>
    /// DTO for adding/removing chat members.
    /// </summary>
    public class UpdateChatMembersDto
    {
        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public Guid ActionBy { get; set; }

        public List<Guid>? UsersToAdd { get; set; }
        public List<Guid>? UsersToRemove { get; set; }
        public Dictionary<Guid, ChatMemberRole>? RoleUpdates { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for chat search and filtering.
    /// </summary>
    public class ChatSearchDto
    {
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
        public List<ChatType>? Types { get; set; }
        public string? SearchTerm { get; set; }
        public bool? HasUnreadMessages { get; set; }
        public DateTime? LastActivityAfter { get; set; }
        public DateTime? LastActivityBefore { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "UpdatedAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for message search within chats.
    /// </summary>
    public class MessageSearchDto
    {
        public Guid? ChatId { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? SenderId { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IncludeDeleted { get; set; } = false;
        public MediaType? HasMediaType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "SentAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for chat moderation actions (SuperAdmin).
    /// </summary>
    public class ChatModerationActionDto
    {
        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public Guid ModeratorId { get; set; }

        public string Action { get; set; } = string.Empty; // "mute", "disable", "delete", "warn"
        public string? Reason { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool NotifyMembers { get; set; } = true;
    }

    /// <summary>
    /// DTO for chat statistics and analytics.
    /// </summary>
    public class ChatStatsDto
    {
        public Guid? TenantId { get; set; }
        public int TotalChats { get; set; }
        public int ActiveChats { get; set; }
        public int DirectMessageChats { get; set; }
        public int GroupChats { get; set; }
        public int TotalMessages { get; set; }
        public int MessagesLastWeek { get; set; }
        public int MessagesLastMonth { get; set; }
        public Dictionary<MediaType, int> MediaCountByType { get; set; } = new Dictionary<MediaType, int>();
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for user typing indicators.
    /// </summary>
    public class TypingIndicatorDto
    {
        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public string? Username { get; set; }
        public bool IsTyping { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for chat operation rate limiting status.
    /// </summary>
    public class ChatRateLimitStatusDto
    {
        public bool IsAllowed { get; set; }
        public int RemainingQuota { get; set; }
        public TimeSpan ResetTime { get; set; }
        public ChatOperationType OperationType { get; set; }
        public Dictionary<string, object> LimitDetails { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// DTO for chat deletion parameters.
    /// </summary>
    public class DeleteChatDto
    {
        /// <summary>
        /// Optional reason for deleting the chat.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Whether to perform soft delete (true) or hard delete (false).
        /// Default is true for data retention.
        /// </summary>
        public bool SoftDelete { get; set; } = true;
    }

    /// <summary>
    /// DTO for message deletion parameters.
    /// </summary>
    public class DeleteMessageDto
    {
        /// <summary>
        /// Optional reason for deleting the message.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Whether to perform soft delete (true) or hard delete (false).
        /// Default is true for data retention.
        /// </summary>
        public bool SoftDelete { get; set; } = true;
    }

    /// <summary>
    /// DTO for message edit request parameters.
    /// </summary>
    public class EditMessageRequestDto
    {
        /// <summary>
        /// Updated message content.
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional reason for editing the message.
        /// </summary>
        public string? EditReason { get; set; }
    }

    /// <summary>
    /// DTO for chat export request parameters.
    /// </summary>
    public class ChatExportRequestDto
    {
        /// <summary>
        /// Optional tenant filter for export.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Optional chat filter for export.
        /// </summary>
        public Guid? ChatId { get; set; }

        /// <summary>
        /// Optional user filter for export.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Start date for export range.
        /// </summary>
        [Required]
        public DateTime FromDate { get; set; }

        /// <summary>
        /// End date for export range.
        /// </summary>
        [Required]
        public DateTime ToDate { get; set; }

        /// <summary>
        /// Export format (JSON, CSV, Excel, PDF).
        /// </summary>
        [Required]
        public string Format { get; set; } = "JSON";

        /// <summary>
        /// Optional chat types to include in export.
        /// </summary>
        public List<ChatType>? ChatTypes { get; set; }

        /// <summary>
        /// Whether to include deleted messages in export.
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;

        /// <summary>
        /// Whether to include file attachments in export.
        /// </summary>
        public bool IncludeAttachments { get; set; } = true;

        /// <summary>
        /// Optional search term to filter messages.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Maximum number of records to export (for performance).
        /// </summary>
        [Range(1, 100000)]
        public int? MaxRecords { get; set; }
    }

    /// <summary>
    /// DTO for chat export operation result.
    /// </summary>
    public class ChatExportResultDto
    {
        /// <summary>
        /// Unique export operation identifier.
        /// </summary>
        public Guid ExportId { get; set; }

        /// <summary>
        /// Current export status (Preparing, Processing, Completed, Failed).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Export format used.
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Number of records processed/exported.
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// Size of the generated export file in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// URL to check export status.
        /// </summary>
        public string? StatusUrl { get; set; }

        /// <summary>
        /// URL to download the export file (available when completed).
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Export file expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Estimated completion time (for in-progress exports).
        /// </summary>
        public DateTime? EstimatedCompletionTime { get; set; }

        /// <summary>
        /// Export creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Export completion timestamp.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if export failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional export metadata.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}