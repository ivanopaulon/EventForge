using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.AI;

// ─── Enums ────────────────────────────────────────────────────────────────────

/// <summary>Classified intent of a customer message.</summary>
public enum MessageIntent
{
    Ordine = 0,
    Domanda = 1,
    Conferma = 2,
    Annullamento = 3,
    Saluto = 4,
    Altro = 5
}

/// <summary>State of an AI order conversation session.</summary>
public enum OrderConversationState
{
    Idle = 0,
    CollectingItems = 1,
    ConfirmingOrder = 2,
    Completed = 3,
    Cancelled = 4
}

// ─── Conversation session ─────────────────────────────────────────────────────

/// <summary>DTO for <c>OrderConversationSession</c>.</summary>
public class OrderConversationSessionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChatThreadId { get; set; }
    public Guid? BusinessPartyId { get; set; }
    public string? BusinessPartyName { get; set; }
    public OrderConversationState State { get; set; }
    public string? DraftJson { get; set; }
    public DateTime? LastAiPromptAt { get; set; }
    public Guid? CreatedDocumentHeaderId { get; set; }
    public int AiRoundCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

// ─── AI settings ─────────────────────────────────────────────────────────────

/// <summary>DTO for <c>AIOrderSettings</c>.</summary>
public class AIOrderSettingsDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? SystemPromptTemplate { get; set; }
    public int MaxItemsPerOrder { get; set; }
    public bool RequireConfirmation { get; set; }
    public bool AutoCreateDocument { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? OrderConfirmationTemplate { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AmbiguousProductMessage { get; set; }
    public bool EnableAI { get; set; }
    public int MaxTokensPerDay { get; set; }
}

// ─── Order draft ──────────────────────────────────────────────────────────────

/// <summary>A single candidate order row extracted by the AI.</summary>
public class OrderDraftItem
{
    /// <summary>Raw product name/code mentioned by the customer.</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>Matched product ID (null if not uniquely matched).</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Matched product code.</summary>
    public string? ProductCode { get; set; }

    /// <summary>Human-readable product name used in responses.</summary>
    public string? ProductName { get; set; }

    /// <summary>Requested quantity.</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>Unit of measure string (may be null).</summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>Unit price resolved from the customer's price list (null if not resolved).</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>True when the product is ambiguous (multiple matches found).</summary>
    public bool IsAmbiguous { get; set; }

    /// <summary>Possible alternative product names when ambiguous.</summary>
    public List<string> Suggestions { get; set; } = [];

    /// <summary>True when the product was not found in the catalogue at all.</summary>
    public bool IsNotFound { get; set; }
}

/// <summary>Full order draft maintained during a conversation session.</summary>
public class OrderDraftContext
{
    public Guid ChatThreadId { get; set; }
    public Guid? BusinessPartyId { get; set; }
    public string? BusinessPartyName { get; set; }
    public OrderConversationState State { get; set; }
    public List<OrderDraftItem> Items { get; set; } = [];
    public string? LastCustomerMessage { get; set; }
    public string? ConversationLocale { get; set; } = "it";
}

// ─── Simulation ───────────────────────────────────────────────────────────────

/// <summary>Request body for the <c>POST /api/v1/whatsapp/simulate-inbound</c> endpoint.</summary>
public class SimulateInboundDto
{
    /// <summary>Phone number to simulate (will be normalised like a real webhook number).</summary>
    [Required]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Text body of the simulated message.</summary>
    [Required]
    [MaxLength(4096)]
    public string MessageText { get; set; } = string.Empty;

    /// <summary>Optional: pin the conversation to a specific BusinessParty for testing.</summary>
    public Guid? BusinessPartyId { get; set; }
}

/// <summary>Result returned by the simulate-inbound endpoint.</summary>
public class SimulateInboundResultDto
{
    public Guid ChatThreadId { get; set; }
    public MessageIntent DetectedIntent { get; set; }
    public OrderConversationState SessionState { get; set; }
    public string? AiResponse { get; set; }
    public List<OrderDraftItem> CurrentDraft { get; set; } = [];
    public int TokensUsed { get; set; }
    public string? RawAiResponse { get; set; }
    public bool DocumentCreated { get; set; }
    public Guid? DocumentHeaderId { get; set; }
}

// ─── AI usage log ─────────────────────────────────────────────────────────────

/// <summary>DTO for AI usage log entries (read-only, for admin dashboards).</summary>
public class AIUsageLogDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ChatThreadId { get; set; }
    public string? ModelUsed { get; set; }
    public int TokensUsed { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public string? CallType { get; set; }
    public DateTime CallAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
