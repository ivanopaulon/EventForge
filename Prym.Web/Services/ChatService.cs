using Microsoft.AspNetCore.Components.Forms;
using Prym.DTOs.Chat;
using Prym.DTOs.Common;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Prym.Web.Services;

/// <summary>
/// Service for handling chat operations and real-time messaging.
/// Integrates with REST API endpoints and SignalR for real-time functionality.
/// </summary>
public interface IChatService : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<List<ChatResponseDto>> GetChatsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default);
    Task<ChatResponseDto?> GetChatByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChatMemberDto>> GetChatMembersAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<List<ChatAvailableUserDto>> GetAvailableUsersAsync(CancellationToken cancellationToken = default);
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
    Task<MessageAttachmentDto?> UploadFileAsync(Guid chatId, IBrowserFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Calls the server to merge any duplicate DirectMessage chats for the current user
    /// (same pair of users, same tenant). Returns the number of duplicate threads removed.
    /// </summary>
    Task<int> MergeDuplicateChatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports (flags) a chat message as inappropriate.
    /// </summary>
    /// <param name="messageId">ID of the message to report.</param>
    /// <param name="dto">Optional report reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was flagged; false if not found or on error.</returns>
    Task<bool> ReportMessageAsync(Guid messageId, ReportMessageDto dto, CancellationToken cancellationToken = default);

    // Events for real-time updates
    event Action<ChatResponseDto>? ChatCreated;
    event Action<ChatMessageDto>? MessageReceived;
    event Action<ChatMessageDto>? MessageEdited;
    event Action<Guid>? MessageDeleted;
    event Action<Guid, Guid>? MessageRead;
    event Action<TypingIndicatorDto>? TypingIndicator;
    event Action<Guid, Guid>? UserJoinedChat;
    event Action<Guid, Guid>? UserLeftChat;
    event Action<Guid, bool>? UserOnlineStatusChanged;
    /// <summary>Fired when the current user is added to a chat by another member.</summary>
    event Action<ChatResponseDto>? AddedToChat;
    /// <summary>Fired when the current user is removed from a chat.</summary>
    event Action<Guid>? RemovedFromChat;
}

public class ChatService : IChatService
{
    private const string BaseUrl = "api/v1/chat";
    private static readonly TimeSpan ChatListCacheTtl = TimeSpan.FromMinutes(5);
    private readonly IHttpClientService _httpClientService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly IRealtimeService _realtimeService;
    private readonly IPerformanceOptimizationService _performanceService;
    private readonly ILogger<ChatService> _logger;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private List<ChatResponseDto>? _preloadedChats;
    private bool _disposed;

    public ChatService(
        IHttpClientService httpClientService,
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        IRealtimeService realtimeService,
        IPerformanceOptimizationService performanceService,
        ILogger<ChatService> logger)
    {
        _httpClientService = httpClientService;
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _realtimeService = realtimeService;
        _performanceService = performanceService;
        _logger = logger;

        // Subscribe to real-time events
        _realtimeService.ChatCreated += OnChatCreated;
        _realtimeService.MessageReceived += OnMessageReceived;
        _realtimeService.MessageEdited += OnMessageEdited;
        _realtimeService.MessageDeleted += OnMessageDeleted;
        _realtimeService.MessageRead += OnMessageRead;
        _realtimeService.TypingIndicator += OnTypingIndicator;
        _realtimeService.UserJoinedChat += OnUserJoinedChat;
        _realtimeService.UserLeftChat += OnUserLeftChat;
        _realtimeService.UserOnlineStatusChanged += OnUserOnlineStatusChanged;
        _realtimeService.AddedToChat += OnAddedToChat;
        _realtimeService.RemovedFromChat += OnRemovedFromChat;
        _realtimeService.ChatDeleted += OnChatDeleted;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _initializeLock.Dispose();

        _realtimeService.ChatCreated -= OnChatCreated;
        _realtimeService.MessageReceived -= OnMessageReceived;
        _realtimeService.MessageEdited -= OnMessageEdited;
        _realtimeService.MessageDeleted -= OnMessageDeleted;
        _realtimeService.MessageRead -= OnMessageRead;
        _realtimeService.TypingIndicator -= OnTypingIndicator;
        _realtimeService.UserJoinedChat -= OnUserJoinedChat;
        _realtimeService.UserLeftChat -= OnUserLeftChat;
        _realtimeService.UserOnlineStatusChanged -= OnUserOnlineStatusChanged;
        _realtimeService.AddedToChat -= OnAddedToChat;
        _realtimeService.RemovedFromChat -= OnRemovedFromChat;
        _realtimeService.ChatDeleted -= OnChatDeleted;
    }

    #region Events
    public event Action<ChatResponseDto>? ChatCreated;
    public event Action<ChatMessageDto>? MessageReceived;
    public event Action<ChatMessageDto>? MessageEdited;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid, Guid>? MessageRead;
    public event Action<TypingIndicatorDto>? TypingIndicator;
    public event Action<Guid, Guid>? UserJoinedChat;
    public event Action<Guid, Guid>? UserLeftChat;
    public event Action<Guid, bool>? UserOnlineStatusChanged;
    public event Action<ChatResponseDto>? AddedToChat;
    public event Action<Guid>? RemovedFromChat;
    #endregion

