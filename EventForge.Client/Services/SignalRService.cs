using EventForge.DTOs.Chat;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR.Client;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing SignalR connections and real-time communication.
/// Handles audit logs, notifications, and chat functionality with localization support.
/// </summary>
[Obsolete("This service is deprecated. Use IRealtimeService (OptimizedSignalRService) instead for better performance with connection pooling, event batching, and optimized retry logic. See docs/decision-log/ONDA4_REALTIME_SERVICE_UNIFICATION.md for migration details.")]
public class SignalRService : IAsyncDisposable
{
    private const string BaseUrl = "hubs";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _auditHubConnection;
    private HubConnection? _notificationHubConnection;
    private HubConnection? _chatHubConnection;
    private HubConnection? _documentCollaborationHubConnection;

    #region Audit Log Events
    public event Action<object>? AuditLogUpdated;
    public event Action<object>? UserStatusChanged;
    public event Action<object>? UserRolesChanged;
    public event Action<object>? PasswordChangeForced;
    public event Action<object>? BackupStatusChanged;
    #endregion

    #region Notification Events
    public event Action<NotificationResponseDto>? NotificationReceived;
    public event Action<NotificationResponseDto>? SystemNotificationReceived;
    public event Action<Guid>? NotificationAcknowledged;
    public event Action<Guid>? NotificationSilenced;
    public event Action<Guid>? NotificationArchived;
    public event Action<UpdateNotificationStatusDto>? NotificationStatusUpdated;
    public event Action<BulkNotificationActionDto>? NotificationsBulkUpdated;
    public event Action<NotificationStatsDto>? NotificationStatsReceived;
    public event Action<List<NotificationTypes>>? NotificationSubscriptionConfirmed;
    public event Action<string>? NotificationLocaleUpdated;
    #endregion

    #region Chat Events
    public event Action<ChatResponseDto>? ChatCreated;
    public event Action<ChatMessageDto>? MessageReceived;
    public event Action<EditMessageDto>? MessageEdited;
    public event Action<object>? MessageDeleted;
    public event Action<object>? MessageRead;
    public event Action<TypingIndicatorDto>? TypingIndicator;
    public event Action<object>? UserJoinedChat;
    public event Action<object>? UserLeftChat;
    public event Action<object>? AddedToChat;
    public event Action<object>? RemovedFromChat;
    public event Action<UpdateChatMembersDto>? ChatMembersUpdated;
    public event Action<object>? ChatUpdated;
    public event Action<ChatModerationActionDto>? ChatModerated;
    public event Action<ChatModerationActionDto>? ChatDeleted;
    public event Action<ChatStatsDto>? ChatStatsReceived;
    public event Action<string>? ChatLocaleUpdated;
    #endregion

    #region Document Collaboration Events
    public event Action<DocumentCommentDto>? CommentCreated;
    public event Action<DocumentCommentDto>? CommentUpdated;
    public event Action<object>? CommentDeleted;
    public event Action<object>? CommentResolved;
    public event Action<object>? CommentReopened;
    public event Action<object>? TaskAssigned;
    public event Action<object>? UserMentioned;
    public event Action<object>? UserJoinedDocument;
    public event Action<object>? UserLeftDocument;
    public event Action<object>? DocumentTypingIndicator;
    
    // Document lock events
    public event Action<object>? DocumentLocked;
    public event Action<object>? DocumentUnlocked;
    #endregion

    public SignalRService(IHttpClientFactory httpClientFactory, IAuthService authService, ILogger<SignalRService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;
    }

    #region Connection Status Properties

