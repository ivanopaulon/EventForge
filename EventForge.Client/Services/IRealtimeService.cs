using EventForge.Client.Services.Updates;
using EventForge.DTOs.Chat;
using EventForge.DTOs.Documents;
using EventForge.DTOs.FiscalPrinting;
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
    Task StartAllConnectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops all SignalR connections gracefully.
    /// </summary>
    Task StopAllConnectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether the unified app connection (notifications + audit + alerts + config + updates) is active.
    /// </summary>
    bool IsAppConnected { get; }

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

    /// <summary>
    /// Gets whether the document collaboration connection is active.
    /// </summary>
    bool IsDocumentCollaborationConnected { get; }

    /// <summary>
    /// Gets whether the update-notifications connection is active.
    /// </summary>
    bool IsUpdateNotificationConnected { get; }

    /// <summary>
    /// Gets whether the fiscal-printer connection is active.
    /// </summary>
    bool IsFiscalPrinterConnected { get; }

    /// <summary>
    /// Gets whether the alerts connection is active.
    /// </summary>
    bool IsAlertsConnected { get; }

    /// <summary>
    /// Gets whether the configuration hub connection is active.
    /// </summary>
    bool IsConfigurationConnected { get; }

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
    /// Notification status updated (acknowledged/archived cross-session).
    /// </summary>
    event Action<object>? NotificationStatusUpdated;

    /// <summary>
    /// Tenant-wide notification received (broadcast to all tenant members).
    /// </summary>
    event Action<object>? TenantNotificationReceived;

    /// <summary>
    /// Bulk notifications received for the current user on connect.
    /// </summary>
    event Action<object>? ReceiveBulkNotifications;

    /// <summary>
    /// Server confirmed subscription to specific notification types.
    /// </summary>
    event Action<object>? SubscriptionConfirmed;

    /// <summary>
    /// Server confirmed unsubscription from specific notification types.
    /// </summary>
    event Action<object>? UnsubscriptionConfirmed;

    /// <summary>
    /// Notification silenced by the current user (Caller-only confirmation).
    /// </summary>
    event Action<Guid>? NotificationSilenced;

    /// <summary>
    /// Bulk notification action completed for the caller.
    /// </summary>
    event Action<object>? BulkActionCompleted;

    /// <summary>
    /// All sessions of this user received bulk notification status change.
    /// </summary>
    event Action<object>? NotificationsBulkUpdated;

    /// <summary>
    /// Caller's preferred notification locale was updated.
    /// </summary>
    event Action<string>? NotificationLocaleUpdated;

    /// <summary>
    /// Notification statistics received (Admin/SuperAdmin only).
    /// </summary>
    event Action<object>? NotificationStatsReceived;

    /// <summary>
    /// Chat created.
    /// </summary>
    event Action<ChatResponseDto>? ChatCreated;

    /// <summary>
    /// Chat metadata updated (last activity, etc.).
    /// </summary>
    event Action<object>? ChatUpdated;

    /// <summary>
    /// Chat members added/removed.
    /// </summary>
    event Action<object>? ChatMembersUpdated;

    /// <summary>
    /// Chat moderation action performed (SuperAdmin).
    /// </summary>
    event Action<object>? ChatModerated;

    /// <summary>
    /// Chat deleted (SuperAdmin moderation).
    /// </summary>
    event Action<object>? ChatDeleted;

    /// <summary>
    /// Chat statistics received (Admin/SuperAdmin).
    /// </summary>
    event Action<object>? ChatStatsReceived;

    /// <summary>
    /// User chat locale updated.
    /// </summary>
    event Action<object>? ChatLocaleUpdated;

    /// <summary>
    /// Current user was added to a chat by another member.
    /// </summary>
    event Action<object>? AddedToChat;

    /// <summary>
    /// Current user was removed from a chat.
    /// </summary>
    event Action<object>? RemovedFromChat;

    /// <summary>
    /// WhatsApp message delivery status updated.
    /// </summary>
    event Action<object>? WhatsAppMessageStatusUpdated;

    /// <summary>
    /// Message edited — the full updated <see cref="ChatMessageDto"/> is broadcast
    /// by the server so all fields (Content, Format, IsEdited, EditedAt, …) are available.
    /// </summary>
    event Action<ChatMessageDto>? MessageEdited;

    /// <summary>
    /// Message deleted.
    /// </summary>
    event Action<object>? MessageDeleted;

    /// <summary>
    /// Message read.
    /// </summary>
    event Action<object>? MessageRead;

    /// <summary>
    /// Fired when a message reaction is added or removed.
    /// </summary>
    event Action<object>? MessageReactionUpdated;

    /// <summary>
    /// User joined chat.
    /// </summary>
    event Action<object>? UserJoinedChat;

    /// <summary>
    /// User left chat.
    /// </summary>
    event Action<object>? UserLeftChat;

    /// <summary>
    /// User online status changed (userId, isOnline).
    /// </summary>
    event Action<Guid, bool>? UserOnlineStatusChanged;

    // ── WhatsApp real-time events (routed through the existing ChatHub) ──
    event Action<EventForge.DTOs.Chat.ChatMessageDto>? WhatsAppMessageReceived;
    event Action<EventForge.DTOs.Chat.ChatResponseDto>? WhatsAppConversazioneAggiornata;
    event Action<EventForge.DTOs.Chat.ChatResponseDto>? WhatsAppNumeroNonRiconosciuto;

    #endregion

    #region Chat Methods

    /// <summary>
    /// Sends a chat message with optimized delivery.
    /// </summary>
    Task SendChatMessageAsync(SendMessageDto messageDto, CancellationToken ct = default);

    /// <summary>
    /// Sends typing indicator with debouncing (300ms).
    /// </summary>
    Task SendTypingIndicatorAsync(Guid chatId, bool isTyping, CancellationToken ct = default);

    /// <summary>
    /// Joins a chat room.
    /// </summary>
    Task JoinChatAsync(Guid chatId, CancellationToken ct = default);

    /// <summary>
    /// Leaves a chat room.
    /// </summary>
    Task LeaveChatAsync(Guid chatId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new chat.
    /// </summary>
    Task CreateChatAsync(CreateChatDto createChatDto, CancellationToken ct = default);

    /// <summary>
    /// Edits a message.
    /// </summary>
    Task EditMessageAsync(EditMessageDto editDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteMessageAsync(Guid messageId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Marks message as read.
    /// </summary>
    Task MarkMessageAsReadAsync(Guid messageId, CancellationToken ct = default);

    #endregion

    #region Notification Methods

    /// <summary>
    /// Subscribes to specific notification types.
    /// </summary>
    Task SubscribeToNotificationTypesAsync(List<NotificationTypes> notificationTypes, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes from specific notification types.
    /// </summary>
    Task UnsubscribeFromNotificationTypesAsync(List<NotificationTypes> notificationTypes, CancellationToken ct = default);

    /// <summary>
    /// Acknowledges a notification.
    /// </summary>
    Task AcknowledgeNotificationAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Archives a notification.
    /// </summary>
    Task ArchiveNotificationAsync(Guid notificationId, CancellationToken ct = default);

    #endregion

    #region Audit Methods

    /// <summary>
    /// Starts audit connection and joins audit log group.
    /// </summary>
    Task StartAuditConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts notification connection.
    /// </summary>
    Task StartNotificationConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts chat connection.
    /// </summary>
    Task StartChatConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts the document collaboration connection.
    /// </summary>
    Task StartDocumentCollaborationConnectionAsync(CancellationToken ct = default);

    #endregion

    #region Update / Maintenance Events

    /// <summary>Fired when the Server starts a planned maintenance/update.</summary>
    event Action<MaintenanceStartedPayload>? ServerMaintenanceStarted;

    /// <summary>Fired when the Server returns online after maintenance.</summary>
    event Action<MaintenanceEndedPayload>? ServerMaintenanceEnded;

    /// <summary>Fired when a new Client version has been deployed to disk.</summary>
    event Action<ClientUpdateDeployedPayload>? ClientUpdateDeployed;

    /// <summary>Fired periodically during an active download/install with current progress.</summary>
    event Action<UpdateProgressPayload>? UpdateProgressReceived;

    /// <summary>Fired when the Server broadcasts the count of packages ready to deploy (SuperAdmin only).</summary>
    event Action<UpdatesAvailablePayload>? UpdatesAvailableReceived;

    /// <summary>Fired when LogCleanupService is about to start deleting log entries (SuperAdmin only).</summary>
    event Action<LogCleanupStartedPayload>? LogCleanupStarted;

    /// <summary>
    /// Fired when LogCleanupService transitions to a new phase (SuperAdmin only).
    /// Phase values: "Backup" → "Deleting".
    /// </summary>
    event Action<LogCleanupPhaseChangedPayload>? LogCleanupPhaseChanged;

    /// <summary>Fired when LogCleanupService finishes (or fails) its cleanup run (SuperAdmin only).</summary>
    event Action<LogCleanupCompletedPayload>? LogCleanupCompleted;

    #endregion

    #region Document Collaboration Events

    /// <summary>
    /// Fired when a document is locked by another user.
    /// </summary>
    event Action<object>? DocumentLocked;

    /// <summary>
    /// Fired when a document lock is released.
    /// </summary>
    event Action<object>? DocumentUnlocked;

    /// <summary>
    /// Fired when a user joins a document collaboration session.
    /// </summary>
    event Action<object>? UserJoinedDocument;

    /// <summary>
    /// Fired when a user leaves a document collaboration session.
    /// </summary>
    event Action<object>? UserLeftDocument;

    /// <summary>
    /// Fired when a typing indicator is received for a document.
    /// </summary>
    event Action<object>? DocumentTypingIndicator;

    /// <summary>
    /// Fired when a new comment is created on a document.
    /// </summary>
    event Action<DocumentCommentDto>? CommentCreated;

    /// <summary>
    /// Fired when a comment is updated on a document.
    /// </summary>
    event Action<DocumentCommentDto>? CommentUpdated;

    /// <summary>
    /// Fired when a comment is deleted on a document.
    /// </summary>
    event Action<object>? CommentDeleted;

    /// <summary>
    /// Fired when a comment is resolved on a document.
    /// </summary>
    event Action<object>? CommentResolved;

    /// <summary>
    /// Fired when a previously resolved comment is reopened.
    /// </summary>
    event Action<object>? CommentReopened;

    /// <summary>
    /// Fired when a task is assigned to the current user on a document.
    /// </summary>
    event Action<object>? TaskAssigned;

    /// <summary>
    /// Fired when the current user is mentioned in a document comment.
    /// </summary>
    event Action<object>? UserMentioned;

    #endregion

    #region Document Collaboration Methods

    /// <summary>
    /// Joins a document collaboration room to receive real-time updates.
    /// </summary>
    Task JoinDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Leaves a document collaboration room.
    /// </summary>
    Task LeaveDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Requests an exclusive edit lock for a document.
    /// </summary>
    Task<bool> RequestDocumentEditLockAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Releases the edit lock for a document.
    /// </summary>
    Task ReleaseDocumentEditLockAsync(Guid documentId, CancellationToken ct = default);

    #endregion

    #region Fiscal Printer Events

    /// <summary>
    /// Fired when a fiscal printer's status is updated by the monitoring service.
    /// Arguments: (printerId, status).
    /// </summary>
    event Action<Guid, FiscalPrinterStatus>? PrinterStatusUpdated;

    /// <summary>
    /// Fired when a fiscal printer requires a daily closure.
    /// Arguments: (printerId, printerName).
    /// </summary>
    event Action<Guid, string>? PrinterClosureRequired;

    /// <summary>
    /// Fired when a fiscal printer has a critical missing closure (fiscal memory full).
    /// Arguments: printerId.
    /// </summary>
    event Action<Guid>? PrinterCriticalClosureMissing;

    #endregion

    #region Fiscal Printer Methods

    /// <summary>
    /// Subscribes to real-time status updates for the given printer.
    /// Reference-counted: the hub group is joined on the first subscriber and left only when all unsubscribe.
    /// </summary>
    Task SubscribeToPrinterAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>
    /// Decrements the subscriber count for the printer and leaves the hub group when it reaches zero.
    /// </summary>
    Task UnsubscribeFromPrinterAsync(Guid printerId, CancellationToken ct = default);

    #endregion

    #region Alert Events

    /// <summary>
    /// Fired when a new supplier price alert is broadcast to the tenant.
    /// </summary>
    event Action<object>? PriceAlertReceived;

    #endregion

    #region Configuration Events

    /// <summary>
    /// Fired when a configuration key is changed by a SuperAdmin.
    /// </summary>
    event Action<object>? ConfigurationChanged;

    /// <summary>
    /// Fired when a server restart is required.
    /// </summary>
    event Action<object>? RestartRequired;

    /// <summary>
    /// Fired when a system operation completes (backup, migration, etc.).
    /// </summary>
    event Action<object>? SystemOperationReceived;

    #endregion

    #region Admin / SuperAdmin Events

    /// <summary>Fired when a database backup status changes (SuperAdmin only).</summary>
    event Action<object>? BackupStatusChanged;

    /// <summary>Fired when a user's roles are updated (SuperAdmin only).</summary>
    event Action<object>? UserRolesChanged;

    /// <summary>Fired when a user password is reset by an admin (SuperAdmin only).</summary>
    event Action<object>? PasswordReset;

    /// <summary>Fired when a forced password-change flag is set on a user (SuperAdmin only).</summary>
    event Action<object>? PasswordChangeForced;

    /// <summary>Fired when a bulk user action completes (SuperAdmin only).</summary>
    event Action<object>? BulkUserActionCompleted;

    /// <summary>Fired when a user's profile is updated (SuperAdmin only).</summary>
    event Action<object>? UserUpdated;

    /// <summary>Fired when a user account is deleted (SuperAdmin only).</summary>
    event Action<object>? UserDeleted;

    /// <summary>Fired when role permissions are updated (broadcast to all clients).</summary>
    event Action<object>? RolePermissionsUpdated;

    /// <summary>Fired when a tenant switch or impersonation operation occurs (SuperAdmin only).</summary>
    event Action<object>? TenantSwitched;

    /// <summary>Fired when impersonation of a user starts (SuperAdmin only).</summary>
    event Action<object>? ImpersonationStarted;

    /// <summary>Fired when impersonation ends (SuperAdmin only).</summary>
    event Action<object>? ImpersonationEnded;

    #endregion
}