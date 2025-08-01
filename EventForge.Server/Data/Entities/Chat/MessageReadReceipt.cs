using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for message read receipts supporting multi-tenant isolation.
/// </summary>
[Table("MessageReadReceipts")]
[Index(nameof(TenantId))]
[Index(nameof(MessageId))]
[Index(nameof(UserId))]
[Index(nameof(ReadAt))]
public class MessageReadReceipt : AuditableEntity
{
    /// <summary>
    /// ID of the message that was read.
    /// </summary>
    [Required]
    public Guid MessageId { get; set; }

    /// <summary>
    /// ID of the user who read the message.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// When the message was read.
    /// </summary>
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the message.
    /// </summary>
    public virtual ChatMessage Message { get; set; } = null!;
}