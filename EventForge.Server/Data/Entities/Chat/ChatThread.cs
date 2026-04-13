using Prym.DTOs.Chat;
using EventForge.Server.Data.Entities.Business;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Entity model for chat threads/conversations supporting multi-tenant isolation.
/// Covers both internal operator chats (DM/Group/Channel) and external WhatsApp conversations.
/// </summary>
[Table("ChatThreads")]
[Index(nameof(TenantId))]
[Index(nameof(Type))]
[Index(nameof(IsPrivate))]
[Index(nameof(CreatedAt))]
[Index(nameof(UpdatedAt))]
[Index(nameof(ExternalPhoneNumber))]
public class ChatThread : AuditableEntity
{
    /// <summary>
    /// Type of chat (DM, Group, Channel, WhatsApp).
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

    // -------------------------------------------------------------------------
    // WhatsApp-specific fields (null for Type != WhatsApp)
    // -------------------------------------------------------------------------

    /// <summary>External phone number for WhatsApp conversations (E.164 format, digits only).</summary>
    [MaxLength(30)]
    public string? ExternalPhoneNumber { get; set; }

    /// <summary>Optional FK to a recognised BusinessParty (WhatsApp conversations only).</summary>
    public Guid? BusinessPartyId { get; set; }

    /// <summary>True when the WhatsApp number has no matching BusinessParty in the registry.</summary>
    public bool IsUnrecognizedNumber { get; set; } = false;

    /// <summary>Last known conversation status for WhatsApp threads.</summary>
    public WhatsAppConversationStatus? WhatsAppLastStatus { get; set; }

    // -------------------------------------------------------------------------
    // Navigation properties
    // -------------------------------------------------------------------------

    /// <summary>Members of this chat thread (internal chats).</summary>
    public virtual ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();

    /// <summary>Messages in this chat thread.</summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>Associated BusinessParty for WhatsApp threads.</summary>
    public virtual BusinessParty? BusinessParty { get; set; }
}