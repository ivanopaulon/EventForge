namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Service for sending messages via WhatsApp Cloud API.
/// </summary>
public interface IWhatsAppService
{
    Task<string?> SendTextMessageAsync(string toPhone, string text, CancellationToken cancellationToken = default);
    Task SendReadReceiptAsync(string toPhone, string messageId, CancellationToken cancellationToken = default);
    /// <summary>Verifies connectivity to the Meta Graph API by requesting the phone number info endpoint.</summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
