using EventForge.Server.Data.Entities.Chat;
using Prym.DTOs.AI;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Creates DocumentHeader+Rows from a completed AI order conversation session.
/// </summary>
public interface IWhatsAppOrderService
{
    /// <summary>
    /// Creates a DocumentHeader and its DocumentRows from the order draft stored in the session.
    /// </summary>
    /// <param name="session">The completed order conversation session.</param>
    /// <param name="tenantId">The tenant for which the document is created.</param>
    /// <param name="createdBy">User/system string for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created DocumentHeader.</returns>
    Task<Guid> CreateDocumentFromSessionAsync(
        OrderConversationSession session,
        Guid tenantId,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads or creates the <see cref="OrderConversationSession"/> for the given chat thread.
    /// </summary>
    Task<OrderConversationSession> GetOrCreateSessionAsync(
        Guid chatThreadId,
        Guid? businessPartyId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current draft and state to the session record.
    /// </summary>
    Task UpdateSessionAsync(
        OrderConversationSession session,
        OrderConversationState newState,
        string? draftJson,
        CancellationToken cancellationToken = default);
}
