using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EventForge.DTOs.External.WhatsApp;
using EventForge.DTOs.RetailCart;
using EventForge.Server.Services.RetailCart;
using Microsoft.Extensions.Configuration;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Processes Meta WhatsApp Cloud API webhook events.
/// On receiving a catalog <em>order</em> message the service:
/// <list type="number">
///   <item>Creates a <see cref="IRetailCartSessionService">RetailCartSession</see> for the order.</item>
///   <item>Adds every product line to that session.</item>
///   <item>Sends a confirmation reply to the customer via the Cloud API.</item>
/// </list>
/// </summary>
public class WhatsAppService(
    IHttpClientFactory httpClientFactory,
    IRetailCartSessionService cartSessionService,
    IConfiguration configuration,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    // ─── Configuration keys ──────────────────────────────────────────────────

    private string VerifyToken =>
        configuration["WhatsApp:VerifyToken"] ?? string.Empty;

    private string AccessToken =>
        configuration["WhatsApp:AccessToken"] ?? string.Empty;

    private string PhoneNumberId =>
        configuration["WhatsApp:PhoneNumberId"] ?? string.Empty;

    private string ApiVersion =>
        configuration["WhatsApp:ApiVersion"] ?? "v19.0";

    // ─── JSON options ────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    // ─── Reply message templates (configurable via appsettings) ─────────────
    // TODO: Replace with a proper i18n / templating mechanism when multi-language
    //       support is required. Keys can be overridden in appsettings.json under
    //       WhatsApp:Messages:*.

    private string OrderErrorMessage =>
        configuration["WhatsApp:Messages:OrderError"]
        ?? "Ci dispiace, si è verificato un problema con il tuo ordine. "
         + "Il nostro team lo verificherà al più presto.";

    private string OrderAckHeader =>
        configuration["WhatsApp:Messages:AckHeader"] ?? "✅ *Ordine ricevuto!*";

    private string OrderAckReference =>
        configuration["WhatsApp:Messages:AckReference"] ?? "Riferimento ordine: *{0}*";

    private string OrderAckFooter =>
        configuration["WhatsApp:Messages:AckFooter"]
        ?? "Ti contatteremo a breve per confermare la consegna. Grazie!";

    // ─── IWhatsAppService ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string? VerifyWebhook(string mode, string verifyToken, string challenge)
    {
        if (mode != "subscribe")
        {
            logger.LogWarning("WhatsApp webhook verification failed: unexpected mode '{Mode}'.", mode);
            return null;
        }

        if (string.IsNullOrWhiteSpace(VerifyToken))
        {
            logger.LogError("WhatsApp:VerifyToken is not configured. Webhook verification cannot proceed.");
            return null;
        }

        if (verifyToken != VerifyToken)
        {
            logger.LogWarning("WhatsApp webhook verification failed: token mismatch.");
            return null;
        }

        logger.LogInformation("WhatsApp webhook successfully verified.");
        return challenge;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WhatsAppOrderReceivedDto>> ProcessWebhookAsync(
        WhatsAppWebhookPayload payload,
        CancellationToken cancellationToken = default)
    {
        var results = new List<WhatsAppOrderReceivedDto>();

        if (payload.Object != "whatsapp_business_account")
        {
            logger.LogWarning(
                "Received webhook with unexpected object type '{Object}'. Skipping.",
                payload.Object);
            return results;
        }

        foreach (var entry in payload.Entry)
        {
            foreach (var change in entry.Changes)
            {
                if (change.Field != "messages" || change.Value.Messages is null)
                    continue;

                foreach (var message in change.Value.Messages)
                {
                    var dto = await ProcessMessageAsync(message, change.Value, cancellationToken);
                    if (dto is not null)
                        results.Add(dto);
                }
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task SendTextMessageAsync(
        string toPhoneNumber,
        string messageText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(AccessToken) || string.IsNullOrWhiteSpace(PhoneNumberId))
        {
            logger.LogWarning(
                "WhatsApp reply skipped: AccessToken or PhoneNumberId is not configured.");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("WhatsAppApi");

            var body = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = toPhoneNumber,
                type = "text",
                text = new { preview_url = false, body = messageText }
            };

            var json = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var url = $"/{ApiVersion}/{PhoneNumberId}/messages";
            var response = await client.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "WhatsApp API replied with {StatusCode} when sending message to {Phone}. Body: {Body}",
                    (int)response.StatusCode, toPhoneNumber, errorBody);
            }
            else
            {
                logger.LogInformation(
                    "WhatsApp reply sent to {Phone}.", toPhoneNumber);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while sending WhatsApp message to {Phone}.", toPhoneNumber);
        }
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task<WhatsAppOrderReceivedDto?> ProcessMessageAsync(
        WhatsAppMessage message,
        WhatsAppChangeValue changeValue,
        CancellationToken cancellationToken)
    {
        if (message.Type != "order" || message.Order is null)
        {
            logger.LogDebug(
                "WhatsApp message {MessageId} of type '{Type}' — not an order, skipping.",
                message.Id, message.Type);
            return null;
        }

        var senderName = changeValue.Contacts?
            .FirstOrDefault(c => c.WaId == message.From)?.Profile.Name;

        var receivedAt = DateTimeOffset.FromUnixTimeSeconds(
            long.TryParse(message.Timestamp, out var ts) ? ts : DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        logger.LogInformation(
            "Processing WhatsApp order {MessageId} from {Phone} ({Name}), {ItemCount} item(s).",
            message.Id, message.From, senderName ?? "unknown", message.Order.ProductItems.Count);

        var orderDto = new WhatsAppOrderReceivedDto
        {
            WhatsAppMessageId = message.Id,
            SenderPhone       = message.From,
            SenderName        = senderName,
            CatalogId         = message.Order.CatalogId,
            CustomerNote      = message.Order.Text,
            ReceivedAt        = receivedAt,
            Lines             = message.Order.ProductItems
                .Select(p => new WhatsAppOrderLineDto
                {
                    ProductRetailerId = p.ProductRetailerId,
                    Quantity          = p.Quantity,
                    UnitPrice         = p.ItemPrice,
                    Currency          = p.Currency
                }).ToList()
        };

        try
        {
            var currency = message.Order.ProductItems.FirstOrDefault()?.Currency ?? "EUR";

            // Create a new cart session for the WhatsApp order
            var session = await cartSessionService.CreateSessionAsync(
                new CreateCartSessionDto
                {
                    SalesChannel = "WhatsApp",
                    Currency     = currency
                },
                cancellationToken);

            // Add every product line to the cart session
            foreach (var line in orderDto.Lines)
            {
                await cartSessionService.AddItemAsync(
                    session.Id,
                    new AddCartItemDto
                    {
                        ProductCode = line.ProductRetailerId,
                        // TODO: Resolve the product name from the catalogue using ProductRetailerId.
                        // The retailer ID is used as a placeholder so staff can identify the item.
                        ProductName = line.ProductRetailerId,
                        UnitPrice   = line.UnitPrice,
                        Quantity    = line.Quantity
                    },
                    cancellationToken);
            }

            orderDto.InternalOrderId    = session.Id;
            orderDto.ProcessingStatus   = WhatsAppOrderStatus.Created;
            orderDto.ProcessingMessage  = $"Cart session {session.Id} created with {orderDto.Lines.Count} item(s).";

            logger.LogInformation(
                "WhatsApp order {MessageId} mapped to cart session {SessionId}.",
                message.Id, session.Id);

            // Send acknowledgment reply to the customer
            var replyText = BuildAcknowledgmentMessage(orderDto, session);
            await SendTextMessageAsync(message.From, replyText, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to create cart session for WhatsApp order {MessageId} from {Phone}.",
                message.Id, message.From);

            orderDto.ProcessingStatus  = WhatsAppOrderStatus.Failed;
            orderDto.ProcessingMessage = ex.Message;

            // Notify customer of temporary failure
            await SendTextMessageAsync(
                message.From,
                OrderErrorMessage,
                cancellationToken);
        }

        return orderDto;
    }

    private string BuildAcknowledgmentMessage(
        WhatsAppOrderReceivedDto order,
        CartSessionDto session)
    {
        var sb = new StringBuilder();
        sb.AppendLine(OrderAckHeader);
        sb.AppendLine();
        sb.AppendLine(string.Format(OrderAckReference, session.Id.ToString("N")));
        sb.AppendLine();

        decimal total = 0;
        foreach (var line in order.Lines)
        {
            sb.AppendLine($"• {line.ProductRetailerId} x{line.Quantity} — {line.LineTotal:F2} {line.Currency}");
            total += line.LineTotal;
        }

        sb.AppendLine();
        sb.AppendLine($"*Totale: {total:F2} {order.Lines.FirstOrDefault()?.Currency ?? "EUR"}*");

        if (!string.IsNullOrWhiteSpace(order.CustomerNote))
        {
            sb.AppendLine();
            sb.AppendLine($"Nota: {order.CustomerNote}");
        }

        sb.AppendLine();
        sb.AppendLine(OrderAckFooter);

        return sb.ToString().TrimEnd();
    }
}