    /// <summary>
    /// Gets whether the audit hub connection is connected.
    /// </summary>
    public bool IsAuditConnected => _auditHubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Gets whether the notification hub connection is connected.
    /// </summary>
    public bool IsNotificationConnected => _notificationHubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Gets whether the chat hub connection is connected.
    /// </summary>
    public bool IsChatConnected => _chatHubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Gets whether the document collaboration hub connection is connected.
    /// </summary>
    public bool IsDocumentCollaborationConnected => _documentCollaborationHubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Gets whether all hub connections are connected.
    /// </summary>
    public bool IsAllConnected => IsAuditConnected && IsNotificationConnected && IsChatConnected && IsDocumentCollaborationConnected;

    #endregion

    #region Connection Management

    /// <summary>
    /// Starts all SignalR connections (audit, notifications, chat, document collaboration).
    /// </summary>
    public async Task StartAllConnectionsAsync()
    {
        await StartAuditConnectionAsync();
        await StartNotificationConnectionAsync();
        await StartChatConnectionAsync();
        await StartDocumentCollaborationConnectionAsync();
    }

    /// <summary>
    /// Stops all SignalR connections.
    /// </summary>
    public async Task StopAllConnectionsAsync()
    {
        await StopAuditConnectionAsync();
        await StopNotificationConnectionAsync();
        await StopChatConnectionAsync();
        await StopDocumentCollaborationConnectionAsync();
    }

    #endregion

    #region Audit Log Hub Methods

    /// <summary>
    /// Starts the SignalR connection to the audit log hub.
    /// </summary>
    public async Task StartAuditConnectionAsync()
    {
        if (_auditHubConnection != null)
        {
            return; // Already connected
        }

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot start audit SignalR connection: no access token available");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var hubUrl = new Uri(httpClient.BaseAddress!, "/hubs/audit-log").ToString();

            _auditHubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            // Register audit event handlers
            RegisterAuditEventHandlers();

            // Handle connection events
            _auditHubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("Audit SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                await JoinAuditLogGroup();
            };

            _auditHubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Audit SignalR connection closed");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await StartAuditConnectionAsync(); // Attempt to reconnect
            };

            await _auditHubConnection.StartAsync();
            await JoinAuditLogGroup();

