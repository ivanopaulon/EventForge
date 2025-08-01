using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Chat;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for chat messages supporting multi-tenant isolation and localization.
/// </summary>
[Table("ChatMessages")]
[Index(nameof(TenantId))]
[Index(nameof(ChatThreadId))]
[Index(nameof(SenderId))]
[Index(nameof(Status))]
[Index(nameof(SentAt))]
[Index(nameof(ReplyToMessageId))]
[Index(nameof(IsDeleted))]
public class ChatMessage : AuditableEntity
{
    /// <summary>
    /// ID of the chat thread this message belongs to.
    /// </summary>
    [Required]
    public Guid ChatThreadId { get; set; }

    /// <summary>
    /// ID of the user who sent the message.
    /// </summary>
    [Required]
    public Guid SenderId { get; set; }

    /// <summary>
    /// Message content (text).
    /// </summary>
    [MaxLength(4000)]
    public string? Content { get; set; }

    /// <summary>
    /// Optional message this is replying to.
    /// </summary>
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>
    /// Current status of the message.
    /// </summary>
    [Required]
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the message was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When the message was edited.
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// When the message was deleted.
    /// </summary>
    public new DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Whether the message has been edited.
    /// </summary>
    public bool IsEdited { get; set; } = false;

    /// <summary>
    /// Message locale for localization.
    /// </summary>
    [MaxLength(10)]
    public string? Locale { get; set; }

    /// <summary>
    /// Additional metadata for extensions (JSON).
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Navigation property to the chat thread.
    /// </summary>
    public virtual ChatThread ChatThread { get; set; } = null!;

    /// <summary>
    /// Navigation property to the message being replied to.
    /// </summary>
    public virtual ChatMessage? ReplyToMessage { get; set; }

    /// <summary>
    /// Messages that reply to this message.
    /// </summary>
    public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// File/media attachments for this message.
    /// </summary>
    public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();

    /// <summary>
    /// Read receipts for this message.
    /// </summary>
    public virtual ICollection<MessageReadReceipt> ReadReceipts { get; set; } = new List<MessageReadReceipt>();
}