using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Sends messages via WhatsApp Cloud API using a named HttpClient.
/// </summary>
public class WhatsAppService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    private readonly string _phoneNumberId = configuration["WhatsApp:PhoneNumberId"] ?? string.Empty;
    private readonly string _apiVersion = configuration["WhatsApp:ApiVersion"] ?? "v19.0";

    public async Task<string?> SendTextMessageAsync(string toPhone, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("WhatsApp");
            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone,
                type = "text",
                text = new { body = text }
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_apiVersion}/{_phoneNumberId}/messages", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("WhatsApp sendText failed ({Status}): {Error}", response.StatusCode, err);
                return null;
            }
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("messages", out var msgs) && msgs.GetArrayLength() > 0
                ? msgs[0].GetProperty("id").GetString()
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending WhatsApp text message to {Phone}", toPhone);
            return null;
        }
    }

    public async Task SendReadReceiptAsync(string toPhone, string messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("WhatsApp");
            var payload = new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = messageId
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_apiVersion}/{_phoneNumberId}/messages", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("WhatsApp read receipt failed ({Status}): {Error}", response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending WhatsApp read receipt for message {MessageId}", messageId);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_phoneNumberId)) return false;
            var client = httpClientFactory.CreateClient("WhatsApp");
            var response = await client.GetAsync($"{_apiVersion}/{_phoneNumberId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing WhatsApp connection");
            return false;
        }
    }
}
