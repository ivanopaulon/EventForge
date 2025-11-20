using EventForge.DTOs.Chat;
using EventForge.Client.Services.Performance;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;
using EventForge.DTOs.Notifications;
using EventForge.Client.Services.Performance;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace EventForge.Client.Services.Infrastructure;

/// <summary>
/// Optimized SignalR service with connection pooling, event batching, and performance monitoring.
/// Reduces latency and improves scalability for multiple users and high load scenarios.
/// </summary>
public class OptimizedSignalRService : IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<OptimizedSignalRService> _logger;
    private readonly IPerformanceOptimizationService _performanceService;

    // Connection management
    private readonly ConcurrentDictionary<string, HubConnection> _connections;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLocks;
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

    #region Events (optimized with batching)
    public event Action<List<object>>? BatchedAuditLogUpdates;
    public event Action<List<NotificationResponseDto>>? BatchedNotifications;
    public event Action<List<ChatMessageDto>>? BatchedChatMessages;
    public event Action<TypingIndicatorDto>? TypingIndicator;
    #endregion

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
    public async Task StartAllConnectionsAsync()
    {
        var connectionTasks = new List<Task>
        {
            StartConnectionAsync("audit", "/hubs/audit-log"),
            StartConnectionAsync("notification", "/hubs/notifications"),
            StartConnectionAsync("chat", "/hubs/chat")
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
            _ = Task.Run(() => ScheduleRetryAsync(connectionKey, hubPath));
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
                options.AccessTokenProvider = () => Task.FromResult(token!);
                // Optimize for mobile and high-load scenarios
                options.SkipNegotiation = true;
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                options.WebSocketConfiguration = ws =>
                {
                    ws.KeepAliveInterval = TimeSpan.FromSeconds(30);
                };
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
            case "audit":
                RegisterAuditEventHandlers(connection);
                break;
            case "notification":
                RegisterNotificationEventHandlers(connection);
                break;
            case "chat":
                RegisterChatEventHandlers(connection);
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
    }

    private void RegisterChatEventHandlers(HubConnection connection)
    {
        _ = connection.On<ChatMessageDto>("MessageReceived", message =>
        {
            EnqueueEvent("chat_message", message);
        });

        // Typing indicators are not batched for responsiveness
        _ = connection.On<TypingIndicatorDto>("TypingIndicator", indicator =>
        {
            TypingIndicator?.Invoke(indicator);
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
                        break;
                    case "notification":
                    case "system_notification":
                        if (batchedEvent.Data is NotificationResponseDto notification)
                            notifications.Add(notification);
                        break;
                    case "chat_message":
                        if (batchedEvent.Data is ChatMessageDto message)
                            chatMessages.Add(message);
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

    private string GetHubPath(string connectionKey) => connectionKey switch
    {
        "audit" => "/hubs/audit-log",
        "notification" => "/hubs/notifications",
        "chat" => "/hubs/chat",
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
            case "notification":
                _performanceService.InvalidateCache(CacheKeys.NOTIFICATION_LIST);
                break;
        }

        // Schedule retry with exponential backoff
        await ScheduleRetryAsync(connectionKey, GetHubPath(connectionKey));
    }

    /// <summary>
    /// Schedules connection retry with exponential backoff.
    /// </summary>
    private async Task ScheduleRetryAsync(string connectionKey, string hubPath)
    {
        var retryCount = 0;
        var delay = _retryConfig.InitialDelay;

        while (retryCount < _retryConfig.MaxRetries)
        {
            await Task.Delay(delay);

            try
            {
                await StartConnectionAsync(connectionKey, hubPath);
                return; // Success
            }
            catch (Exception ex)
            {
                retryCount++;
                delay = TimeSpan.FromMilliseconds(Math.Min(
                    delay.TotalMilliseconds * _retryConfig.BackoffMultiplier,
                    _retryConfig.MaxDelay.TotalMilliseconds));

                _logger.LogWarning(ex, "Retry {Count}/{Max} failed for {ConnectionKey}",
                    retryCount, _retryConfig.MaxRetries, connectionKey);
            }
        }

        _logger.LogError("Failed to reconnect {ConnectionKey} after {MaxRetries} attempts",
            connectionKey, _retryConfig.MaxRetries);
    }

    #endregion

    #region Connection-specific Methods

    private async Task OnConnectionEstablishedAsync(string connectionKey)
    {
        switch (connectionKey)
        {
            case "audit":
                await JoinAuditGroupAsync();
                break;
            case "notification":
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
        if (_connections.TryGetValue("audit", out var connection) &&
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
    public async Task SendChatMessageAsync(SendMessageDto messageDto)
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
    public async Task SendTypingIndicatorAsync(Guid chatId, bool isTyping)
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

    #endregion

    #region Connection State Properties

    public bool IsAuditConnected => GetConnectionState("audit") == HubConnectionState.Connected;
    public bool IsNotificationConnected => GetConnectionState("notification") == HubConnectionState.Connected;
    public bool IsChatConnected => GetConnectionState("chat") == HubConnectionState.Connected;
    public bool IsAllConnected => IsAuditConnected && IsNotificationConnected && IsChatConnected;

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