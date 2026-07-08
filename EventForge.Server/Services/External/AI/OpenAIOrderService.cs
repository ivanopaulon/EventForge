using Prym.DTOs.AI;
using Prym.DTOs.Products;
using System.Text.Json;

namespace EventForge.Server.Services.External.AI;

/// <summary>
/// OpenAI-backed implementation of <see cref="IAIOrderService"/>.
/// Uses the Chat Completions API with configurable model (default: gpt-4o).
/// Falls back gracefully when the API key is not configured.
/// </summary>
public class OpenAIOrderService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OpenAIOrderService> logger) : IAIOrderService
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    // ─── Config helpers ───────────────────────────────────────────────────────

    private string ApiKey => configuration["OpenAI:ApiKey"] ?? string.Empty;
    private string Model => configuration["OpenAI:Model"] ?? "gpt-4o";
    private int MaxTokens => int.TryParse(configuration["OpenAI:MaxTokens"], out var v) ? v : 512;
    private double Temperature => double.TryParse(configuration["OpenAI:Temperature"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.2;
    private bool IsEnabled => configuration.GetValue<bool>("OpenAI:EnableAI", false) && !string.IsNullOrWhiteSpace(ApiKey);

    // ─── IAIOrderService ──────────────────────────────────────────────────────

    public async Task<MessageIntent> ClassifyIntentAsync(string message, string? sessionContext, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return MessageIntent.Ordine;

        var systemPrompt =
            "Sei un classificatore di messaggi WhatsApp per un negozio. " +
            "Rispondi con UNA SOLA parola tra: Ordine, Domanda, Conferma, Annullamento, Saluto, Altro. " +
            "Non aggiungere altro testo.";

        var userContent = string.IsNullOrWhiteSpace(sessionContext)
            ? message
            : $"Contesto sessione: {sessionContext}\n\nMessaggio cliente: {message}";

        try
        {
            var raw = await CallChatCompletionAsync(systemPrompt, userContent, maxTokens: 10, callType: "ClassifyIntent", cancellationToken: cancellationToken);
            return raw?.Trim() switch
            {
                "Ordine" => MessageIntent.Ordine,
                "Domanda" => MessageIntent.Domanda,
                "Conferma" => MessageIntent.Conferma,
                "Annullamento" => MessageIntent.Annullamento,
                "Saluto" => MessageIntent.Saluto,
                _ => MessageIntent.Altro
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ClassifyIntentAsync failed");
            return MessageIntent.Altro;
        }
    }

    public async Task<List<OrderDraftItem>> ExtractOrderItemsAsync(string message, IList<ProductDto> catalog, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return [];

        var catalogJson = JsonSerializer.Serialize(
            catalog.Select(p => new { p.Id, p.Code, p.Name, p.DefaultPrice }).Take(200),
            _jsonOpts);

        var systemPrompt =
            "Sei un assistente per un negozio. Estrai le righe d'ordine dal messaggio del cliente. " +
            "Rispondi SOLO con un array JSON con questa struttura per ogni riga: " +
            "[{\"RawText\":\"...\",\"ProductId\":\"guid-or-null\",\"ProductCode\":\"...\",\"ProductName\":\"...\",\"Quantity\":1,\"UnitOfMeasure\":\"pz\",\"IsAmbiguous\":false,\"IsNotFound\":false,\"Suggestions\":[]}]." +
            "Se un prodotto non è presente nel catalogo imposta IsNotFound=true. " +
            "Usa solo i prodotti forniti nel catalogo. Non inventare prodotti.";

        var userContent = $"Catalogo prodotti disponibili (JSON):\n{catalogJson}\n\nMessaggio cliente:\n{message}";

        try
        {
            var raw = await CallChatCompletionAsync(systemPrompt, userContent, callType: "ExtractOrderItems", cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(raw)) return [];

            // Strip potential markdown code fences
            var json = raw.Trim().TrimStart('`').TrimEnd('`');
            if (json.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                json = json[4..].TrimStart();

            var items = JsonSerializer.Deserialize<List<OrderDraftItem>>(json, _jsonOpts) ?? [];
            return items;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ExtractOrderItemsAsync failed — raw response parsing error");
            return [];
        }
    }

    public async Task<string> GenerateGuidanceResponseAsync(OrderDraftContext ctx, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return "Grazie per il tuo messaggio. Un operatore risponderà a breve.";

        var draftSummary = ctx.Items.Count == 0
            ? "Nessun prodotto ancora selezionato."
            : string.Join("\n", ctx.Items.Select(i => $"- {i.ProductName ?? i.RawText}: {i.Quantity} {i.UnitOfMeasure ?? "pz"}"));

        var systemPrompt =
            "Sei un assistente WhatsApp per un negozio. Rispondi in italiano in modo cordiale e conciso. " +
            "Aiuta il cliente a completare il suo ordine. " +
            (ctx.State == OrderConversationState.ConfirmingOrder
                ? "Stai chiedendo conferma dell'ordine. Elenca i prodotti e chiedi conferma."
                : "Stai raccogliendo i prodotti dell'ordine. Chiedi di continuare o di confermare.");

        var userContent =
            $"Cliente: {ctx.BusinessPartyName ?? "Cliente"}\n" +
            $"Stato sessione: {ctx.State}\n" +
            $"Prodotti nel draft:\n{draftSummary}\n" +
            $"Ultimo messaggio del cliente: {ctx.LastCustomerMessage}";

        try
        {
            return await CallChatCompletionAsync(systemPrompt, userContent, callType: "GenerateGuidance", cancellationToken: cancellationToken)
                   ?? "Grazie per il tuo messaggio. Come posso aiutarti?";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GenerateGuidanceResponseAsync failed");
            return "Grazie per il tuo messaggio. Un operatore risponderà a breve.";
        }
    }

    public Task<OrderConversationState> GetNextStepAsync(OrderConversationState state, CancellationToken cancellationToken = default)
    {
        var next = state switch
        {
            OrderConversationState.Idle => OrderConversationState.CollectingItems,
            OrderConversationState.CollectingItems => OrderConversationState.ConfirmingOrder,
            OrderConversationState.ConfirmingOrder => OrderConversationState.Completed,
            _ => state
        };
        return Task.FromResult(next);
    }

    // ─── Internal OpenAI helper ───────────────────────────────────────────────

    private async Task<string?> CallChatCompletionAsync(
        string systemPrompt,
        string userContent,
        int? maxTokens = null,
        string? callType = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("OpenAI");

        var requestBody = new
        {
            model = Model,
            max_tokens = maxTokens ?? MaxTokens,
            temperature = Temperature,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userContent  }
            }
        };

        var response = await client.PostAsJsonAsync("v1/chat/completions", requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("OpenAI API error {Status} [{CallType}]: {Body}", response.StatusCode, callType, errorBody);
            return null;
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
