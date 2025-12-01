using EventForge.DTOs.Chat;
using EventForge.DTOs.Notifications;

namespace EventForge.Client.Services;

/// <summary>
/// Unified interface for real-time communication services.
/// Provides optimized SignalR connections with batching, backoff, and health monitoring.
/// </summary>
public interface IRealtimeService
{
    #region Connection Management

    /// <summary>
    /// Starts all SignalR connections (audit, notification, chat).
    /// </summary>
    Task StartAllConnectionsAsync();

    /// <summary>
    /// Stops all SignalR connections gracefully.
    /// </summary>
    Task StopAllConnectionsAsync();

    /// <summary>
    /// Gets whether all connections are active.
    /// </summary>
    bool IsAllConnected { get; }

    /// <summary>
    /// Gets whether the audit connection is active.
    /// </summary>
    bool IsAuditConnected { get; }

    /// <summary>
    /// Gets whether the notification connection is active.
    /// </summary>
    bool IsNotificationConnected { get; }

    /// <summary>
    /// Gets whether the chat connection is active.
    /// </summary>
    bool IsChatConnected { get; }

    #endregion

    #region Batched Events (Optimized for Performance)

    /// <summary>
    /// Batched audit log updates (fires every 100ms with accumulated events).
    /// </summary>
    event Action<List<object>>? BatchedAuditLogUpdates;

    /// <summary>
    /// Batched notifications (fires every 100ms with accumulated events).
    /// </summary>
    event Action<List<NotificationResponseDto>>? BatchedNotifications;

    /// <summary>
    /// Batched chat messages (fires every 100ms with accumulated events).
    /// </summary>
    event Action<List<ChatMessageDto>>? BatchedChatMessages;

    #endregion

    #region Individual Events (Backward Compatibility)

    /// <summary>
    /// Individual notification received (for backward compatibility).
    /// Use BatchedNotifications for better performance.
    /// </summary>
    event Action<NotificationResponseDto>? NotificationReceived;

    /// <summary>
    /// Individual chat message received (for backward compatibility).
    /// Use BatchedChatMessages for better performance.
    /// </summary>
    event Action<ChatMessageDto>? MessageReceived;

    /// <summary>
    /// Typing indicator (not batched for responsiveness).
    /// </summary>
    event Action<TypingIndicatorDto>? TypingIndicator;

    /// <summary>
    /// Audit log updated (for backward compatibility).
    /// </summary>
    event Action<object>? AuditLogUpdated;

    /// <summary>
    /// Notification acknowledged.
    /// </summary>
    event Action<Guid>? NotificationAcknowledged;

    /// <summary>
    /// Notification archived.
    /// </summary>
    event Action<Guid>? NotificationArchived;

    /// <summary>
    /// Chat created.
    /// </summary>
    event Action<ChatResponseDto>? ChatCreated;

    /// <summary>
    /// Message edited.
    /// </summary>
    event Action<EditMessageDto>? MessageEdited;

    /// <summary>
    /// Message deleted.
    /// </summary>
    event Action<object>? MessageDeleted;

    /// <summary>
    /// Message read.
    /// </summary>
    event Action<object>? MessageRead;

    /// <summary>
    /// User joined chat.
    /// </summary>
    event Action<object>? UserJoinedChat;

    /// <summary>
    /// User left chat.
    /// </summary>
    event Action<object>? UserLeftChat;

    #endregion

    #region Chat Methods

    /// <summary>
    /// Sends a chat message with optimized delivery.
    /// </summary>
    Task SendChatMessageAsync(SendMessageDto messageDto);

    /// <summary>
    /// Sends typing indicator with debouncing (300ms).
    /// </summary>
    Task SendTypingIndicatorAsync(Guid chatId, bool isTyping);

    /// <summary>
    /// Joins a chat room.
    /// </summary>
    Task JoinChatAsync(Guid chatId);

    /// <summary>
    /// Leaves a chat room.
    /// </summary>
    Task LeaveChatAsync(Guid chatId);

    /// <summary>
    /// Creates a new chat.
    /// </summary>
    Task CreateChatAsync(CreateChatDto createChatDto);

    /// <summary>
    /// Edits a message.
    /// </summary>
    Task EditMessageAsync(EditMessageDto editDto);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteMessageAsync(Guid messageId, string? reason = null);

    /// <summary>
    /// Marks message as read.
    /// </summary>
    Task MarkMessageAsReadAsync(Guid messageId);

    #endregion

    #region Notification Methods

    /// <summary>
    /// Subscribes to specific notification types.
    /// </summary>
    Task SubscribeToNotificationTypesAsync(List<NotificationTypes> notificationTypes);

    /// <summary>
    /// Unsubscribes from specific notification types.
    /// </summary>
    Task UnsubscribeFromNotificationTypesAsync(List<NotificationTypes> notificationTypes);

    /// <summary>
    /// Acknowledges a notification.
    /// </summary>
    Task AcknowledgeNotificationAsync(Guid notificationId);

    /// <summary>
    /// Archives a notification.
    /// </summary>
    Task ArchiveNotificationAsync(Guid notificationId);

    #endregion

    #region Audit Methods

    /// <summary>
    /// Starts audit connection and joins audit log group.
    /// </summary>
    Task StartAuditConnectionAsync();

    /// <summary>
    /// Starts notification connection.
    /// </summary>
    Task StartNotificationConnectionAsync();

    /// <summary>
    /// Starts chat connection.
    /// </summary>
    Task StartChatConnectionAsync();

    #endregion
}