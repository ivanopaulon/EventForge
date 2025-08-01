using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Chat;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for message attachments supporting multi-tenant isolation.
/// </summary>
[Table("MessageAttachments")]
[Index(nameof(TenantId))]
[Index(nameof(MessageId))]
[Index(nameof(MediaType))]
[Index(nameof(UploadedAt))]
[Index(nameof(UploadedBy))]
public class MessageAttachment : AuditableEntity
{
    /// <summary>
    /// ID of the message this attachment belongs to.
    /// </summary>
    [Required]
    public Guid MessageId { get; set; }

    /// <summary>
    /// File name as stored on server.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Original file name as uploaded by user.
    /// </summary>
    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Type of media/file.
    /// </summary>
    [Required]
    public MediaType MediaType { get; set; }

    /// <summary>
    /// URL to access the file.
    /// </summary>
    [MaxLength(500)]
    public string? FileUrl { get; set; }

    /// <summary>
    /// URL to access a thumbnail (for images/videos).
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who uploaded the file.
    /// </summary>
    [Required]
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Additional metadata for media files (duration, dimensions, etc.) - JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MediaMetadataJson { get; set; }

    /// <summary>
    /// Navigation property to the message.
    /// </summary>
    public virtual ChatMessage Message { get; set; } = null!;
}