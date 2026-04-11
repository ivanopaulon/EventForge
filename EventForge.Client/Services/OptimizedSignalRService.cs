using EventForge.Client.Services.Updates;
using EventForge.DTOs.Chat;
using EventForge.DTOs.Documents;
using EventForge.DTOs.FiscalPrinting;
using EventForge.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace EventForge.Client.Services;

/// <summary>
/// Optimized SignalR service with connection pooling, event batching, and performance monitoring.
/// Reduces latency and improves scalability for multiple users and high load scenarios.
/// </summary>
public class OptimizedSignalRService : IRealtimeService, IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<OptimizedSignalRService> _logger;
    private readonly IPerformanceOptimizationService _performanceService;

    // Connection management
    private readonly ConcurrentDictionary<string, HubConnection> _connections;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLocks;
    private readonly ConcurrentDictionary<string, bool> _retryInProgress;
    private readonly Timer _connectionHealthTimer;
    private readonly Timer _eventBatchTimer;

    // Event batching for performance
    private readonly ConcurrentQueue<BatchedEvent> _eventQueue;
    private readonly ConcurrentDictionary<string, DateTime> _lastEventTime;

    // Connection retry configuration
    private readonly RetryConfiguration _retryConfig;

    private class RetryConfiguration
    {
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 5;
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    #region Events - Batched (optimized with batching)
    public event Action<List<object>>? BatchedAuditLogUpdates;
    public event Action<List<NotificationResponseDto>>? BatchedNotifications;
    public event Action<List<ChatMessageDto>>? BatchedChatMessages;
    #endregion

    #region Events - Individual (backward compatibility)
    public event Action<NotificationResponseDto>? NotificationReceived;
    public event Action<ChatMessageDto>? MessageReceived;
    public event Action<TypingIndicatorDto>? TypingIndicator;
    public event Action<object>? AuditLogUpdated;
    public event Action<Guid>? NotificationAcknowledged;
    public event Action<Guid>? NotificationArchived;
    public event Action<ChatResponseDto>? ChatCreated;
    public event Action<EditMessageDto>? MessageEdited;
    public event Action<object>? MessageDeleted;
    public event Action<object>? MessageRead;
    public event Action<object>? UserJoinedChat;
    public event Action<object>? UserLeftChat;
    public event Action<Guid, bool>? UserOnlineStatusChanged;
    public event Action<EventForge.DTOs.Chat.ChatMessageDto>? WhatsAppMessageReceived;
    public event Action<EventForge.DTOs.Chat.ChatResponseDto>? WhatsAppConversazioneAggiornata;
    public event Action<EventForge.DTOs.Chat.ChatResponseDto>? WhatsAppNumeroNonRiconosciuto;
    #endregion

    #region Events - Document Collaboration
    public event Action<object>? DocumentLocked;
    public event Action<object>? DocumentUnlocked;
    public event Action<object>? UserJoinedDocument;
    public event Action<object>? UserLeftDocument;
    public event Action<object>? DocumentTypingIndicator;
    public event Action<DocumentCommentDto>? CommentCreated;
    public event Action<DocumentCommentDto>? CommentUpdated;
    #endregion

    #region Events - Update / Maintenance
    public event Action<MaintenanceStartedPayload>? ServerMaintenanceStarted;
    public event Action<MaintenanceEndedPayload>? ServerMaintenanceEnded;
    public event Action<ClientUpdateDeployedPayload>? ClientUpdateDeployed;
    public event Action<UpdateProgressPayload>? UpdateProgressReceived;
    public event Action<UpdatesAvailablePayload>? UpdatesAvailableReceived;
    public event Action<LogCleanupStartedPayload>? LogCleanupStarted;
    public event Action<LogCleanupPhaseChangedPayload>? LogCleanupPhaseChanged;
    public event Action<LogCleanupCompletedPayload>? LogCleanupCompleted;
    #endregion

    #region Events - Fiscal Printer
    public event Action<Guid, FiscalPrinterStatus>? PrinterStatusUpdated;
    public event Action<Guid, string>? PrinterClosureRequired;
    public event Action<Guid>? PrinterCriticalClosureMissing;
    #endregion

    #region Events - Alerts
    public event Action<object>? PriceAlertReceived;
    #endregion

    #region Events - Configuration
    public event Action<object>? ConfigurationChanged;
    public event Action<object>? RestartRequired;
    public event Action<object>? SystemOperationReceived;
    #endregion

    // Reference counting for fiscal printer group subscriptions (printerId → subscriber count)
    private readonly ConcurrentDictionary<Guid, int> _printerSubscriptions = new();

    private class BatchedEvent
    {
        public string Type { get; set; } = string.Empty;
        public object Data { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

    public OptimizedSignalRService(
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        ILogger<OptimizedSignalRService> logger,
        IPerformanceOptimizationService performanceService)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;
        _performanceService = performanceService;

        _connections = new ConcurrentDictionary<string, HubConnection>();
        _connectionLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        _retryInProgress = new ConcurrentDictionary<string, bool>();
        _eventQueue = new ConcurrentQueue<BatchedEvent>();
        _lastEventTime = new ConcurrentDictionary<string, DateTime>();

        _retryConfig = new RetryConfiguration();

        // Health check timer - every 30 seconds
        _connectionHealthTimer = new Timer(CheckConnectionHealthAsync, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        // Event batch processing timer - every 100ms for responsiveness
        _eventBatchTimer = new Timer(ProcessEventBatchAsync, null,
            TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

        _logger.LogInformation("Optimized SignalR service initialized");
    }

    #region Connection Management

    /// <summary>
    /// Starts all optimized SignalR connections with connection pooling.
    /// </summary>
    public async Task StartAllConnectionsAsync(CancellationToken ct = default)
    {
        var connectionTasks = new List<Task>
        {
            // AppHub consolidates: audit, notifications, alerts, configuration, update-notifications
            StartConnectionAsync("app", "/hubs/app"),
            StartConnectionAsync("chat", "/hubs/chat"),
            StartConnectionAsync("document-collaboration", "/hubs/document-collaboration"),
            StartConnectionAsync("fiscal-printer", "/hubs/fiscal-printer")
        };

        await Task.WhenAll(connectionTasks);
        _logger.LogInformation("All SignalR connections started");
    }

    /// <summary>
    /// Starts a specific optimized connection with retry logic.
    /// </summary>
    private async Task StartConnectionAsync(string connectionKey, string hubPath)
    {
        var lockSemaphore = _connectionLocks.GetOrAdd(connectionKey, _ => new SemaphoreSlim(1, 1));

        await lockSemaphore.WaitAsync();
        try
        {
            if (_connections.TryGetValue(connectionKey, out var existingConnection) &&
                existingConnection.State == HubConnectionState.Connected)
            {
                return; // Already connected
            }

            var connection = await CreateOptimizedConnectionAsync(hubPath);
            if (connection == null) return;

            // Register optimized event handlers
            RegisterOptimizedEventHandlers(connection, connectionKey);

            // Setup connection event handlers
            connection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("SignalR {ConnectionKey} reconnected: {ConnectionId}",
                    connectionKey, connectionId);
                await OnConnectionReconnectedAsync(connectionKey);
            };

            connection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "SignalR {ConnectionKey} connection closed", connectionKey);
                await HandleConnectionClosedAsync(connectionKey, error);
            };

            await connection.StartAsync();
            _connections[connectionKey] = connection;

            // Post-connection setup
            await OnConnectionEstablishedAsync(connectionKey);

            _logger.LogInformation("SignalR {ConnectionKey} connection established", connectionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR {ConnectionKey} connection", connectionKey);
            // Retry is handled via the connection.Closed event or the calling ScheduleRetryAsync loop.
            // Do NOT spawn a second ScheduleRetryAsync here to avoid duplicate retry loops.
        }
        finally
        {
            _ = lockSemaphore.Release();
        }
    }

    /// <summary>
    /// Creates an optimized SignalR connection with performance settings.
    /// </summary>
    private async Task<HubConnection?> CreateOptimizedConnectionAsync(string hubPath)
    {
        var token = await _authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Cannot create SignalR connection: no access token available");
            return null;
        }

        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        var hubUrl = new Uri(httpClient.BaseAddress!, hubPath).ToString();

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Use a dynamic provider so every reconnect attempt fetches the latest valid token
                // instead of reusing the token captured at connection-creation time.
                options.AccessTokenProvider = () => _authService.GetAccessTokenAsync();
                // Optimize for mobile and high-load scenarios
                options.SkipNegotiation = true;
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                // Note: WebSocketConfiguration (including KeepAliveInterval) is not supported in Blazor WebAssembly 
                // as it runs in browser environment. The browser handles WebSocket keep-alive automatically.
            })
            .WithAutomaticReconnect(new OptimizedRetryPolicy(new OptimizedRetryPolicy.RetryConfiguration()))
            .ConfigureLogging(logging =>
            {
                // Reduce logging overhead in production
                _ = logging.SetMinimumLevel(LogLevel.Warning);
            })
            // Note: MessagePack protocol can be added for better performance in production
            // .AddMessagePackProtocol()
            .Build();

        return connection;
    }

    /// <summary>
    /// Registers optimized event handlers with batching capabilities.
    /// </summary>
    private void RegisterOptimizedEventHandlers(HubConnection connection, string connectionKey)
    {
        switch (connectionKey)
        {
            case "app":
                RegisterAuditEventHandlers(connection);
                RegisterNotificationEventHandlers(connection);
                RegisterUpdateNotificationEventHandlers(connection);
                RegisterAlertEventHandlers(connection);
                RegisterConfigurationEventHandlers(connection);
                break;
            case "chat":
                RegisterChatEventHandlers(connection);
                break;
            case "document-collaboration":
                RegisterDocumentCollaborationEventHandlers(connection);
                break;
            case "fiscal-printer":
                RegisterFiscalPrinterEventHandlers(connection);
                break;
        }
    }

    private void RegisterAuditEventHandlers(HubConnection connection)
    {
        _ = connection.On<object>("AuditLogUpdated", data =>
        {
            EnqueueEvent("audit_log", data);
        });

        _ = connection.On<object>("UserStatusChanged", data =>
        {
            EnqueueEvent("user_status", data);
        });
    }

    private void RegisterNotificationEventHandlers(HubConnection connection)
    {
        _ = connection.On<NotificationResponseDto>("NotificationReceived", notification =>
        {
            EnqueueEvent("notification", notification);
        });

        _ = connection.On<NotificationResponseDto>("SystemNotificationReceived", notification =>
        {
            EnqueueEvent("system_notification", notification);
        });

        _ = connection.On<Guid>("NotificationAcknowledged", notificationId =>
        {
            NotificationAcknowledged?.Invoke(notificationId);
        });

        _ = connection.On<Guid>("NotificationArchived", notificationId =>
        {
            NotificationArchived?.Invoke(notificationId);
        });
    }

    private void RegisterChatEventHandlers(HubConnection connection)
    {
        _ = connection.On<ChatResponseDto>("ChatCreated", chat =>
        {
            ChatCreated?.Invoke(chat);
        });

        _ = connection.On<ChatMessageDto>("MessageReceived", message =>
        {
            EnqueueEvent("chat_message", message);
        });

        _ = connection.On<EditMessageDto>("MessageEdited", editDto =>
        {
            MessageEdited?.Invoke(editDto);
        });

        _ = connection.On<object>("MessageDeleted", data =>
        {
            MessageDeleted?.Invoke(data);
        });

        _ = connection.On<object>("MessageRead", data =>
        {
            MessageRead?.Invoke(data);
        });

        _ = connection.On<object>("UserJoinedChat", data =>
        {
            UserJoinedChat?.Invoke(data);
        });

        _ = connection.On<object>("UserLeftChat", data =>
        {
            UserLeftChat?.Invoke(data);
        });

        _ = connection.On<Guid, bool>("UserOnlineStatusChanged", (userId, isOnline) =>
        {
            UserOnlineStatusChanged?.Invoke(userId, isOnline);
        });

        // Typing indicators are not batched for responsiveness
        _ = connection.On<TypingIndicatorDto>("TypingIndicator", indicator =>
        {
            TypingIndicator?.Invoke(indicator);
        });

        // WhatsApp real-time events
        _ = connection.On<EventForge.DTOs.Chat.ChatMessageDto>("NuovoMessaggioWhatsApp", msg =>
        {
            WhatsAppMessageReceived?.Invoke(msg);
        });
        _ = connection.On<EventForge.DTOs.Chat.ChatResponseDto>("ConversazioneAggiornata", conv =>
        {
            WhatsAppConversazioneAggiornata?.Invoke(conv);
        });
        _ = connection.On<EventForge.DTOs.Chat.ChatResponseDto>("NumeroNonRiconosciuto", conv =>
        {
            WhatsAppNumeroNonRiconosciuto?.Invoke(conv);
        });
    }

    private void RegisterDocumentCollaborationEventHandlers(HubConnection connection)
    {
        _ = connection.On<object>("DocumentLocked", data =>
        {
            _logger.LogInformation("Document locked");
            DocumentLocked?.Invoke(data);
        });

        _ = connection.On<object>("DocumentUnlocked", data =>
        {
            _logger.LogInformation("Document unlocked");
            DocumentUnlocked?.Invoke(data);
        });

        _ = connection.On<object>("UserJoinedDocument", data =>
        {
            UserJoinedDocument?.Invoke(data);
        });

        _ = connection.On<object>("UserLeftDocument", data =>
        {
            UserLeftDocument?.Invoke(data);
        });

        _ = connection.On<object>("TypingIndicator", data =>
        {
            DocumentTypingIndicator?.Invoke(data);
        });

        _ = connection.On<DocumentCommentDto>("CommentCreated", comment =>
        {
            _logger.LogInformation("Comment created: {CommentId}", comment.Id);
            CommentCreated?.Invoke(comment);
        });

        _ = connection.On<DocumentCommentDto>("CommentUpdated", comment =>
        {
            _logger.LogInformation("Comment updated: {CommentId}", comment.Id);
            CommentUpdated?.Invoke(comment);
        });
    }

    private void RegisterUpdateNotificationEventHandlers(HubConnection connection)
    {
        _ = connection.On<System.Text.Json.JsonElement>("MaintenanceStarted", data =>
        {
            try
            {
                var payload = new MaintenanceStartedPayload(
                    data.TryGetProperty("component", out var c) ? c.GetString() : null,
                    data.TryGetProperty("version", out var v) ? v.GetString() : null,
                    data.TryGetProperty("startedAt", out var t) ? t.GetDateTime() : DateTime.UtcNow);
                ServerMaintenanceStarted?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse MaintenanceStarted payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("MaintenanceEnded", data =>
        {
            try
            {
                var payload = new MaintenanceEndedPayload(
                    data.TryGetProperty("component", out var c) ? c.GetString() : null,
                    data.TryGetProperty("version", out var v) ? v.GetString() : null,
                    data.TryGetProperty("endedAt", out var t) ? t.GetDateTime() : DateTime.UtcNow);
                ServerMaintenanceEnded?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse MaintenanceEnded payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("ClientUpdateDeployed", data =>
        {
            try
            {
                var payload = new ClientUpdateDeployedPayload(
                    data.TryGetProperty("component", out var c) ? c.GetString() : null,
                    data.TryGetProperty("version", out var v) ? v.GetString() : null,
                    data.TryGetProperty("deployedAt", out var t) ? t.GetDateTime() : DateTime.UtcNow);
                ClientUpdateDeployed?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse ClientUpdateDeployed payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("UpdateProgress", data =>
        {
            try
            {
                var payload = new UpdateProgressPayload(
                    data.TryGetProperty("component", out var c) ? c.GetString() : null,
                    data.TryGetProperty("version", out var v) ? v.GetString() : null,
                    data.TryGetProperty("phase", out var ph) ? ph.GetString() : null,
                    data.TryGetProperty("percentComplete", out var pct) && pct.ValueKind == System.Text.Json.JsonValueKind.Number ? pct.GetInt32() : null,
                    data.TryGetProperty("formattedDownloaded", out var fd) ? fd.GetString() : null,
                    data.TryGetProperty("formattedTotal", out var ft) ? ft.GetString() : null,
                    data.TryGetProperty("formattedSpeed", out var fs) ? fs.GetString() : null,
                    data.TryGetProperty("eta", out var eta) ? eta.GetString() : null,
                    data.TryGetProperty("sentAt", out var sa) ? sa.GetDateTime() : DateTime.UtcNow,
                    IsManualInstall: data.TryGetProperty("isManualInstall", out var im) && im.ValueKind == System.Text.Json.JsonValueKind.True ? true
                                   : im.ValueKind == System.Text.Json.JsonValueKind.False ? false : null,
                    PackageId: data.TryGetProperty("packageId", out var pid) && pid.ValueKind == System.Text.Json.JsonValueKind.String && Guid.TryParse(pid.GetString(), out var g) ? g : null,
                    NextWindowAt: data.TryGetProperty("nextWindowAt", out var nw) ? nw.GetString() : null,
                    Detail: data.TryGetProperty("detail", out var det) ? det.GetString() : null);
                UpdateProgressReceived?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse UpdateProgress payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("UpdatesAvailable", data =>
        {
            try
            {
                var count = data.TryGetProperty("count", out var cnt) && cnt.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? cnt.GetInt32() : 0;
                UpdatesAvailableReceived?.Invoke(new UpdatesAvailablePayload(count));
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse UpdatesAvailable payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("LogCleanupStarted", data =>
        {
            try
            {
                var payload = new LogCleanupStartedPayload(
                    data.TryGetProperty("retentionDays", out var rd) && rd.ValueKind == System.Text.Json.JsonValueKind.Number ? rd.GetInt32() : 30,
                    data.TryGetProperty("cutoffDate", out var cd) ? cd.GetDateTime() : DateTime.UtcNow,
                    data.TryGetProperty("backupEnabled", out var be) && be.ValueKind == System.Text.Json.JsonValueKind.True,
                    data.TryGetProperty("startedAt", out var sa) ? sa.GetDateTime() : DateTime.UtcNow);
                LogCleanupStarted?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse LogCleanupStarted payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("LogCleanupPhaseChanged", data =>
        {
            try
            {
                bool? backupSucceeded = null;
                if (data.TryGetProperty("backupSucceeded", out var bs))
                {
                    if (bs.ValueKind == System.Text.Json.JsonValueKind.True)  backupSucceeded = true;
                    if (bs.ValueKind == System.Text.Json.JsonValueKind.False) backupSucceeded = false;
                }
                var payload = new LogCleanupPhaseChangedPayload(
                    Phase:          data.TryGetProperty("phase", out var ph) ? ph.GetString() ?? string.Empty : string.Empty,
                    Detail:         data.TryGetProperty("detail", out var det) ? det.GetString() : null,
                    BackupSucceeded: backupSucceeded,
                    ChangedAt:      data.TryGetProperty("changedAt", out var ca) ? ca.GetDateTime() : DateTime.UtcNow);
                LogCleanupPhaseChanged?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse LogCleanupPhaseChanged payload"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("LogCleanupCompleted", data =>
        {
            try
            {
                var payload = new LogCleanupCompletedPayload(
                    Success:        data.TryGetProperty("success", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.True,
                    TotalDeleted:   data.TryGetProperty("totalDeleted", out var td) && td.ValueKind == System.Text.Json.JsonValueKind.Number ? td.GetInt32() : 0,
                    LoginAudits:    data.TryGetProperty("loginAudits", out var la) && la.ValueKind == System.Text.Json.JsonValueKind.Number ? la.GetInt32() : 0,
                    AuditTrails:    data.TryGetProperty("auditTrails", out var at) && at.ValueKind == System.Text.Json.JsonValueKind.Number ? at.GetInt32() : 0,
                    OperationLogs:  data.TryGetProperty("operationLogs", out var ol) && ol.ValueKind == System.Text.Json.JsonValueKind.Number ? ol.GetInt32() : 0,
                    PerformanceLogs:data.TryGetProperty("performanceLogs", out var pl) && pl.ValueKind == System.Text.Json.JsonValueKind.Number ? pl.GetInt32() : 0,
                    SerilogLogs:    data.TryGetProperty("serilogLogs", out var sl) && sl.ValueKind == System.Text.Json.JsonValueKind.Number ? sl.GetInt32() : 0,
                    RetentionDays:  data.TryGetProperty("retentionDays", out var rd) && rd.ValueKind == System.Text.Json.JsonValueKind.Number ? rd.GetInt32() : 0,
                    BackupFile:     data.TryGetProperty("backupFile", out var bf) ? bf.GetString() ?? "none" : "none",
                    ElapsedSeconds: data.TryGetProperty("elapsedSeconds", out var es) && es.ValueKind == System.Text.Json.JsonValueKind.Number ? es.GetDouble() : 0,
                    CompletedAt:    data.TryGetProperty("completedAt", out var ca) ? ca.GetDateTime() : DateTime.UtcNow,
                    Error:          data.TryGetProperty("error", out var err) ? err.GetString() : null);
                LogCleanupCompleted?.Invoke(payload);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse LogCleanupCompleted payload"); }
        });
    }

    private void RegisterFiscalPrinterEventHandlers(HubConnection connection)
    {
        _ = connection.On<Guid, FiscalPrinterStatus>("PrinterStatusUpdated", (printerId, status) =>
        {
            try { PrinterStatusUpdated?.Invoke(printerId, status); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process PrinterStatusUpdated"); }
        });

        _ = connection.On<Guid, string>("ClosureRequired", (printerId, printerName) =>
        {
            try { PrinterClosureRequired?.Invoke(printerId, printerName); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process ClosureRequired"); }
        });

        _ = connection.On<Guid>("CriticalClosureMissing", printerId =>
        {
            try { PrinterCriticalClosureMissing?.Invoke(printerId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process CriticalClosureMissing"); }
        });
    }

    private void RegisterAlertEventHandlers(HubConnection connection)
    {
        _ = connection.On<System.Text.Json.JsonElement>("NewAlert", data =>
        {
            try { PriceAlertReceived?.Invoke(data); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process NewAlert"); }
        });
    }

    private void RegisterConfigurationEventHandlers(HubConnection connection)
    {
        _ = connection.On<System.Text.Json.JsonElement>("ConfigurationChanged", data =>
        {
            try { ConfigurationChanged?.Invoke(data); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process ConfigurationChanged"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("RestartRequired", data =>
        {
            try { RestartRequired?.Invoke(data); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process RestartRequired"); }
        });

        _ = connection.On<System.Text.Json.JsonElement>("SystemOperation", data =>
        {
            try { SystemOperationReceived?.Invoke(data); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to process SystemOperation"); }
        });
    }

    #endregion

    #region Event Batching and Processing

    /// <summary>
    /// Enqueues an event for batched processing to reduce UI update frequency.
    /// </summary>
    private void EnqueueEvent(string eventType, object data)
    {
        _eventQueue.Enqueue(new BatchedEvent
        {
            Type = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow
        });

        _lastEventTime[eventType] = DateTime.UtcNow;
    }

    /// <summary>
    /// Processes batched events to reduce rendering overhead.
    /// </summary>
    private async void ProcessEventBatchAsync(object? state)
    {
        try
        {
            var auditEvents = new List<object>();
            var notifications = new List<NotificationResponseDto>();
            var chatMessages = new List<ChatMessageDto>();

            // Process up to 50 events per batch to avoid overwhelming the UI
            var processedCount = 0;
            while (_eventQueue.TryDequeue(out var batchedEvent) && processedCount < 50)
            {
                switch (batchedEvent.Type)
                {
                    case "audit_log":
                    case "user_status":
                        auditEvents.Add(batchedEvent.Data);
                        // Fire individual event for backward compatibility
                        AuditLogUpdated?.Invoke(batchedEvent.Data);
                        break;
                    case "notification":
                    case "system_notification":
                        if (batchedEvent.Data is NotificationResponseDto notification)
                        {
                            notifications.Add(notification);
                            // Fire individual event for backward compatibility
                            NotificationReceived?.Invoke(notification);
                        }
                        break;
                    case "chat_message":
                        if (batchedEvent.Data is ChatMessageDto message)
                        {
                            chatMessages.Add(message);
                            // Fire individual event for backward compatibility
                            MessageReceived?.Invoke(message);
                        }
                        break;
                }

                processedCount++;
            }

            // Dispatch batched events
            if (auditEvents.Count > 0)
                BatchedAuditLogUpdates?.Invoke(auditEvents);

            if (notifications.Count > 0)
                BatchedNotifications?.Invoke(notifications);

            if (chatMessages.Count > 0)
                BatchedChatMessages?.Invoke(chatMessages);

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing event batch");
        }
    }

    #endregion

    #region Connection Health and Recovery

    /// <summary>
    /// Periodically checks connection health and attempts recovery.
    /// </summary>
    private async void CheckConnectionHealthAsync(object? state)
    {
        try
        {
            // Skip recovery entirely if the user session has expired
            if (!await _authService.IsAuthenticatedAsync())
            {
                _logger.LogDebug("Skipping connection health check: user is not authenticated");
                return;
            }

            var healthCheckTasks = _connections.Select(async kvp =>
            {
                var (key, connection) = kvp;
                if (connection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogWarning("Connection {Key} is disconnected, attempting recovery", key);
                    await StartConnectionAsync(key, GetHubPath(key));
                }
            });

            await Task.WhenAll(healthCheckTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in SignalR connection health check");
        }
    }

    private string GetHubPath(string connectionKey) => connectionKey switch
    {
        "app" => "/hubs/app",
        "chat" => "/hubs/chat",
        "document-collaboration" => "/hubs/document-collaboration",
        "fiscal-printer" => "/hubs/fiscal-printer",
        _ => throw new ArgumentException($"Unknown connection key: {connectionKey}")
    };

    /// <summary>
    /// Handles connection closed events with optimized recovery.
    /// </summary>
    private async Task HandleConnectionClosedAsync(string connectionKey, Exception? error)
    {
        _ = _connections.TryRemove(connectionKey, out _);

        // Clear relevant cache on disconnection
        switch (connectionKey)
        {
            case "chat":
                _performanceService.InvalidateCachePattern(CacheKeys.CHAT_MESSAGES_PREFIX);
                break;
            case "app":
                _performanceService.InvalidateCache(CacheKeys.NOTIFICATION_LIST);
                break;
        }

        // Schedule retry with exponential backoff
        await ScheduleRetryAsync(connectionKey, GetHubPath(connectionKey));
    }

    /// <summary>
    /// Schedules connection retry with exponential backoff.
    /// Only one retry loop per connection key is allowed to run at a time.
    /// </summary>
    private async Task ScheduleRetryAsync(string connectionKey, string hubPath)
    {
        // Prevent duplicate concurrent retry loops for the same connection
        if (!_retryInProgress.TryAdd(connectionKey, true))
        {
            _logger.LogDebug("Retry already in progress for {ConnectionKey}, skipping duplicate", connectionKey);
            return;
        }

        try
        {
            var retryCount = 0;
            var delay = _retryConfig.InitialDelay;

            while (retryCount < _retryConfig.MaxRetries)
            {
                _logger.LogInformation("Scheduling retry for {ConnectionKey} with backoff delay: {Delay}s",
                    connectionKey, delay.TotalSeconds);

                await Task.Delay(delay);

                // Stop retrying if the user session has expired — no point reconnecting with an invalid token
                if (!await _authService.IsAuthenticatedAsync())
                {
                    _logger.LogWarning("Stopping reconnect retries for {ConnectionKey}: user session has expired",
                        connectionKey);
                    return;
                }

                try
                {
                    await StartConnectionAsync(connectionKey, hubPath);

                    // Verify the connection actually succeeded before declaring victory
                    if (_connections.TryGetValue(connectionKey, out var conn) &&
                        conn.State == HubConnectionState.Connected)
                    {
                        _logger.LogInformation("Successfully reconnected {ConnectionKey} after {RetryCount} retries",
                            connectionKey, retryCount);
                        return; // Success
                    }

                    // StartConnectionAsync swallowed an exception; treat as failure and retry
                    retryCount++;
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryConfig.BackoffMultiplier,
                        _retryConfig.MaxDelay.TotalMilliseconds));
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var previousDelay = delay;
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryConfig.BackoffMultiplier,
                        _retryConfig.MaxDelay.TotalMilliseconds));

                    _logger.LogWarning(ex, "Retry {Count}/{Max} failed for {ConnectionKey}. Previous delay: {PreviousDelay}s, Next delay: {NextDelay}s",
                        retryCount, _retryConfig.MaxRetries, connectionKey, previousDelay.TotalSeconds, delay.TotalSeconds);
                }
            }

            _logger.LogError("Failed to reconnect {ConnectionKey} after {MaxRetries} attempts with delays: 2s, 4s, 8s, 16s, 30s",
                connectionKey, _retryConfig.MaxRetries);
        }
        finally
        {
            _retryInProgress.TryRemove(connectionKey, out _);
        }
    }

    #endregion

    #region Connection-specific Methods

    private async Task OnConnectionEstablishedAsync(string connectionKey)
    {
        switch (connectionKey)
        {
            case "app":
                await JoinAuditGroupAsync();
                // Preload notification preferences
                _performanceService.PreloadData(CacheKeys.NOTIFICATION_LIST,
                    async () => await GetNotificationPreferencesAsync());
                break;
            case "chat":
                // Preload active chats
                _performanceService.PreloadData(CacheKeys.CHAT_LIST,
                    async () => await GetActiveChatListAsync());
                break;
        }
    }

    private async Task OnConnectionReconnectedAsync(string connectionKey)
    {
        // Invalidate relevant cache and reload data
        await OnConnectionEstablishedAsync(connectionKey);
    }

    private async Task JoinAuditGroupAsync()
    {
        if (_connections.TryGetValue("app", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("JoinAuditLogGroup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join audit log group");
            }
        }
    }

    // Placeholder methods for preloading data
    private async Task<object> GetNotificationPreferencesAsync()
    {
        // Implementation would fetch from API
        await Task.Delay(1);
        return new object();
    }

    private async Task<object> GetActiveChatListAsync()
    {
        // Implementation would fetch from API
        await Task.Delay(1);
        return new object();
    }

    #endregion

    #region Public API Methods (optimized versions)

    /// <summary>
    /// Sends a chat message with optimized delivery.
    /// </summary>
    public async Task SendChatMessageAsync(SendMessageDto messageDto, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("SendMessage", messageDto);

                // Optimistically update local cache
                var cacheKey = CacheKeys.ChatMessages(messageDto.ChatId);
                _performanceService.InvalidateCache(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send chat message");
                throw;
            }
        }
    }

    /// <summary>
    /// Sends typing indicator with debouncing to reduce network traffic.
    /// </summary>
    public async Task SendTypingIndicatorAsync(Guid chatId, bool isTyping, CancellationToken ct = default)
    {
        try
        {
            _ = await _performanceService.DebounceAsync(
                $"{DebounceKeys.TYPING_INDICATOR}_{chatId}",
                async () =>
                {
                    if (_connections.TryGetValue("chat", out var connection) &&
                        connection.State == HubConnectionState.Connected)
                    {
                        await connection.InvokeAsync("SendTypingIndicator", chatId, isTyping);
                    }
                    return true;
                },
                TimeSpan.FromMilliseconds(300) // 300ms debounce for typing
            );
        }
        catch (OperationCanceledException)
        {
            // Debounced operation was cancelled, this is expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing indicator");
        }
    }

    /// <summary>
    /// Stops all SignalR connections gracefully.
    /// </summary>
    public async Task StopAllConnectionsAsync(CancellationToken ct = default)
    {
        var stopTasks = new List<Task>();

        foreach (var kvp in _connections)
        {
            var (key, connection) = kvp;
            if (connection.State != HubConnectionState.Disconnected)
            {
                stopTasks.Add(connection.StopAsync());
            }
        }

        await Task.WhenAll(stopTasks);
        _connections.Clear();
        _logger.LogInformation("All SignalR connections stopped");
    }

    /// <summary>
    /// Starts the unified app connection (audit, notifications, alerts, configuration, updates).
    /// </summary>
    public async Task StartAuditConnectionAsync(CancellationToken ct = default)
    {
        await StartConnectionAsync("app", "/hubs/app");
    }

    /// <summary>
    /// Starts the unified app connection (audit, notifications, alerts, configuration, updates).
    /// </summary>
    public async Task StartNotificationConnectionAsync(CancellationToken ct = default)
    {
        await StartConnectionAsync("app", "/hubs/app");
    }

    /// <summary>
    /// Starts chat connection.
    /// </summary>
    public async Task StartChatConnectionAsync(CancellationToken ct = default)
    {
        await StartConnectionAsync("chat", "/hubs/chat");
    }

    /// <summary>
    /// Starts the document collaboration connection.
    /// </summary>
    public async Task StartDocumentCollaborationConnectionAsync(CancellationToken ct = default)
    {
        await StartConnectionAsync("document-collaboration", "/hubs/document-collaboration");
    }

    /// <summary>
    /// Joins a document collaboration room to receive real-time updates.
    /// </summary>
    public async Task JoinDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("document-collaboration", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("JoinDocument", documentId);
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
    public async Task LeaveDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("document-collaboration", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("LeaveDocument", documentId);
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
    public async Task<bool> RequestDocumentEditLockAsync(Guid documentId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("document-collaboration", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                var lockAcquired = await connection.InvokeAsync<bool>("RequestEditLock", documentId);

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
    public async Task ReleaseDocumentEditLockAsync(Guid documentId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("document-collaboration", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("ReleaseEditLock", documentId);
                _logger.LogInformation("Released lock on document {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock for document {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// Joins a chat room.
    /// </summary>
    public async Task JoinChatAsync(Guid chatId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("JoinChat", chatId);
                _logger.LogInformation("Joined chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join chat {ChatId}", chatId);
                throw;
            }
        }
    }

    /// <summary>
    /// Leaves a chat room.
    /// </summary>
    public async Task LeaveChatAsync(Guid chatId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("LeaveChat", chatId);
                _logger.LogInformation("Left chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave chat {ChatId}", chatId);
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a new chat.
    /// </summary>
    public async Task CreateChatAsync(CreateChatDto createChatDto, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("CreateChat", createChatDto);
                _logger.LogInformation("Created {ChatType} chat with {ParticipantCount} participants",
                    createChatDto.Type, createChatDto.ParticipantIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chat");
                throw;
            }
        }
    }

    /// <summary>
    /// Edits a message.
    /// </summary>
    public async Task EditMessageAsync(EditMessageDto editDto, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("EditMessage", editDto);
                _logger.LogInformation("Edited message {MessageId}", editDto.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit message {MessageId}", editDto.MessageId);
                throw;
            }
        }
    }

    /// <summary>
    /// Deletes a message.
    /// </summary>
    public async Task DeleteMessageAsync(Guid messageId, string? reason = null, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("DeleteMessage", messageId, reason);
                _logger.LogInformation("Deleted message {MessageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
                throw;
            }
        }
    }

    /// <summary>
    /// Marks message as read.
    /// </summary>
    public async Task MarkMessageAsReadAsync(Guid messageId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("chat", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("MarkMessageAsRead", messageId);
                _logger.LogInformation("Marked message {MessageId} as read", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark message {MessageId} as read", messageId);
                throw;
            }
        }
    }

    /// <summary>
    /// Subscribes to specific notification types.
    /// </summary>
    public async Task SubscribeToNotificationTypesAsync(List<NotificationTypes> notificationTypes, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("app", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("SubscribeToNotificationTypes", notificationTypes);
                _logger.LogInformation("Subscribed to notification types: {Types}", string.Join(", ", notificationTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to notification types");
                throw;
            }
        }
    }

    /// <summary>
    /// Unsubscribes from specific notification types.
    /// </summary>
    public async Task UnsubscribeFromNotificationTypesAsync(List<NotificationTypes> notificationTypes, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("app", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("UnsubscribeFromNotificationTypes", notificationTypes);
                _logger.LogInformation("Unsubscribed from notification types: {Types}", string.Join(", ", notificationTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from notification types");
                throw;
            }
        }
    }

    /// <summary>
    /// Acknowledges a notification.
    /// </summary>
    public async Task AcknowledgeNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("app", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("AcknowledgeNotification", notificationId);
                _logger.LogInformation("Acknowledged notification {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acknowledge notification {NotificationId}", notificationId);
                throw;
            }
        }
    }

    /// <summary>
    /// Archives a notification.
    /// </summary>
    public async Task ArchiveNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("app", out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("ArchiveNotification", notificationId);
                _logger.LogInformation("Archived notification {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive notification {NotificationId}", notificationId);
                throw;
            }
        }
    }

    #endregion

    #region Connection State Properties

    // "app" hub consolidates: audit, notifications, alerts, configuration, update-notifications
    public bool IsAppConnected => GetConnectionState("app") == HubConnectionState.Connected;
    public bool IsAuditConnected => IsAppConnected;
    public bool IsNotificationConnected => IsAppConnected;
    public bool IsChatConnected => GetConnectionState("chat") == HubConnectionState.Connected;
    public bool IsDocumentCollaborationConnected => GetConnectionState("document-collaboration") == HubConnectionState.Connected;
    public bool IsUpdateNotificationConnected => IsAppConnected;
    public bool IsFiscalPrinterConnected => GetConnectionState("fiscal-printer") == HubConnectionState.Connected;
    public bool IsAlertsConnected => IsAppConnected;
    public bool IsConfigurationConnected => IsAppConnected;
    public bool IsAllConnected => IsAppConnected && IsChatConnected && IsDocumentCollaborationConnected;

    public async Task SubscribeToPrinterAsync(Guid printerId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("fiscal-printer", out var conn) && conn.State == HubConnectionState.Connected)
        {
            try { await conn.InvokeAsync("SubscribeToPrinter", printerId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to subscribe to printer {PrinterId}", printerId); }
        }
    }

    public async Task UnsubscribeFromPrinterAsync(Guid printerId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue("fiscal-printer", out var conn) && conn.State == HubConnectionState.Connected)
        {
            try { await conn.InvokeAsync("UnsubscribeFromPrinter", printerId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to unsubscribe from printer {PrinterId}", printerId); }
        }
    }

    private HubConnectionState GetConnectionState(string connectionKey)
    {
        return _connections.TryGetValue(connectionKey, out var connection)
            ? connection.State
            : HubConnectionState.Disconnected;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        _connectionHealthTimer?.Dispose();
        _eventBatchTimer?.Dispose();

        var disposeTasks = _connections.Values.Select(connection => connection.DisposeAsync().AsTask());
        await Task.WhenAll(disposeTasks);

        foreach (var semaphore in _connectionLocks.Values)
        {
            semaphore.Dispose();
        }

        _logger.LogInformation("Optimized SignalR service disposed");
    }
}

/// <summary>
/// Custom retry policy optimized for mobile and high-load scenarios.
/// </summary>
public class OptimizedRetryPolicy : IRetryPolicy
{
    private readonly RetryConfiguration _config;

    public OptimizedRetryPolicy(RetryConfiguration config)
    {
        _config = config;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= _config.MaxRetries)
            return null;

        var delay = TimeSpan.FromMilliseconds(
            _config.InitialDelay.TotalMilliseconds *
            Math.Pow(_config.BackoffMultiplier, retryContext.PreviousRetryCount));

        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, _config.MaxDelay.TotalMilliseconds));
    }

    public class RetryConfiguration
    {
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 5;
        public double BackoffMultiplier { get; set; } = 2.0;
    }
}