using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Chat;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for chat threads/conversations supporting multi-tenant isolation.
/// </summary>
[Table("ChatThreads")]
[Index(nameof(TenantId))]
[Index(nameof(Type))]
[Index(nameof(IsPrivate))]
[Index(nameof(CreatedAt))]
[Index(nameof(UpdatedAt))]
public class ChatThread : AuditableEntity
{
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
    /// Whether the chat is private or discoverable.
    /// </summary>
    public bool IsPrivate { get; set; } = true;

    /// <summary>
    /// Preferred locale for this chat.
    /// </summary>
    [MaxLength(10)]
    public string? PreferredLocale { get; set; }

    /// <summary>
    /// Last update timestamp for ordering purposes.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Members of this chat thread.
    /// </summary>
    public virtual ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();

    /// <summary>
    /// Messages in this chat thread.
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}