using EventForge.DTOs.Chat;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Service for handling chat operations and real-time messaging.
/// Integrates with REST API endpoints and SignalR for real-time functionality.
/// </summary>
public interface IChatService
{
    Task<List<ChatResponseDto>> GetChatsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default);
    Task<ChatResponseDto?> GetChatByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChatResponseDto> CreateChatAsync(CreateChatDto createDto, CancellationToken cancellationToken = default);
    Task<List<ChatMessageDto>> GetMessagesAsync(Guid chatId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(SendMessageDto messageDto, CancellationToken cancellationToken = default);
    Task<bool> EditMessageAsync(Guid messageId, EditMessageDto editDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<bool> MarkMessagesAsReadAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<bool> ToggleMessageReactionAsync(MessageReactionActionDto reactionDto, CancellationToken cancellationToken = default);
    Task<bool> AddMemberToChatAsync(Guid chatId, UpdateChatMembersDto memberDto, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberFromChatAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateTypingStatusAsync(Guid chatId, bool isTyping, CancellationToken cancellationToken = default);
    Task<ChatStatsDto> GetChatStatsAsync(CancellationToken cancellationToken = default);

    // Events for real-time updates
    event Action<ChatResponseDto>? ChatCreated;
    event Action<ChatMessageDto>? MessageReceived;
    event Action<EditMessageDto>? MessageEdited;
    event Action<Guid>? MessageDeleted;
    event Action<Guid, Guid>? MessageRead;
    event Action<TypingIndicatorDto>? TypingIndicator;
    event Action<Guid, Guid>? UserJoinedChat;
    event Action<Guid, Guid>? UserLeftChat;
}

public class ChatService : IChatService
{
    private readonly IHttpClientService _httpClientService;
    private readonly SignalRService _signalRService;
    private readonly IPerformanceOptimizationService _performanceService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IHttpClientService httpClientService,
        SignalRService signalRService,
        IPerformanceOptimizationService performanceService,
        ILogger<ChatService> logger)
    {
        _httpClientService = httpClientService;
        _signalRService = signalRService;
        _performanceService = performanceService;
        _logger = logger;

        // Subscribe to SignalR events
        _signalRService.ChatCreated += OnChatCreated;
        _signalRService.MessageReceived += OnMessageReceived;
        _signalRService.MessageEdited += OnMessageEdited;
        _signalRService.MessageDeleted += OnMessageDeleted;
        _signalRService.MessageRead += OnMessageRead;
        _signalRService.TypingIndicator += OnTypingIndicator;
        _signalRService.UserJoinedChat += OnUserJoinedChat;
        _signalRService.UserLeftChat += OnUserLeftChat;
    }

    #region Events
    public event Action<ChatResponseDto>? ChatCreated;
    public event Action<ChatMessageDto>? MessageReceived;
    public event Action<EditMessageDto>? MessageEdited;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid, Guid>? MessageRead;
    public event Action<TypingIndicatorDto>? TypingIndicator;
    public event Action<Guid, Guid>? UserJoinedChat;
    public event Action<Guid, Guid>? UserLeftChat;
    #endregion

    #region API Operations

    public async Task<List<ChatResponseDto>> GetChatsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use caching for improved performance
            var cacheKey = $"{CacheKeys.CHAT_LIST}_{page}_{pageSize}_{filter}";

            return await _performanceService.GetCachedDataAsync(cacheKey, async () =>
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrEmpty(filter))
                {
                    queryParams.Add($"type={filter}");
                }

                var query = string.Join("&", queryParams);
                var pagedResult = await _httpClientService.GetAsync<PagedResult<ChatResponseDto>>($"api/v1/chat?{query}", cancellationToken);
                return pagedResult?.Items?.ToList() ?? new List<ChatResponseDto>();
            }, TimeSpan.FromMinutes(5)) ?? new List<ChatResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chats");
            return new List<ChatResponseDto>();
        }
    }

    public async Task<ChatResponseDto?> GetChatByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ChatResponseDto>($"api/v1/chat/{id}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat {Id}", id);
            return null;
        }
    }

    public async Task<ChatResponseDto> CreateChatAsync(CreateChatDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateChatDto, ChatResponseDto>("api/v1/chat", createDto, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to create chat");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat");
            throw;
        }
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid chatId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use caching for chat messages with shorter expiration for real-time updates
            var cacheKey = $"{CacheKeys.ChatMessages(chatId)}_{page}_{pageSize}";

            return await _performanceService.GetCachedDataAsync(cacheKey, async () =>
            {
                var query = $"page={page}&pageSize={pageSize}";
                var pagedResult = await _httpClientService.GetAsync<PagedResult<ChatMessageDto>>($"api/v1/chat/{chatId}/messages?{query}", cancellationToken);
                return pagedResult?.Items?.ToList() ?? new List<ChatMessageDto>();
            }, TimeSpan.FromMinutes(2)) ?? new List<ChatMessageDto>(); // Shorter cache for messages
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for chat {ChatId}", chatId);
            return new List<ChatMessageDto>();
        }
    }

    public async Task<ChatMessageDto> SendMessageAsync(SendMessageDto messageDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<SendMessageDto, ChatMessageDto>($"api/v1/chat/{messageDto.ChatId}/messages", messageDto, cancellationToken);

            // Invalidate cache for this chat's messages
            _performanceService.InvalidateCachePattern(CacheKeys.ChatMessages(messageDto.ChatId));

            return result ?? throw new InvalidOperationException("Failed to send message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to chat {ChatId}", messageDto.ChatId);
            throw;
        }
    }

    public async Task<bool> EditMessageAsync(Guid messageId, EditMessageDto editDto, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PutAsync($"api/v1/chat/messages/{messageId}", editDto, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", messageId);
            return false;
        }
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/chat/messages/{messageId}", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return false;
        }
    }

    public async Task<bool> MarkMessagesAsReadAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _httpClientService.PatchAsync<object, object>($"api/v1/chat/{chatId}/read", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read in chat {ChatId}", chatId);
            return false;
        }
    }

    public async Task<bool> AddMemberToChatAsync(Guid chatId, UpdateChatMembersDto memberDto, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PostAsync($"api/v1/chat/{chatId}/members", memberDto, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to chat {ChatId}", chatId);
            return false;
        }
    }

    public async Task<bool> RemoveMemberFromChatAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/chat/{chatId}/members/{userId}", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {UserId} from chat {ChatId}", userId, chatId);
            return false;
        }
    }

    public async Task<bool> UpdateTypingStatusAsync(Guid chatId, bool isTyping, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use debouncing to reduce network traffic for typing indicators
            _ = await _performanceService.DebounceAsync(
                $"{DebounceKeys.TYPING_INDICATOR}_{chatId}",
                async () =>
                {
                    if (_signalRService.IsChatConnected)
                    {
                        await _signalRService.SendTypingIndicatorAsync(chatId, isTyping);
                    }
                    return true;
                },
                TimeSpan.FromMilliseconds(300) // 300ms debounce
            );
            return true;
        }
        catch (OperationCanceledException)
        {
            // Debounced operation was cancelled, this is expected
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating typing status for chat {ChatId}", chatId);
            return false;
        }
    }

    public async Task<ChatStatsDto> GetChatStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ChatStatsDto>("api/v1/chat/stats", cancellationToken) ?? new ChatStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat stats");
            return new ChatStatsDto();
        }
    }

    #endregion

    #region SignalR Event Handlers

    private void OnChatCreated(ChatResponseDto chat)
    {
        ChatCreated?.Invoke(chat);
    }

    private void OnMessageReceived(ChatMessageDto message)
    {
        MessageReceived?.Invoke(message);
    }

    private void OnMessageEdited(EditMessageDto editMessage)
    {
        MessageEdited?.Invoke(editMessage);
    }

    private void OnMessageDeleted(object deletedMessage)
    {
        // Extract message ID from the deleted message object
        if (deletedMessage is Dictionary<string, object> dict && dict.TryGetValue("messageId", out var messageIdObj))
        {
            if (Guid.TryParse(messageIdObj?.ToString(), out var messageId))
            {
                MessageDeleted?.Invoke(messageId);
            }
        }
    }

    private void OnMessageRead(object readMessage)
    {
        // Extract chat ID and message ID from the read message object
        if (readMessage is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("chatId", out var chatIdObj) &&
                dict.TryGetValue("messageId", out var messageIdObj))
            {
                if (Guid.TryParse(chatIdObj?.ToString(), out var chatId) &&
                    Guid.TryParse(messageIdObj?.ToString(), out var messageId))
                {
                    MessageRead?.Invoke(chatId, messageId);
                }
            }
        }
    }

    private void OnTypingIndicator(TypingIndicatorDto typingIndicator)
    {
        TypingIndicator?.Invoke(typingIndicator);
    }

    private void OnUserJoinedChat(object userJoinedEvent)
    {
        // Extract chat ID and user ID from the event object
        if (userJoinedEvent is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("chatId", out var chatIdObj) &&
                dict.TryGetValue("userId", out var userIdObj))
            {
                if (Guid.TryParse(chatIdObj?.ToString(), out var chatId) &&
                    Guid.TryParse(userIdObj?.ToString(), out var userId))
                {
                    UserJoinedChat?.Invoke(chatId, userId);
                }
            }
        }
    }

    private void OnUserLeftChat(object userLeftEvent)
    {
        // Extract chat ID and user ID from the event object
        if (userLeftEvent is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("chatId", out var chatIdObj) &&
                dict.TryGetValue("userId", out var userIdObj))
            {
                if (Guid.TryParse(chatIdObj?.ToString(), out var chatId) &&
                    Guid.TryParse(userIdObj?.ToString(), out var userId))
                {
                    UserLeftChat?.Invoke(chatId, userId);
                }
            }
        }
    }

    #endregion

    public async Task<bool> ToggleMessageReactionAsync(MessageReactionActionDto reactionDto, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _httpClientService.PostAsync<MessageReactionActionDto, object>($"api/v1/chat/messages/{reactionDto.MessageId}/reactions", reactionDto, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling reaction on message {MessageId}", reactionDto.MessageId);
            return false;
        }
    }
}