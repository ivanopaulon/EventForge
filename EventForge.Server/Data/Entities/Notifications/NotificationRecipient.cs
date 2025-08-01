using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Data.Entities.Notifications;

/// <summary>
/// Entity model for notification recipients supporting multi-tenant isolation.
/// </summary>
[Table("NotificationRecipients")]
[Index(nameof(TenantId))]
[Index(nameof(NotificationId))]
[Index(nameof(RecipientUserId))]
[Index(nameof(Status))]
[Index(nameof(ReadAt))]
public class NotificationRecipient : AuditableEntity
{
    /// <summary>
    /// ID of the notification.
    /// </summary>
    [Required]
    public Guid NotificationId { get; set; }

    /// <summary>
    /// ID of the recipient user.
    /// </summary>
    [Required]
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Recipient-specific status of the notification.
    /// </summary>
    [Required]
    public DTOs.Notifications.NotificationStatus Status { get; set; } = DTOs.Notifications.NotificationStatus.Pending;

    /// <summary>
    /// Timestamp when this recipient read the notification.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Timestamp when this recipient acknowledged the notification.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Timestamp when this recipient silenced the notification.
    /// </summary>
    public DateTime? SilencedAt { get; set; }

    /// <summary>
    /// Timestamp when this recipient archived the notification.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Whether this recipient has archived the notification.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Navigation property to the notification.
    /// </summary>
    public virtual Notification Notification { get; set; } = null!;
}