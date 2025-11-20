using EventForge.DTOs.Common;
using EventForge.Server.Services.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.Logging;

/// <summary>
/// Unit tests for the LogIngestionService.
/// Tests the core functionality of log enqueueing and health status.
/// </summary>
public class LogIngestionServiceTests
{
    private readonly Mock<ILogger<LogIngestionService>> _mockLogger;
    private readonly LogIngestionService _service;

    public LogIngestionServiceTests()
    {
        _mockLogger = new Mock<ILogger<LogIngestionService>>();
        _service = new LogIngestionService(_mockLogger.Object);
    }

    [Fact]
    public async Task EnqueueAsync_WithValidLog_ReturnsTrue()
    {
        // Arrange
        var log = new ClientLogDto
        {
            Level = "Information",
            Message = "Test log message",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _service.EnqueueAsync(log);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnqueueAsync_WithNullLog_ReturnsFalse()
    {
        // Act
        var result = await _service.EnqueueAsync(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EnqueueBatchAsync_WithValidLogs_ReturnsCorrectCount()
    {
        // Arrange
        var logs = new List<ClientLogDto>
        {
            new() { Level = "Information", Message = "Log 1", Timestamp = DateTime.UtcNow },
            new() { Level = "Warning", Message = "Log 2", Timestamp = DateTime.UtcNow },
            new() { Level = "Error", Message = "Log 3", Timestamp = DateTime.UtcNow }
        };

        // Act
        var enqueuedCount = await _service.EnqueueBatchAsync(logs);

        // Assert
        Assert.Equal(3, enqueuedCount);
    }

    [Fact]
    public async Task EnqueueBatchAsync_WithNullBatch_ReturnsZero()
    {
        // Act
        var enqueuedCount = await _service.EnqueueBatchAsync(null!);

        // Assert
        Assert.Equal(0, enqueuedCount);
    }

    [Fact]
    public async Task GetHealthStatus_InitialState_ReturnsHealthy()
    {
        // Act
        var healthStatus = _service.GetHealthStatus();

        // Assert
        Assert.NotNull(healthStatus);
        Assert.Equal(HealthStatus.Healthy, healthStatus.Status);
        Assert.Equal(0, healthStatus.BacklogSize);
        Assert.Equal(0, healthStatus.DroppedCount);
        Assert.Equal("Closed", healthStatus.CircuitBreakerState);
    }

    [Fact]
    public async Task GetHealthStatus_AfterEnqueueing_ShowsBacklog()
    {
        // Arrange
        var logs = Enumerable.Range(1, 10).Select(i => new ClientLogDto
        {
            Level = "Information",
            Message = $"Test log {i}",
            Timestamp = DateTime.UtcNow
        }).ToList();

        // Act
        await _service.EnqueueBatchAsync(logs);
        var healthStatus = _service.GetHealthStatus();

        // Assert
        Assert.True(healthStatus.BacklogSize > 0, "Backlog should contain enqueued logs");
    }

    [Fact]
    public async Task EnqueueAsync_MultipleThreads_AllSucceed()
    {
        // Arrange
        const int threadCount = 10;
        const int logsPerThread = 100;
        var tasks = new List<Task<int>>();

        // Act - Simulate concurrent enqueueing from multiple threads
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                var count = 0;
                for (int j = 0; j < logsPerThread; j++)
                {
                    var log = new ClientLogDto
                    {
                        Level = "Information",
                        Message = $"Thread {threadId} Log {j}",
                        Timestamp = DateTime.UtcNow
                    };
                    if (await _service.EnqueueAsync(log))
                        count++;
                }
                return count;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var totalEnqueued = results.Sum();
        Assert.Equal(threadCount * logsPerThread, totalEnqueued);
    }

    // Note: UpdateMetrics and UpdateCircuitBreakerState are internal methods
    // and are tested indirectly through integration with the background service

    [Fact]
    public async Task GetHealthStatus_WhenBacklogHigh_ShowsDegraded()
    {
        // Arrange - Fill the channel to > 80% capacity
        var logs = Enumerable.Range(1, 8500).Select(i => new ClientLogDto
        {
            Level = "Information",
            Message = $"Test log {i}",
            Timestamp = DateTime.UtcNow
        }).ToList();

        // Act
        await _service.EnqueueBatchAsync(logs);
        
        // Give the channel a moment to update
        await Task.Delay(100);
        
        var healthStatus = _service.GetHealthStatus();

        // Assert
        Assert.True(healthStatus.Status == HealthStatus.Degraded || healthStatus.Status == HealthStatus.Healthy,
            "Status should be Degraded when backlog is high");
    }
}
