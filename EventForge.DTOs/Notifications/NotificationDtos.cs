using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Notifications
{
    /// <summary>
    /// Enumeration of notification priorities.
    /// </summary>
    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Enumeration of notification types.
    /// </summary>
    public enum NotificationTypes
    {
        System = 0,
        Event = 1,
        User = 2,
        Security = 3,
        Audit = 4,
        Marketing = 5
    }

    /// <summary>
    /// Enumeration of notification status.
    /// </summary>
    public enum NotificationStatus
    {
        Pending = 0,
        Sent = 1,
        Delivered = 2,
        Read = 3,
        Acknowledged = 4,
        Silenced = 5,
        Archived = 6,
        Expired = 7
    }

    /// <summary>
    /// DTO for creating a new notification.
    /// </summary>
    public class CreateNotificationDto
    {
        /// <summary>
        /// Tenant ID for multi-tenant isolation. Null for system-wide notifications.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// ID of the user/system sending the notification.
        /// </summary>
        public Guid? SenderId { get; set; }

        /// <summary>
        /// List of recipient user IDs. Empty for broadcast notifications.
        /// </summary>
        public List<Guid> RecipientIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Notification type for categorization and filtering.
        /// </summary>
        [Required]
        public NotificationTypes Type { get; set; }

        /// <summary>
        /// Priority level for display ordering and urgency.
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Localized notification payload with title, message, and metadata.
        /// </summary>
        [Required]
        public NotificationPayloadDto Payload { get; set; } = new NotificationPayloadDto();

        /// <summary>
        /// Optional expiration timestamp. Null for permanent notifications.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Additional metadata for extensibility and custom handling.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for notification payload with localization support.
    /// </summary>
    public class NotificationPayloadDto
    {
        /// <summary>
        /// Title of the notification (localization key or direct text).
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Message body (localization key or direct text).
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional action URL for clickable notifications.
        /// </summary>
        [MaxLength(500)]
        public string? ActionUrl { get; set; }

        /// <summary>
        /// Optional icon or image URL for rich notifications.
        /// </summary>
        [MaxLength(500)]
        public string? IconUrl { get; set; }

        /// <summary>
        /// Language/locale for this payload (e.g., "en-US", "it-IT").
        /// </summary>
        [MaxLength(10)]
        public string? Locale { get; set; }

        /// <summary>
        /// Localization parameters for dynamic content substitution.
        /// </summary>
        public Dictionary<string, string>? LocalizationParams { get; set; }

        /// <summary>
        /// Rich content attachments (files, images, links)
        /// </summary>
        public List<NotificationAttachmentDto>? Attachments { get; set; }

        /// <summary>
        /// Contextual actions that users can perform on this notification
        /// </summary>
        public List<NotificationActionDto>? Actions { get; set; }

        /// <summary>
        /// Avatar information for sender display
        /// </summary>
        public NotificationAvatarDto? Avatar { get; set; }

        /// <summary>
        /// Group identifier for related notifications
        /// </summary>
        [MaxLength(100)]
        public string? GroupId { get; set; }

        /// <summary>
        /// Tags for categorization and filtering
        /// </summary>
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// DTO for notification response/display.
    /// </summary>
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? SenderId { get; set; }
        public string? SenderName { get; set; }
        public List<Guid> RecipientIds { get; set; } = new List<Guid>();
        public NotificationTypes Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationPayloadDto Payload { get; set; } = new NotificationPayloadDto();
        public NotificationStatus Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? SilencedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for updating notification status (ack, silence, archive).
    /// </summary>
    public class UpdateNotificationStatusDto
    {
        [Required]
        public Guid NotificationId { get; set; }

        [Required]
        public NotificationStatus Status { get; set; }

        /// <summary>
        /// Optional user ID performing the action (for audit purposes).
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Optional reason/note for the status change.
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for bulk notification operations.
    /// </summary>
    public class BulkNotificationActionDto
    {
        [Required]
        public List<Guid> NotificationIds { get; set; } = new List<Guid>();

        [Required]
        public NotificationStatus Action { get; set; }

        public Guid? UserId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for notification preferences per user/tenant.
    /// </summary>
    public class NotificationPreferencesDto
    {
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Whether notifications are enabled for this user.
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Minimum priority level to receive notifications.
        /// </summary>
        public NotificationPriority MinPriority { get; set; } = NotificationPriority.Low;

        /// <summary>
        /// Notification types this user wants to receive.
        /// </summary>
        public List<NotificationTypes> EnabledTypes { get; set; } = new List<NotificationTypes>();

        /// <summary>
        /// Preferred locale for localized notifications.
        /// </summary>
        [MaxLength(10)]
        public string PreferredLocale { get; set; } = "en-US";

        /// <summary>
        /// Whether to play sound for notifications.
        /// </summary>
        public bool SoundEnabled { get; set; } = true;

        /// <summary>
        /// Auto-archive notifications after specified days.
        /// </summary>
        public int? AutoArchiveAfterDays { get; set; }
    }

    /// <summary>
    /// DTO for notification search and filtering.
    /// </summary>
    public class NotificationSearchDto
    {
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
        public List<NotificationTypes>? Types { get; set; }
        public List<NotificationPriority>? Priorities { get; set; }
        public List<NotificationStatus>? Statuses { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool IncludeExpired { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for notification statistics and analytics.
    /// </summary>
    public class NotificationStatsDto
    {
        public Guid? TenantId { get; set; }
        public int TotalNotifications { get; set; }
        public int UnreadCount { get; set; }
        public int AcknowledgedCount { get; set; }
        public int SilencedCount { get; set; }
        public int ArchivedCount { get; set; }
        public int ExpiredCount { get; set; }
        public Dictionary<NotificationTypes, int> CountByType { get; set; } = new Dictionary<NotificationTypes, int>();
        public Dictionary<NotificationPriority, int> CountByPriority { get; set; } = new Dictionary<NotificationPriority, int>();
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for notification attachments (files, images, links)
    /// </summary>
    public class NotificationAttachmentDto
    {
        /// <summary>
        /// Unique identifier for the attachment
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name for the attachment
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the attachment
        /// </summary>
        [MaxLength(100)]
        public string? MimeType { get; set; }

        /// <summary>
        /// Size of the attachment in bytes
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// URL to download or view the attachment
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Thumbnail URL for images/videos
        /// </summary>
        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Type of attachment for UI rendering
        /// </summary>
        public AttachmentType Type { get; set; }
    }

    /// <summary>
    /// Enumeration for attachment types
    /// </summary>
    public enum AttachmentType
    {
        File = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Document = 4,
        Link = 5,
        Other = 6
    }

    /// <summary>
    /// DTO for notification actions that users can perform
    /// </summary>
    public class NotificationActionDto
    {
        /// <summary>
        /// Unique identifier for the action
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display label for the action button
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// URL to navigate to when action is clicked
        /// </summary>
        [MaxLength(500)]
        public string? Url { get; set; }

        /// <summary>
        /// API endpoint to call when action is clicked
        /// </summary>
        [MaxLength(500)]
        public string? ApiEndpoint { get; set; }

        /// <summary>
        /// HTTP method for API calls (GET, POST, PUT, DELETE)
        /// </summary>
        [MaxLength(10)]
        public string? HttpMethod { get; set; } = "GET";

        /// <summary>
        /// Icon to display with the action
        /// </summary>
        [MaxLength(100)]
        public string? Icon { get; set; }

        /// <summary>
        /// Style/color of the action button
        /// </summary>
        public ActionStyle Style { get; set; } = ActionStyle.Default;

        /// <summary>
        /// Whether this action requires confirmation
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Confirmation message if RequiresConfirmation is true
        /// </summary>
        [MaxLength(200)]
        public string? ConfirmationMessage { get; set; }
    }

    /// <summary>
    /// Enumeration for action button styles
    /// </summary>
    public enum ActionStyle
    {
        Default = 0,
        Primary = 1,
        Secondary = 2,
        Success = 3,
        Warning = 4,
        Error = 5,
        Info = 6
    }

    /// <summary>
    /// DTO for avatar information in notifications
    /// </summary>
    public class NotificationAvatarDto
    {
        /// <summary>
        /// Display name for the avatar
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// URL to the avatar image
        /// </summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Initials to display if no image is available
        /// </summary>
        [MaxLength(5)]
        public string? Initials { get; set; }

        /// <summary>
        /// Background color for the avatar (hex color)
        /// </summary>
        [MaxLength(7)]
        public string? BackgroundColor { get; set; }

        /// <summary>
        /// Text color for the avatar (hex color)
        /// </summary>
        [MaxLength(7)]
        public string? TextColor { get; set; }
    }

    /// <summary>
    /// DTO for activity feed entries
    /// </summary>
    public class ActivityFeedEntryDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public NotificationAvatarDto? UserAvatar { get; set; }

        /// <summary>
        /// Type of activity (notification, event, chat, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// Action performed (created, updated, deleted, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Title of the activity
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the activity
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// URL to view the related item
        /// </summary>
        [MaxLength(500)]
        public string? Url { get; set; }

        /// <summary>
        /// Icon representing the activity
        /// </summary>
        [MaxLength(100)]
        public string? Icon { get; set; }

        /// <summary>
        /// Color theme for the activity
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Tags for categorization
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// When the activity occurred
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for notification grouping information
    /// </summary>
    public class NotificationGroupDto
    {
        /// <summary>
        /// Group identifier
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>
        /// Group title for display
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Number of notifications in this group
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Number of unread notifications in this group
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Latest notification in the group
        /// </summary>
        public NotificationResponseDto? LatestNotification { get; set; }

        /// <summary>
        /// Preview of notifications in the group
        /// </summary>
        public List<NotificationResponseDto>? PreviewNotifications { get; set; }

        /// <summary>
        /// Whether the group is collapsed
        /// </summary>
        public bool IsCollapsed { get; set; } = true;

        /// <summary>
        /// Group creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}