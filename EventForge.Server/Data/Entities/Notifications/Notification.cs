using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Notifications;

namespace EventForge.Server.Data.Entities.Notifications;

/// <summary>
/// Entity model for notifications supporting multi-tenant isolation and localization.
/// </summary>
[Table("Notifications")]
[Index(nameof(TenantId))]
[Index(nameof(Status))]
[Index(nameof(Type))]
[Index(nameof(Priority))]
[Index(nameof(ExpiresAt))]
[Index(nameof(CreatedAt))]
[Index(nameof(IsArchived))]
public class Notification : AuditableEntity
{
    /// <summary>
    /// ID of the user/system sending the notification.
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// Notification type for categorization and filtering.
    /// </summary>
    [Required]
    public NotificationTypes Type { get; set; }

    /// <summary>
    /// Priority level for display ordering and urgency.
    /// </summary>
    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Current status of the notification.
    /// </summary>
    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

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
    [Column("Locale")]
    public string? PayloadLocale { get; set; }

    /// <summary>
    /// Localization parameters for dynamic content substitution (JSON).
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? LocalizationParamsJson { get; set; }

    /// <summary>
    /// Optional expiration timestamp. Null for permanent notifications.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when notification was read by any recipient.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Timestamp when notification was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Timestamp when notification was silenced.
    /// </summary>
    public DateTime? SilencedAt { get; set; }

    /// <summary>
    /// Timestamp when notification was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Whether the notification is archived.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Additional metadata for extensibility and custom handling (JSON).
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Recipients of this notification.
    /// </summary>
    public virtual ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();

    /// <summary>
    /// Computed property to check if notification is expired.
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}