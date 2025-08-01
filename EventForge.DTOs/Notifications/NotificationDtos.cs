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
}