using Prym.DTOs.AI;
using Prym.DTOs.Products;

namespace EventForge.Server.Services.External.AI;

/// <summary>
/// Builds the AI system prompt and context for a given customer/conversation.
/// </summary>
public interface IOrderAIContextBuilder
{
    /// <summary>
    /// Builds the full <see cref="OrderDraftContext"/> for a conversation, enriched with
    /// BusinessParty data, catalogue, and order history.
    /// </summary>
    Task<OrderDraftContext> BuildContextAsync(
        Guid chatThreadId,
        Guid tenantId,
        string? draftJson,
        string? lastCustomerMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the product catalogue available for the given BusinessParty (uses forced price list
    /// when configured, otherwise returns the default catalogue).
    /// </summary>
    Task<List<ProductDto>> GetCatalogForPartyAsync(
        Guid? businessPartyId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the system-prompt string to pass to the AI model, incorporating tenant instructions,
    /// customer info, catalogue summary, and recent order history.
    /// </summary>
    Task<string> BuildSystemPromptAsync(
        OrderDraftContext ctx,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
