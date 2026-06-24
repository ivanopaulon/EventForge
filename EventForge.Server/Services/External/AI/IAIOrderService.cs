using Prym.DTOs.AI;
using Prym.DTOs.Products;

namespace EventForge.Server.Services.External.AI;

/// <summary>
/// Core AI service for WhatsApp order processing.
/// </summary>
public interface IAIOrderService
{
    /// <summary>
    /// Classifies the intent of a customer message.
    /// </summary>
    /// <param name="message">The raw message text from the customer.</param>
    /// <param name="sessionContext">Serialised session context (prior conversation summary).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detected <see cref="MessageIntent"/>.</returns>
    Task<MessageIntent> ClassifyIntentAsync(string message, string? sessionContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts candidate order rows from a customer message matched against the given catalogue.
    /// </summary>
    /// <param name="message">The raw message text.</param>
    /// <param name="catalog">Available products for the current customer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of extracted <see cref="OrderDraftItem"/> rows.</returns>
    Task<List<OrderDraftItem>> ExtractOrderItemsAsync(string message, IList<ProductDto> catalog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a natural-language guidance response to send back to the customer.
    /// </summary>
    /// <param name="ctx">Current order draft context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Text to send to the customer via WhatsApp.</returns>
    Task<string> GenerateGuidanceResponseAsync(OrderDraftContext ctx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the next conversational step given the current session state.
    /// </summary>
    /// <param name="state">Current order conversation state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recommended next <see cref="OrderConversationState"/>.</returns>
    Task<OrderConversationState> GetNextStepAsync(OrderConversationState state, CancellationToken cancellationToken = default);
}
