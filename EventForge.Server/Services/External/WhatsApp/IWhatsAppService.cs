using EventForge.DTOs.External.WhatsApp;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Service responsible for processing incoming WhatsApp webhook events
/// and creating corresponding orders inside EventForge.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Verifies a Meta webhook subscription challenge.
    /// Returns the <paramref name="challenge"/> string when the <paramref name="verifyToken"/>
    /// matches the configured secret; otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="mode">Should be "subscribe" for a valid challenge.</param>
    /// <param name="verifyToken">Token sent by Meta to verify ownership.</param>
    /// <param name="challenge">Opaque string that must be echoed back on success.</param>
    string? VerifyWebhook(string mode, string verifyToken, string challenge);

    /// <summary>
    /// Processes an incoming WhatsApp webhook payload.
    /// Extracts order messages and creates the corresponding internal orders.
    /// </summary>
    /// <param name="payload">Deserialized webhook payload from Meta.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of <see cref="WhatsAppOrderReceivedDto"/> produced from the payload.</returns>
    Task<IReadOnlyList<WhatsAppOrderReceivedDto>> ProcessWebhookAsync(
        WhatsAppWebhookPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a text reply to a WhatsApp user via the Cloud API.
    /// </summary>
    /// <param name="toPhoneNumber">Recipient phone number (without "+").</param>
    /// <param name="messageText">Text to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTextMessageAsync(
        string toPhoneNumber,
        string messageText,
        CancellationToken cancellationToken = default);
}
