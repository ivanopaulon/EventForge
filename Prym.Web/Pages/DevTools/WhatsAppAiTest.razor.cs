using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Prym.DTOs.AI;
using Prym.Web.Services;
using Prym.Web.Services.AI;

namespace Prym.Web.Pages.DevTools;

public partial class WhatsAppAiTest : ComponentBase
{
    [Inject] private IAIOrderClientService AIOrderClient { get; set; } = default!;
    [Inject] private IAppNotificationService AppNotification { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<WhatsAppAiTest> Logger { get; set; } = default!;

    // ─── State ───────────────────────────────────────────────────────────────

    private string SimulatedPhone { get; set; } = "+39 333 0000000";
    private string BusinessPartyIdText { get; set; } = string.Empty;
    private bool AutoReplyEnabled { get; set; } = true;

    private string _messageInput = string.Empty;
    private bool _isSending = false;
    private SimulateInboundResultDto? _lastResult;
    private ElementReference _chatContainer;

    private readonly List<ChatBubble> _chatMessages = [];

    // ─── Actions ─────────────────────────────────────────────────────────────

    private async Task SendMessageAsync()
    {
        var text = _messageInput.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        _chatMessages.Add(new ChatBubble(text, false, DateTime.Now));
        _messageInput = string.Empty;
        _isSending = true;
        StateHasChanged();

        try
        {
            Guid? bpId = null;
            if (Guid.TryParse(BusinessPartyIdText, out var parsed)) bpId = parsed;

            var request = new SimulateInboundDto
            {
                PhoneNumber = SimulatedPhone,
                MessageText = text,
                BusinessPartyId = bpId
            };

            _lastResult = await AIOrderClient.SimulateInboundAsync(request);

            if (_lastResult?.AiResponse is { Length: > 0 } reply)
                _chatMessages.Add(new ChatBubble(reply, true, DateTime.Now));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SimulateInbound failed");
            AppNotification.ShowError("Errore durante la simulazione.");
        }
        finally
        {
            _isSending = false;
            StateHasChanged();
            await ScrollChatToBottomAsync();
        }
    }

    private async Task ResetSessionAsync()
    {
        await AIOrderClient.ResetSessionAsync(SimulatedPhone);
        _lastResult = null;
        _chatMessages.Clear();
        AppNotification.ShowSuccess("Sessione AI resettata.");
        StateHasChanged();
    }

    private async Task OnMessageKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendMessageAsync();
    }

    private async Task ScrollChatToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("eval", $"document.getElementById('chat-container')?.scrollTo({{top: 9999, behavior: 'smooth'}})");
        }
        catch { /* non-critical */ }
    }

    // ─── Colour helpers ───────────────────────────────────────────────────────

    private static Color GetIntentColor(MessageIntent? intent) => intent switch
    {
        MessageIntent.Ordine => Color.Primary,
        MessageIntent.Conferma => Color.Success,
        MessageIntent.Annullamento => Color.Error,
        MessageIntent.Domanda => Color.Info,
        MessageIntent.Saluto => Color.Secondary,
        _ => Color.Default
    };

    private static Color GetStateColor(OrderConversationState? state) => state switch
    {
        OrderConversationState.CollectingItems => Color.Info,
        OrderConversationState.ConfirmingOrder => Color.Warning,
        OrderConversationState.Completed => Color.Success,
        OrderConversationState.Cancelled => Color.Error,
        _ => Color.Default
    };

    // ─── Chat bubble model ────────────────────────────────────────────────────

    private record ChatBubble(string Text, bool IsOutgoing, DateTime Timestamp);
}
