using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Audit;

/// <summary>
/// Records every AI API call for cost tracking and rate-limiting.
/// </summary>
[Table("AIUsageLogs")]
public class AIUsageLog : AuditableEntity
{
    /// <summary>FK to the chat thread that triggered this AI call (nullable for non-chat calls).</summary>
    public Guid? ChatThreadId { get; set; }

    /// <summary>AI model used (e.g. "gpt-4o").</summary>
    [MaxLength(100)]
    public string? ModelUsed { get; set; }

    /// <summary>Total tokens consumed by the call (prompt + completion).</summary>
    public int TokensUsed { get; set; }

    /// <summary>Prompt tokens only.</summary>
    public int PromptTokens { get; set; }

    /// <summary>Completion tokens only.</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Estimated cost in USD (calculated from token counts).</summary>
    public decimal? EstimatedCostUsd { get; set; }

    /// <summary>Type of AI call (e.g. "ClassifyIntent", "ExtractOrderItems", "GenerateGuidance").</summary>
    [MaxLength(100)]
    public string? CallType { get; set; }

    /// <summary>UTC timestamp when the call was made.</summary>
    public DateTime CallAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether the call succeeded.</summary>
    public bool Success { get; set; } = true;

    /// <summary>Error message if the call failed.</summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
