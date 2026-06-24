using Prym.DTOs.AI;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Chat;

/// <summary>
/// Tracks the AI-driven order conversation state for a WhatsApp chat thread.
/// One session per ChatThread; state resets when a new order cycle begins.
/// </summary>
[Table("OrderConversationSessions")]
public class OrderConversationSession : AuditableEntity
{
    /// <summary>FK to the WhatsApp chat thread driving this conversation.</summary>
    [Required]
    public Guid ChatThreadId { get; set; }

    /// <summary>Navigation property to the chat thread.</summary>
    public virtual ChatThread? ChatThread { get; set; }

    /// <summary>FK to the associated BusinessParty (may be null if the number is unrecognised).</summary>
    public Guid? BusinessPartyId { get; set; }

    /// <summary>Current state of the AI order wizard. Uses <see cref="OrderConversationState"/> from Prym.DTOs.AI.</summary>
    [Required]
    public OrderConversationState State { get; set; } = OrderConversationState.Idle;

    /// <summary>Serialised JSON of the current order draft (list of candidate rows).</summary>
    [MaxLength(16000)]
    public string? DraftJson { get; set; }

    /// <summary>When the last AI prompt was sent (rate-limit tracking).</summary>
    public DateTime? LastAiPromptAt { get; set; }

    /// <summary>ID of the DocumentHeader created when the session reaches Completed.</summary>
    public Guid? CreatedDocumentHeaderId { get; set; }

    /// <summary>Number of AI rounds in the current session (used for usage tracking).</summary>
    public int AiRoundCount { get; set; } = 0;
}
