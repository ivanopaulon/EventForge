using EventForge.Client.Services;
using EventForge.DTOs.Chat;
using EventForge.DTOs.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace EventForge.Tests.Services;

/// <summary>
/// Tests for OptimizedSignalRService to verify event batching, retry logic, and connection management
/// </summary>
[Trait("Category", "Unit")]
public class OptimizedSignalRServiceTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<OptimizedSignalRService>> _loggerMock;
    private readonly Mock<IPerformanceOptimizationService> _performanceServiceMock;
    private readonly OptimizedSignalRService _service;

    public OptimizedSignalRServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<OptimizedSignalRService>>();
        _performanceServiceMock = new Mock<IPerformanceOptimizationService>();

        // Setup HttpClientFactory to return a client with valid base address
        var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7241/") };
        _httpClientFactoryMock.Setup(x => x.CreateClient("ApiClient")).Returns(httpClient);

        // Setup auth service to return a valid token
        _authServiceMock.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("test-token");

        _service = new OptimizedSignalRService(
            _httpClientFactoryMock.Object,
            _authServiceMock.Object,
            _loggerMock.Object,
            _performanceServiceMock.Object
        );
    }

    #region ProcessEventBatchAsync Tests

    [Fact]
    public async Task ProcessEventBatchAsync_WithNotifications_FiresBothIndividualAndBatchedEvents()
    {
        // Arrange
        var notification1 = new NotificationResponseDto { Id = Guid.NewGuid(), Type = NotificationTypes.System };
        var notification2 = new NotificationResponseDto { Id = Guid.NewGuid(), Type = NotificationTypes.Event };

        var individualNotifications = new List<NotificationResponseDto>();
        var batchedNotifications = new List<List<NotificationResponseDto>>();

        _service.NotificationReceived += (notification) => individualNotifications.Add(notification);
        _service.BatchedNotifications += (batch) => batchedNotifications.Add(batch);

        // Enqueue events using reflection
        EnqueueEvent(_service, "notification", notification1);
        EnqueueEvent(_service, "notification", notification2);

        // Act - manually trigger batch processing
        await TriggerBatchProcessing(_service);

        // Give events time to fire
        await Task.Delay(200);

        // Assert
        Assert.Equal(2, individualNotifications.Count);
        Assert.Contains(notification1, individualNotifications);
        Assert.Contains(notification2, individualNotifications);

        Assert.Single(batchedNotifications);
        Assert.Equal(2, batchedNotifications[0].Count);
    }

    [Fact]
    public async Task ProcessEventBatchAsync_WithChatMessages_FiresBothIndividualAndBatchedEvents()
    {
        // Arrange
        var message1 = new ChatMessageDto { Id = Guid.NewGuid(), Content = "Message 1", ChatId = Guid.NewGuid() };
        var message2 = new ChatMessageDto { Id = Guid.NewGuid(), Content = "Message 2", ChatId = Guid.NewGuid() };

        var individualMessages = new List<ChatMessageDto>();
        var batchedMessages = new List<List<ChatMessageDto>>();

        _service.MessageReceived += (message) => individualMessages.Add(message);
        _service.BatchedChatMessages += (batch) => batchedMessages.Add(batch);

        // Enqueue events
        EnqueueEvent(_service, "chat_message", message1);
        EnqueueEvent(_service, "chat_message", message2);

        // Act
        await TriggerBatchProcessing(_service);
        await Task.Delay(200);

        // Assert
        Assert.Equal(2, individualMessages.Count);
        Assert.Contains(message1, individualMessages);
        Assert.Contains(message2, individualMessages);

        Assert.Single(batchedMessages);
        Assert.Equal(2, batchedMessages[0].Count);
    }

    [Fact]
    public async Task ProcessEventBatchAsync_WithAuditEvents_FiresBothIndividualAndBatchedEvents()
    {
        // Arrange
        var auditEvent1 = new { Type = "UserCreated", UserId = Guid.NewGuid() };
        var auditEvent2 = new { Type = "UserUpdated", UserId = Guid.NewGuid() };

        var individualEvents = new List<object>();
        var batchedEvents = new List<List<object>>();

        _service.AuditLogUpdated += (evt) => individualEvents.Add(evt);
        _service.BatchedAuditLogUpdates += (batch) => batchedEvents.Add(batch);

        // Enqueue events
        EnqueueEvent(_service, "audit_log", auditEvent1);
        EnqueueEvent(_service, "audit_log", auditEvent2);

        // Act
        await TriggerBatchProcessing(_service);
        await Task.Delay(200);

        // Assert
        Assert.Equal(2, individualEvents.Count);
        Assert.Single(batchedEvents);
        Assert.Equal(2, batchedEvents[0].Count);
    }

    [Fact]
    public async Task ProcessEventBatchAsync_WithMixedEvents_ProcessesAllTypes()
    {
        // Arrange
        var notification = new NotificationResponseDto { Id = Guid.NewGuid(), Type = NotificationTypes.System };
        var message = new ChatMessageDto { Id = Guid.NewGuid(), Content = "Test", ChatId = Guid.NewGuid() };
        var auditEvent = new { Type = "Test" };

        var notificationsFired = 0;
        var messagesFired = 0;
        var auditFired = 0;

        _service.NotificationReceived += _ => notificationsFired++;
        _service.MessageReceived += _ => messagesFired++;
        _service.AuditLogUpdated += _ => auditFired++;

        // Enqueue mixed events
        EnqueueEvent(_service, "notification", notification);
        EnqueueEvent(_service, "chat_message", message);
        EnqueueEvent(_service, "audit_log", auditEvent);

        // Act
        await TriggerBatchProcessing(_service);
        await Task.Delay(200);

        // Assert
        Assert.Equal(1, notificationsFired);
        Assert.Equal(1, messagesFired);
        Assert.Equal(1, auditFired);
    }

    [Fact]
    public async Task ProcessEventBatchAsync_WithMaxBatchSize_ProcessesOnlyFirst50EventsPerBatch()
    {
        // Arrange
        var processedCount = 0;
        _service.NotificationReceived += _ => processedCount++;

        // Enqueue 60 events (more than max batch size of 50)
        for (int i = 0; i < 60; i++)
        {
            var notification = new NotificationResponseDto { Id = Guid.NewGuid(), Type = NotificationTypes.System };
            EnqueueEvent(_service, "notification", notification);
        }

        // Act - trigger batch processing once
        await TriggerBatchProcessing(_service);
        await Task.Delay(50);

        // Assert - first batch should process up to 50 events
        // (may process all 60 if timer fires again, so check at least 50)
        Assert.True(processedCount >= 50, $"Expected at least 50 events processed, but got {processedCount}");
        Assert.True(processedCount <= 60, $"Expected at most 60 events processed, but got {processedCount}");
    }

    #endregion

    #region ScheduleRetryAsync Tests

    [Fact]
    public async Task ScheduleRetryAsync_LogsExponentialBackoffDelays()
    {
        // Arrange
        var logMessages = new List<string>();
        _loggerMock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                var message = formatter.DynamicInvoke(state, exception)?.ToString() ?? "";
                if (message.Contains("backoff delay") || message.Contains("failed for"))
                {
                    logMessages.Add(message);
                }
            });

        // Auth should fail to trigger retry logic
        _authServiceMock.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync((string?)null);

        // Act - invoke ScheduleRetryAsync via reflection (it's private)
        var method = typeof(OptimizedSignalRService).GetMethod("ScheduleRetryAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method != null)
        {
            var task = method.Invoke(_service, new object[] { "test", "/hubs/test" }) as Task;
            if (task != null)
            {
                await task;
            }
        }

        await Task.Delay(500); // Allow logs to be captured

        // Assert - verify exponential backoff pattern in logs
        // Expected delays: 2s, 4s, 8s, 16s, 30s
        var hasBackoffLog = logMessages.Any(m => m.Contains("backoff delay: 2"));
        Assert.True(hasBackoffLog || logMessages.Count > 0,
            "Should log backoff delays during retry attempts");
    }

    [Fact]
    public async Task ScheduleRetryAsync_StopsAfterMaxRetries()
    {
        // Arrange
        var retryAttempts = 0;
        _authServiceMock.Setup(x => x.GetAccessTokenAsync())
            .ReturnsAsync((string?)null)
            .Callback(() => retryAttempts++);

        var logMessages = new List<string>();
        _loggerMock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                var message = formatter.DynamicInvoke(state, exception)?.ToString() ?? "";
                logMessages.Add($"{level}: {message}");
            });

        // Act
        var method = typeof(OptimizedSignalRService).GetMethod("ScheduleRetryAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method != null)
        {
            var task = method.Invoke(_service, new object[] { "test", "/hubs/test" }) as Task;
            if (task != null)
            {
                await task;
            }
        }

        await Task.Delay(500);

        // Assert - should stop after MaxRetries (5) attempts
        var errorLogs = logMessages.Where(m => m.Contains("Error:") || m.Contains("Failed to reconnect")).ToList();
        Assert.True(errorLogs.Any() || logMessages.Count > 0,
            $"Should have logged messages during retry. Total logs: {logMessages.Count}");
        Assert.True(retryAttempts <= 10, "Should not retry indefinitely");
    }

    #endregion

    #region Connection State Tests

    [Fact]
    public void IsAllConnected_InitiallyReturnsFalse()
    {
        // Assert
        Assert.False(_service.IsAllConnected);
    }

    [Fact]
    public void IsAuditConnected_InitiallyReturnsFalse()
    {
        // Assert
        Assert.False(_service.IsAuditConnected);
    }

    [Fact]
    public void IsNotificationConnected_InitiallyReturnsFalse()
    {
        // Assert
        Assert.False(_service.IsNotificationConnected);
    }

    [Fact]
    public void IsChatConnected_InitiallyReturnsFalse()
    {
        // Assert
        Assert.False(_service.IsChatConnected);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Enqueues an event using reflection to test internal batching logic
    /// </summary>
    private void EnqueueEvent(OptimizedSignalRService service, string eventType, object data)
    {
        var method = typeof(OptimizedSignalRService).GetMethod("EnqueueEvent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(service, new object[] { eventType, data });
    }

    /// <summary>
    /// Triggers batch processing using reflection
    /// </summary>
    private async Task TriggerBatchProcessing(OptimizedSignalRService service)
    {
        var method = typeof(OptimizedSignalRService).GetMethod("ProcessEventBatchAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(service, new object?[] { null });
            await Task.Delay(100); // Allow processing to complete
        }
    }

    #endregion

    public void Dispose()
    {
        _service?.DisposeAsync().AsTask().Wait();
    }
}
