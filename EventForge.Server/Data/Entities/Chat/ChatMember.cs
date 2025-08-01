using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Chat;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for chat members supporting multi-tenant isolation.
/// </summary>
[Table("ChatMembers")]
[Index(nameof(TenantId))]
[Index(nameof(ChatThreadId))]
[Index(nameof(UserId))]
[Index(nameof(Role))]
[Index(nameof(JoinedAt))]
[Index(nameof(LastSeenAt))]
public class ChatMember : AuditableEntity
{
    /// <summary>
    /// ID of the chat thread.
    /// </summary>
    [Required]
    public Guid ChatThreadId { get; set; }

    /// <summary>
    /// ID of the user member.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Role of the member in the chat.
    /// </summary>
    [Required]
    public ChatMemberRole Role { get; set; } = ChatMemberRole.Member;

    /// <summary>
    /// When the user joined the chat.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user was seen in this chat.
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    /// Whether the user is currently online.
    /// </summary>
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// Whether the user has muted this chat.
    /// </summary>
    public bool IsMuted { get; set; } = false;

    /// <summary>
    /// Navigation property to the chat thread.
    /// </summary>
    public virtual ChatThread ChatThread { get; set; } = null!;
}