            _logger.LogInformation("Audit SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audit SignalR connection");
        }
    }

    /// <summary>
    /// Stops the audit SignalR connection.
    /// </summary>
    public async Task StopAuditConnectionAsync()
    {
        if (_auditHubConnection != null)
        {
            await _auditHubConnection.DisposeAsync();
            _auditHubConnection = null;
            _logger.LogInformation("Audit SignalR connection stopped");
        }
    }

    /// <summary>
    /// Joins the audit log group for receiving updates.
    /// </summary>
    private async Task JoinAuditLogGroup()
    {
        if (_auditHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _auditHubConnection.InvokeAsync("JoinAuditLogGroup");
                _logger.LogInformation("Joined audit log group");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join audit log group");
            }
        }
    }

    /// <summary>
    /// Registers event handlers for audit hub events.
    /// </summary>
    private void RegisterAuditEventHandlers()
    {
        if (_auditHubConnection == null) return;

        _ = _auditHubConnection.On<object>("AuditLogUpdated", (data) =>
        {
            _logger.LogInformation("Audit log updated: {Data}", data);
            AuditLogUpdated?.Invoke(data);
        });

        _ = _auditHubConnection.On<object>("UserStatusChanged", (data) =>
        {
            _logger.LogInformation("User status changed: {Data}", data);
            UserStatusChanged?.Invoke(data);
        });

        _ = _auditHubConnection.On<object>("UserRolesChanged", (data) =>
        {
            _logger.LogInformation("User roles changed: {Data}", data);
            UserRolesChanged?.Invoke(data);
        });

        _ = _auditHubConnection.On<object>("PasswordChangeForced", (data) =>
        {
            _logger.LogInformation("Password change forced: {Data}", data);
            PasswordChangeForced?.Invoke(data);
        });

        _ = _auditHubConnection.On<object>("BackupStatusChanged", (data) =>
        {
            _logger.LogInformation("Backup status changed: {Data}", data);
            BackupStatusChanged?.Invoke(data);
        });
    }

    #endregion

    #region Notification Hub Methods

    /// <summary>
    /// Starts the SignalR connection to the notification hub.
    /// </summary>
    public async Task StartNotificationConnectionAsync()
    {
        if (_notificationHubConnection != null)
        {
            return; // Already connected
        }

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot start notification SignalR connection: no access token available");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var hubUrl = new Uri(httpClient.BaseAddress!, "/hubs/notifications").ToString();

            _notificationHubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            // Register notification event handlers
            RegisterNotificationEventHandlers();

            // Handle connection events
            _notificationHubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("Notification SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                // TODO: Re-subscribe to notification types based on user preferences
            };

            _notificationHubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Notification SignalR connection closed");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await StartNotificationConnectionAsync(); // Attempt to reconnect
            };

            await _notificationHubConnection.StartAsync();
            _logger.LogInformation("Notification SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start notification SignalR connection");
        }
    }

    /// <summary>
    /// Stops the notification SignalR connection.
    /// </summary>
    public async Task StopNotificationConnectionAsync()
    {
        if (_notificationHubConnection != null)
        {
            await _notificationHubConnection.DisposeAsync();
            _notificationHubConnection = null;
            _logger.LogInformation("Notification SignalR connection stopped");
        }
    }

    /// <summary>
    /// Subscribes to specific notification types.
    /// </summary>
    /// <param name="notificationTypes">List of notification types to subscribe to</param>
    public async Task SubscribeToNotificationTypesAsync(List<NotificationTypes> notificationTypes)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("SubscribeToNotificationTypes", notificationTypes);
                _logger.LogInformation("Subscribed to notification types: {Types}", string.Join(", ", notificationTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to notification types");
            }
        }
    }

    /// <summary>
    /// Unsubscribes from specific notification types.
    /// </summary>
    /// <param name="notificationTypes">List of notification types to unsubscribe from</param>
    public async Task UnsubscribeFromNotificationTypesAsync(List<NotificationTypes> notificationTypes)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("UnsubscribeFromNotificationTypes", notificationTypes);
                _logger.LogInformation("Unsubscribed from notification types: {Types}", string.Join(", ", notificationTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from notification types");
            }
        }
    }

    /// <summary>
    /// Acknowledges a notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to acknowledge</param>
    public async Task AcknowledgeNotificationAsync(Guid notificationId)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("AcknowledgeNotification", notificationId);
                _logger.LogInformation("Acknowledged notification {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acknowledge notification {NotificationId}", notificationId);
            }
        }
    }

    /// <summary>
    /// Silences a notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to silence</param>
    /// <param name="reason">Optional reason for silencing</param>
    public async Task SilenceNotificationAsync(Guid notificationId, string? reason = null)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("SilenceNotification", notificationId, reason);
                _logger.LogInformation("Silenced notification {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to silence notification {NotificationId}", notificationId);
            }
        }
    }

    /// <summary>
    /// Archives a notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to archive</param>
    public async Task ArchiveNotificationAsync(Guid notificationId)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("ArchiveNotification", notificationId);
                _logger.LogInformation("Archived notification {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive notification {NotificationId}", notificationId);
            }
        }
    }

    /// <summary>
    /// Performs bulk action on multiple notifications.
    /// </summary>
    /// <param name="action">Bulk action to perform</param>
    public async Task BulkNotificationActionAsync(BulkNotificationActionDto action)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("BulkNotificationAction", action);
                _logger.LogInformation("Performed bulk notification action {Action} on {Count} notifications",
                    action.Action, action.NotificationIds?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform bulk notification action");
            }
        }
    }

    /// <summary>
    /// Updates notification locale preference.
    /// </summary>
    /// <param name="locale">Preferred locale</param>
    public async Task UpdateNotificationLocaleAsync(string locale)
    {
        if (_notificationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _notificationHubConnection.InvokeAsync("UpdateNotificationLocale", locale);
                _logger.LogInformation("Updated notification locale to {Locale}", locale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update notification locale");
            }
        }
    }

    /// <summary>
    /// Registers event handlers for notification hub events.
    /// </summary>
    private void RegisterNotificationEventHandlers()
    {
        if (_notificationHubConnection == null) return;

        _ = _notificationHubConnection.On<NotificationResponseDto>("NotificationReceived", (notification) =>
        {
            _logger.LogInformation("Notification received: {NotificationId}", notification.Id);
            NotificationReceived?.Invoke(notification);
        });

        _ = _notificationHubConnection.On<NotificationResponseDto>("SystemNotificationReceived", (notification) =>
        {
            _logger.LogInformation("System notification received: {NotificationId}", notification.Id);
            SystemNotificationReceived?.Invoke(notification);
        });

        _ = _notificationHubConnection.On<Guid>("NotificationAcknowledged", (notificationId) =>
        {
            NotificationAcknowledged?.Invoke(notificationId);
        });

        _ = _notificationHubConnection.On<Guid>("NotificationSilenced", (notificationId) =>
        {
            NotificationSilenced?.Invoke(notificationId);
        });

        _ = _notificationHubConnection.On<Guid>("NotificationArchived", (notificationId) =>
        {
            NotificationArchived?.Invoke(notificationId);
        });

        _ = _notificationHubConnection.On<UpdateNotificationStatusDto>("NotificationStatusUpdated", (update) =>
        {
            NotificationStatusUpdated?.Invoke(update);
        });

        _ = _notificationHubConnection.On<BulkNotificationActionDto>("NotificationsBulkUpdated", (bulkAction) =>
        {
            NotificationsBulkUpdated?.Invoke(bulkAction);
        });

        _ = _notificationHubConnection.On<NotificationStatsDto>("NotificationStatsReceived", (stats) =>
        {
            NotificationStatsReceived?.Invoke(stats);
        });

        _ = _notificationHubConnection.On<List<NotificationTypes>>("SubscriptionConfirmed", (types) =>
        {
            NotificationSubscriptionConfirmed?.Invoke(types);
        });

        _ = _notificationHubConnection.On<string>("LocaleUpdated", (locale) =>
        {
            NotificationLocaleUpdated?.Invoke(locale);
        });
    }

    #endregion

    #region Chat Hub Methods

    /// <summary>
    /// Starts the SignalR connection to the chat hub.
    /// </summary>
    public async Task StartChatConnectionAsync()
    {
        if (_chatHubConnection != null)
        {
            return; // Already connected
        }

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot start chat SignalR connection: no access token available");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var hubUrl = new Uri(httpClient.BaseAddress!, "/hubs/chat").ToString();

            _chatHubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            // Register chat event handlers
            RegisterChatEventHandlers();

            // Handle connection events
            _chatHubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("Chat SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                // TODO: Rejoin active chats
            };

            _chatHubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Chat SignalR connection closed");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await StartChatConnectionAsync(); // Attempt to reconnect
            };

            await _chatHubConnection.StartAsync();
            _logger.LogInformation("Chat SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start chat SignalR connection");
        }
    }

    /// <summary>
    /// Stops the chat SignalR connection.
    /// </summary>
    public async Task StopChatConnectionAsync()
    {
        if (_chatHubConnection != null)
        {
            await _chatHubConnection.DisposeAsync();
            _chatHubConnection = null;
            _logger.LogInformation("Chat SignalR connection stopped");
        }
    }

    /// <summary>
    /// Starts the document collaboration SignalR connection.
    /// </summary>
    public async Task StartDocumentCollaborationConnectionAsync()
    {
        if (_documentCollaborationHubConnection != null)
        {
            _logger.LogWarning("Document collaboration SignalR connection already started");
            return;
        }

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot start document collaboration connection: no authentication token");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var hubUrl = new Uri(httpClient.BaseAddress!, "/hubs/document-collaboration").ToString();

            _documentCollaborationHubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterDocumentCollaborationEventHandlers();

            await _documentCollaborationHubConnection.StartAsync();
            _logger.LogInformation("Document collaboration SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start document collaboration SignalR connection");
        }
    }

    /// <summary>
    /// Stops the document collaboration SignalR connection.
    /// </summary>
    public async Task StopDocumentCollaborationConnectionAsync()
    {
        if (_documentCollaborationHubConnection != null)
        {
            await _documentCollaborationHubConnection.DisposeAsync();
            _documentCollaborationHubConnection = null;
            _logger.LogInformation("Document collaboration SignalR connection stopped");
        }
    }

    /// <summary>
    /// Creates a new chat.
    /// </summary>
    /// <param name="createChatDto">Chat creation parameters</param>
    public async Task CreateChatAsync(CreateChatDto createChatDto)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("CreateChat", createChatDto);
                _logger.LogInformation("Created {ChatType} chat with {ParticipantCount} participants",
                    createChatDto.Type, createChatDto.ParticipantIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chat");
            }
        }
    }

    /// <summary>
    /// Joins an existing chat.
    /// </summary>
    /// <param name="chatId">ID of the chat to join</param>
    public async Task JoinChatAsync(Guid chatId)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("JoinChat", chatId);
                _logger.LogInformation("Joined chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join chat {ChatId}", chatId);
            }
        }
    }

    /// <summary>
    /// Leaves a chat.
    /// </summary>
    /// <param name="chatId">ID of the chat to leave</param>
    public async Task LeaveChatAsync(Guid chatId)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("LeaveChat", chatId);
                _logger.LogInformation("Left chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave chat {ChatId}", chatId);
            }
        }
    }

    /// <summary>
    /// Sends a message in a chat.
    /// </summary>
    /// <param name="messageDto">Message to send</param>
    public async Task SendMessageAsync(SendMessageDto messageDto)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("SendMessage", messageDto);
                _logger.LogInformation("Sent message in chat {ChatId}", messageDto.ChatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message in chat {ChatId}", messageDto.ChatId);
            }
        }
    }

    /// <summary>
    /// Edits a message.
    /// </summary>
    /// <param name="editDto">Message edit parameters</param>
    public async Task EditMessageAsync(EditMessageDto editDto)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("EditMessage", editDto);
                _logger.LogInformation("Edited message {MessageId}", editDto.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit message {MessageId}", editDto.MessageId);
            }
        }
    }

    /// <summary>
    /// Deletes a message.
    /// </summary>
    /// <param name="messageId">ID of the message to delete</param>
    /// <param name="reason">Optional reason for deletion</param>
    public async Task DeleteMessageAsync(Guid messageId, string? reason = null)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("DeleteMessage", messageId, reason);
                _logger.LogInformation("Deleted message {MessageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            }
        }
    }

    /// <summary>
    /// Marks a message as read.
    /// </summary>
    /// <param name="messageId">ID of the message to mark as read</param>
    public async Task MarkMessageAsReadAsync(Guid messageId)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("MarkMessageAsRead", messageId);
                _logger.LogInformation("Marked message {MessageId} as read", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark message {MessageId} as read", messageId);
            }
        }
    }

    /// <summary>
    /// Sends typing indicator.
    /// </summary>
    /// <param name="chatId">ID of the chat where user is typing</param>
    /// <param name="isTyping">Whether user is currently typing</param>
    public async Task SendTypingIndicatorAsync(Guid chatId, bool isTyping)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("SendTypingIndicator", chatId, isTyping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send typing indicator for chat {ChatId}", chatId);
            }
        }
    }

    /// <summary>
    /// Updates chat members.
    /// </summary>
    /// <param name="updateMembersDto">Member update parameters</param>
    public async Task UpdateChatMembersAsync(UpdateChatMembersDto updateMembersDto)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("UpdateChatMembers", updateMembersDto);
                _logger.LogInformation("Updated members for chat {ChatId}", updateMembersDto.ChatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update chat members for chat {ChatId}", updateMembersDto.ChatId);
            }
        }
    }

    /// <summary>
    /// Updates chat locale preference.
    /// </summary>
    /// <param name="locale">Preferred locale</param>
    public async Task UpdateChatLocaleAsync(string locale)
    {
        if (_chatHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _chatHubConnection.InvokeAsync("UpdateChatLocale", locale);
                _logger.LogInformation("Updated chat locale to {Locale}", locale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update chat locale");
            }
        }
    }

    /// <summary>
    /// Registers event handlers for chat hub events.
    /// </summary>
    private void RegisterChatEventHandlers()
    {
        if (_chatHubConnection == null) return;

        _ = _chatHubConnection.On<ChatResponseDto>("ChatCreated", (chat) =>
        {
            _logger.LogInformation("Chat created: {ChatId}", chat.Id);
            ChatCreated?.Invoke(chat);
        });

        _ = _chatHubConnection.On<ChatMessageDto>("MessageReceived", (message) =>
        {
            _logger.LogInformation("Message received: {MessageId} in chat {ChatId}", message.Id, message.ChatId);
            MessageReceived?.Invoke(message);
        });

        _ = _chatHubConnection.On<EditMessageDto>("MessageEdited", (editDto) =>
        {
            MessageEdited?.Invoke(editDto);
        });

        _ = _chatHubConnection.On<object>("MessageDeleted", (data) =>
        {
            MessageDeleted?.Invoke(data);
        });

        _ = _chatHubConnection.On<object>("MessageRead", (data) =>
        {
            MessageRead?.Invoke(data);
        });

        _ = _chatHubConnection.On<TypingIndicatorDto>("TypingIndicator", (indicator) =>
        {
            TypingIndicator?.Invoke(indicator);
        });

        _ = _chatHubConnection.On<object>("UserJoinedChat", (data) =>
        {
            UserJoinedChat?.Invoke(data);
        });

        _ = _chatHubConnection.On<object>("UserLeftChat", (data) =>
        {
            UserLeftChat?.Invoke(data);
        });

        _ = _chatHubConnection.On<object>("AddedToChat", (data) =>
        {
            AddedToChat?.Invoke(data);
        });

        _ = _chatHubConnection.On<object>("RemovedFromChat", (data) =>
        {
            RemovedFromChat?.Invoke(data);
        });

        _ = _chatHubConnection.On<UpdateChatMembersDto>("ChatMembersUpdated", (update) =>
        {
            ChatMembersUpdated?.Invoke(update);
        });

        _ = _chatHubConnection.On<object>("ChatUpdated", (data) =>
        {
            ChatUpdated?.Invoke(data);
        });

        _ = _chatHubConnection.On<ChatModerationActionDto>("ChatModerated", (moderation) =>
        {
            ChatModerated?.Invoke(moderation);
        });

        _ = _chatHubConnection.On<ChatModerationActionDto>("ChatDeleted", (deletion) =>
        {
            ChatDeleted?.Invoke(deletion);
        });

        _ = _chatHubConnection.On<ChatStatsDto>("ChatStatsReceived", (stats) =>
        {
            ChatStatsReceived?.Invoke(stats);
        });

        _ = _chatHubConnection.On<string>("ChatLocaleUpdated", (locale) =>
        {
            ChatLocaleUpdated?.Invoke(locale);
        });
    }

    #endregion

    #region Document Collaboration Methods

    /// <summary>
    /// Joins a document collaboration room to receive real-time updates.
    /// </summary>
    /// <param name="documentId">ID of the document to subscribe to</param>
    public async Task JoinDocumentAsync(Guid documentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("JoinDocument", documentId);
                _logger.LogInformation("Joined document {DocumentId} collaboration room", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join document {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// Leaves a document collaboration room.
    /// </summary>
    /// <param name="documentId">ID of the document to unsubscribe from</param>
    public async Task LeaveDocumentAsync(Guid documentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("LeaveDocument", documentId);
                _logger.LogInformation("Left document {DocumentId} collaboration room", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave document {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// Requests an exclusive edit lock for a document.
    /// </summary>
    /// <param name="documentId">ID of the document to lock</param>
    /// <returns>True if lock was acquired successfully, false otherwise</returns>
    public async Task<bool> RequestDocumentEditLockAsync(Guid documentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                var lockAcquired = await _documentCollaborationHubConnection
                    .InvokeAsync<bool>("RequestEditLock", documentId);
                
                _logger.LogInformation(
                    "Lock request for document {DocumentId}: {Result}",
                    documentId,
                    lockAcquired ? "Acquired" : "Failed");
                
                return lockAcquired;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire lock for document {DocumentId}", documentId);
                throw;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Releases the edit lock for a document.
    /// </summary>
    /// <param name="documentId">ID of the document to unlock</param>
    public async Task ReleaseDocumentEditLockAsync(Guid documentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("ReleaseEditLock", documentId);
                _logger.LogInformation("Released lock on document {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock for document {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// Creates a comment on a document.
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    public async Task CreateDocumentCommentAsync(CreateDocumentCommentDto createDto)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("CreateComment", createDto);
                _logger.LogInformation("Created comment on document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create document comment");
            }
        }
    }

    /// <summary>
    /// Updates a comment on a document.
    /// </summary>
    /// <param name="commentId">ID of the comment to update</param>
    /// <param name="updateDto">Comment update data</param>
    public async Task UpdateDocumentCommentAsync(Guid commentId, UpdateDocumentCommentDto updateDto)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("UpdateComment", commentId, updateDto);
                _logger.LogInformation("Updated comment {CommentId}", commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update comment {CommentId}", commentId);
            }
        }
    }

    /// <summary>
    /// Deletes a comment from a document.
    /// </summary>
    /// <param name="commentId">ID of the comment to delete</param>
    public async Task DeleteDocumentCommentAsync(Guid commentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("DeleteComment", commentId);
                _logger.LogInformation("Deleted comment {CommentId}", commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete comment {CommentId}", commentId);
            }
        }
    }

    /// <summary>
    /// Resolves a comment.
    /// </summary>
    /// <param name="commentId">ID of the comment to resolve</param>
    /// <param name="resolveDto">Resolution data</param>
    public async Task ResolveDocumentCommentAsync(Guid commentId, ResolveCommentDto resolveDto)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("ResolveComment", commentId, resolveDto);
                _logger.LogInformation("Resolved comment {CommentId}", commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve comment {CommentId}", commentId);
            }
        }
    }

    /// <summary>
    /// Reopens a resolved comment.
    /// </summary>
    /// <param name="commentId">ID of the comment to reopen</param>
    public async Task ReopenDocumentCommentAsync(Guid commentId)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("ReopenComment", commentId);
                _logger.LogInformation("Reopened comment {CommentId}", commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reopen comment {CommentId}", commentId);
            }
        }
    }

    /// <summary>
    /// Sends typing indicator for document collaboration.
    /// </summary>
    /// <param name="documentId">ID of the document where user is typing</param>
    /// <param name="isTyping">Whether user is currently typing</param>
    public async Task SendDocumentTypingIndicatorAsync(Guid documentId, bool isTyping)
    {
        if (_documentCollaborationHubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _documentCollaborationHubConnection.InvokeAsync("SendTypingIndicator", documentId, isTyping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send typing indicator for document {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// Registers event handlers for document collaboration hub events.
    /// </summary>
    private void RegisterDocumentCollaborationEventHandlers()
    {
        if (_documentCollaborationHubConnection == null) return;

        _ = _documentCollaborationHubConnection.On<DocumentCommentDto>("CommentCreated", (comment) =>
        {
            _logger.LogInformation("Comment created: {CommentId}", comment.Id);
            CommentCreated?.Invoke(comment);
        });

        _ = _documentCollaborationHubConnection.On<DocumentCommentDto>("CommentUpdated", (comment) =>
        {
            _logger.LogInformation("Comment updated: {CommentId}", comment.Id);
            CommentUpdated?.Invoke(comment);
        });

        _ = _documentCollaborationHubConnection.On<object>("CommentDeleted", (data) =>
        {
            CommentDeleted?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("CommentResolved", (data) =>
        {
            CommentResolved?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("CommentReopened", (data) =>
        {
            CommentReopened?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("TaskAssigned", (data) =>
        {
            TaskAssigned?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("UserMentioned", (data) =>
        {
            UserMentioned?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("UserJoinedDocument", (data) =>
        {
            UserJoinedDocument?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("UserLeftDocument", (data) =>
        {
            UserLeftDocument?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("TypingIndicator", (data) =>
        {
            DocumentTypingIndicator?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("DocumentLocked", (data) =>
        {
            _logger.LogInformation("Document locked");
            DocumentLocked?.Invoke(data);
        });

        _ = _documentCollaborationHubConnection.On<object>("DocumentUnlocked", (data) =>
        {
            _logger.LogInformation("Document unlocked");
            DocumentUnlocked?.Invoke(data);
        });
    }

    #endregion

    #region Legacy Compatibility Methods

    /// <summary>
    /// Legacy method - starts the audit connection for backward compatibility.
    /// </summary>
    [Obsolete("Use StartAllConnectionsAsync() or StartAuditConnectionAsync() instead")]
    public async Task StartConnectionAsync()
    {
        await StartAuditConnectionAsync();
    }

    /// <summary>
    /// Stops the SignalR connection.
    /// </summary>
    [Obsolete("Use StopAllConnectionsAsync() instead")]
    public async Task StopConnectionAsync()
    {
        await StopAuditConnectionAsync();
    }

    #endregion

    #region Connection State Properties

    /// <summary>
    /// Gets the current audit connection state.
    /// </summary>
    public HubConnectionState AuditConnectionState => _auditHubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets the current notification connection state.
    /// </summary>
    public HubConnectionState NotificationConnectionState => _notificationHubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets the current chat connection state.
    /// </summary>
    public HubConnectionState ChatConnectionState => _chatHubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets the current document collaboration connection state.
    /// </summary>
    public HubConnectionState DocumentCollaborationConnectionState => _documentCollaborationHubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Legacy property - gets the audit connection state for backward compatibility.
    /// </summary>
    [Obsolete("Use AuditConnectionState instead")]
    public HubConnectionState ConnectionState => AuditConnectionState;

    /// <summary>
    /// Legacy property - checks if audit connection is active for backward compatibility.
    /// </summary>
    [Obsolete("Use IsAuditConnected instead")]
    public bool IsConnected => IsAuditConnected;

    /// <summary>
    /// Checks if all connections are active.
    /// </summary>
    public bool AreAllConnectionsActive => IsAuditConnected && IsNotificationConnected && IsChatConnected && IsDocumentCollaborationConnected;

    #endregion

    #region Dispose

    public async ValueTask DisposeAsync()
    {
        if (_auditHubConnection != null)
        {
            await _auditHubConnection.DisposeAsync();
        }
        if (_notificationHubConnection != null)
        {
            await _notificationHubConnection.DisposeAsync();
        }
        if (_chatHubConnection != null)
        {
            await _chatHubConnection.DisposeAsync();
        }
        if (_documentCollaborationHubConnection != null)
        {
            await _documentCollaborationHubConnection.DisposeAsync();
        }
    }

    #endregion
}