    #region API Operations

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_preloadedChats is not null)
            return;

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_preloadedChats is not null || !await _authService.IsAuthenticatedAsync())
                return;

            _preloadedChats = await LoadAllChatsAsync(cancellationToken);
            var defaultPage = _preloadedChats.Take(50).ToList();
            _performanceService.PreloadData(
                BuildChatCacheKey(1, 50, null),
                () => Task.FromResult(defaultPage),
                ChatListCacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error preloading chat threads during initialization");
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public async Task<List<ChatResponseDto>> GetChatsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildChatCacheKey(page, pageSize, filter);

        if (_preloadedChats is not null)
            return SliceChats(_preloadedChats, page, pageSize, filter);

        // Fast path: if the cache is already populated serve it immediately without a factory call.
        // The cache is invalidated on ChatCreated / AddedToChat / RemovedFromChat events,
        // so staleness is bounded by those events rather than the TTL alone.
        if (_performanceService.IsCached(cacheKey))
            return await _performanceService.GetCachedDataAsync<List<ChatResponseDto>>(
                cacheKey, () => Task.FromResult<List<ChatResponseDto>>([]), ChatListCacheTtl) ?? [];

        try
        {
            var (_, items) = await LoadChatPageAsync(page, pageSize, filter, cancellationToken);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chats");
            return [];
        }
    }

    private async Task<List<ChatResponseDto>> LoadAllChatsAsync(CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var page = 1;
        var allChats = new List<ChatResponseDto>();

        while (true)
        {
            var (pagedResult, items) = await LoadChatPageAsync(page, pageSize, null, cancellationToken, cacheResult: false);
            if (items.Count == 0)
                break;

            allChats.AddRange(items);

            var totalCount = pagedResult?.TotalCount ?? allChats.Count;
            if (allChats.Count >= totalCount)
                break;

            page++;
        }

        return allChats;
    }

    private async Task<(PagedResult<ChatResponseDto>? PagedResult, List<ChatResponseDto> Items)> LoadChatPageAsync(
        int page,
        int pageSize,
        string? filter,
        CancellationToken cancellationToken,
        bool cacheResult = true)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={page}",
            $"pageSize={pageSize}"
        };

        var chatTypeValue = MapFilterToChatType(filter);
        if (!string.IsNullOrEmpty(chatTypeValue))
            queryParams.Add($"types={chatTypeValue}");

        var query = string.Join("&", queryParams);
        var pagedResult = await _httpClientService.GetAsync<PagedResult<ChatResponseDto>>($"api/v1/chat?{query}", cancellationToken);
        var items = pagedResult?.Items?.ToList() ?? [];

        if (cacheResult && (items.Count > 0 || await _authService.IsAuthenticatedAsync()))
        {
            _performanceService.PreloadData(
                BuildChatCacheKey(page, pageSize, filter),
                () => Task.FromResult(items),
                ChatListCacheTtl);
        }

        return (pagedResult, items);
    }

    private static string BuildChatCacheKey(int page, int pageSize, string? filter)
        => $"{CacheKeys.CHAT_LIST}_{page}_{pageSize}_{filter}";

    private static string? MapFilterToChatType(string? filter) => filter?.ToLowerInvariant() switch
    {
        "direct" => "DirectMessage",
        "group" => "Group",
        "channel" => "Channel",
        _ => null
    };

    private static List<ChatResponseDto> SliceChats(IEnumerable<ChatResponseDto> chats, int page, int pageSize, string? filter)
    {
        var filtered = MapFilterToChatType(filter) switch
        {
            "DirectMessage" => chats.Where(chat => chat.Type == ChatType.DirectMessage),
            "Group" => chats.Where(chat => chat.Type == ChatType.Group),
            "Channel" => chats.Where(chat => chat.Type == ChatType.Channel),
            _ => chats
        };

        return filtered
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();
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

    public async Task<List<ChatMemberDto>> GetChatMembersAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId, cancellationToken);
            return chat?.Members ?? new List<ChatMemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for chat {ChatId}", chatId);
            return new List<ChatMemberDto>();
        }
    }

    public async Task<List<ChatAvailableUserDto>> GetAvailableUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<List<ChatAvailableUserDto>>("api/v1/chat/available-users", cancellationToken)
                   ?? new List<ChatAvailableUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available users for chat");
            return new List<ChatAvailableUserDto>();
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
            var result = await _httpClientService.PostAsync<SendMessageDto, ChatMessageDto>($"api/v1/chat/messages", messageDto, cancellationToken);

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
            _ = await _httpClientService.PostAsync<object, object>($"api/v1/chat/{chatId}/messages/read-all", new { }, cancellationToken);
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
                    if (_realtimeService.IsChatConnected)
                    {
                        await _realtimeService.SendTypingIndicatorAsync(chatId, isTyping);
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
            return await _httpClientService.GetAsync<ChatStatsDto>("api/v1/chat/statistics", cancellationToken) ?? new ChatStatsDto();
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
        _preloadedChats = null;
        // Invalidate the chat list cache so the next GetChatsAsync call returns fresh data
        _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);
        ChatCreated?.Invoke(chat);
    }

    private void OnMessageReceived(ChatMessageDto message)
    {
        // Invalidate cached messages for this chat so the next GetMessagesAsync fetches fresh data
        _performanceService.InvalidateCachePattern(CacheKeys.ChatMessages(message.ChatId));
        MessageReceived?.Invoke(message);
    }

    private void OnMessageEdited(ChatMessageDto updatedMessage)
    {
        MessageEdited?.Invoke(updatedMessage);
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

    private void OnUserOnlineStatusChanged(Guid userId, bool isOnline)
    {
        UserOnlineStatusChanged?.Invoke(userId, isOnline);
    }

    private void OnAddedToChat(object addedToChatData)
    {
        _preloadedChats = null;
        // Invalidate chat list so the next GetChatsAsync call picks up the new chat.
        _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);

        // Try to deserialize the payload as a ChatResponseDto for a richer event.
        if (addedToChatData is System.Text.Json.JsonElement element)
        {
            try
            {
                var chat = System.Text.Json.JsonSerializer.Deserialize<ChatResponseDto>(element.GetRawText(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (chat is not null) { AddedToChat?.Invoke(chat); return; }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize AddedToChat payload as ChatResponseDto");
            }
        }

        _logger.LogInformation("AddedToChat event received; chat list cache invalidated");
    }

    private void OnRemovedFromChat(object removedFromChatData)
    {
        _preloadedChats = null;
        // Invalidate chat list so the removed chat disappears on next load.
        _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);

        if (removedFromChatData is System.Text.Json.JsonElement element &&
            element.TryGetProperty("chatId", out var chatIdEl) &&
            chatIdEl.TryGetGuid(out var chatId))
        {
            _performanceService.InvalidateCachePattern(CacheKeys.ChatMessages(chatId));
            RemovedFromChat?.Invoke(chatId);
            return;
        }

        _logger.LogInformation("RemovedFromChat event received; chat list cache invalidated");
    }

    private void OnChatDeleted(object chatDeletedData)
    {
        _preloadedChats = null;
        // SignalR serializes with camelCase by default; try both casings for robustness.
        if (chatDeletedData is System.Text.Json.JsonElement element
            && (element.TryGetProperty("chatId", out var el) || element.TryGetProperty("ChatId", out el))
            && el.TryGetGuid(out var chatId))
        {
            InvalidateChatCaches(chatId);
            RemovedFromChat?.Invoke(chatId);
            return;
        }

        _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);
        _logger.LogInformation("ChatDeleted event received; chat list cache invalidated");
    }

    private void InvalidateChatCaches(Guid chatId)
    {
        _preloadedChats = null;
        _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);
        _performanceService.InvalidateCachePattern(CacheKeys.ChatMessages(chatId));
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

    public async Task<MessageAttachmentDto?> UploadFileAsync(Guid chatId, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        const long maxFileSize = 50 * 1024 * 1024; // 50 MB
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(chatId.ToString()), "chatId");

            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/chat/upload") { Content = content };
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("File upload failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FileUploadResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result is null || !result.Success)
            {
                _logger.LogWarning("File upload failed: {ErrorMessage}", result?.ErrorMessage ?? "unknown error");
                return null;
            }

            return new MessageAttachmentDto
            {
                Id = result.AttachmentId,
                FileName = result.FileName,
                FileUrl = result.FileUrl ?? string.Empty,
                FileSize = result.FileSize,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to chat {ChatId}", chatId);
            throw;
        }
    }

    // Minimal DTO to deserialize the server's FileUploadResultDto
    private sealed class FileUploadResult
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public long FileSize { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public async Task<bool> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{chatId}", cancellationToken);
            InvalidateChatCaches(chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat {ChatId}", chatId);
            throw;
        }
    }

    public async Task<int> MergeDuplicateChatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<object, DmMergeResultDto>(
                $"{BaseUrl}/merge-duplicates", new { }, cancellationToken);

            var merged = result?.MergedThreadCount ?? 0;
            if (merged > 0)
            {
                // Invalidate the chat list so the sidebar is refreshed with the merged threads.
                _performanceService.InvalidateCachePattern(CacheKeys.CHAT_LIST);
                _logger.LogInformation("Merged {Count} duplicate DM thread(s).", merged);
            }
            return merged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MergeDuplicateChatsAsync failed (non-critical).");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReportMessageAsync(Guid messageId, ReportMessageDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<ReportMessageDto, MessageOperationResultDto>(
                $"{BaseUrl}/messages/{messageId}/report", dto, cancellationToken);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting message {MessageId}", messageId);
            return false;
        }
    }
}