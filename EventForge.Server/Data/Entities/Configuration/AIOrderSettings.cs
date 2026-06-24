using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Per-tenant configuration for the AI-powered WhatsApp order assistant.
/// </summary>
[Table("AIOrderSettings")]
public class AIOrderSettings : AuditableEntity
{
    // ─── AI behaviour ─────────────────────────────────────────────────────────

    /// <summary>Custom system prompt template injected before the per-customer context.
    /// Supports placeholders: {BusinessPartyName}, {Catalog}, {OrderHistory}.</summary>
    [MaxLength(4000)]
    public string? SystemPromptTemplate { get; set; }

    /// <summary>Maximum number of order items the AI is allowed to collect per order.</summary>
    public int MaxItemsPerOrder { get; set; } = 50;

    /// <summary>When true, the bot sends a summary and waits for explicit confirmation before creating the document.</summary>
    public bool RequireConfirmation { get; set; } = true;

    /// <summary>When true, a DocumentHeader+Rows is automatically created when the session reaches Completed.</summary>
    public bool AutoCreateDocument { get; set; } = true;

    // ─── Message templates ────────────────────────────────────────────────────

    /// <summary>Welcome message sent when a new conversation is started.</summary>
    [MaxLength(1000)]
    public string? WelcomeMessage { get; set; }

    /// <summary>Template for the order confirmation summary sent to the customer.
    /// Supports {OrderLines}, {Total}.</summary>
    [MaxLength(2000)]
    public string? OrderConfirmationTemplate { get; set; }

    /// <summary>Message sent when a generic error occurs.</summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>Message sent when a product is ambiguous or not found.
    /// Supports {ProductName}, {Suggestions}.</summary>
    [MaxLength(500)]
    public string? AmbiguousProductMessage { get; set; }

    // ─── Feature flags ────────────────────────────────────────────────────────

    /// <summary>Enables or disables the AI assistant for this tenant.</summary>
    public bool EnableAI { get; set; } = false;

    /// <summary>Maximum AI tokens allowed per day per tenant (0 = unlimited).</summary>
    public int MaxTokensPerDay { get; set; } = 0;
}
