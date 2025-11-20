using EventForge.DTOs.Common;
using System.Threading.Channels;
using System.Diagnostics;

namespace EventForge.Server.Services.Logging;

/// <summary>
/// Implementation of log ingestion service using a bounded channel for async processing.
/// Uses a DropOldest strategy when the channel is full to ensure newer logs are prioritized.
/// </summary>
public class LogIngestionService : ILogIngestionService
{
    private const int DefaultChannelCapacity = 10000;
    private readonly Channel<ClientLogDto> _logChannel;
    private readonly ILogger<LogIngestionService> _logger;
    
    // Metrics tracking
    private long _droppedCount;
    private readonly object _metricsLock = new();
    private readonly Queue<double> _latencySamples = new(100); // Keep last 100 samples
    private DateTime? _lastProcessedAt;
    private string _circuitBreakerState = "Closed";

    public LogIngestionService(ILogger<LogIngestionService> logger)
    {
        _logger = logger;
        
        // Create bounded channel with DropOldest behavior
        var options = new BoundedChannelOptions(DefaultChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true, // Only one background processor will read
            SingleWriter = false // Multiple API requests can write concurrently
        };
        
        _logChannel = Channel.CreateBounded<ClientLogDto>(options);
        
        _logger.LogInformation(
            "LogIngestionService initialized with channel capacity: {Capacity}",
            DefaultChannelCapacity);
    }

    /// <inheritdoc/>
    public async Task<bool> EnqueueAsync(ClientLogDto logEntry, CancellationToken cancellationToken = default)
    {
        if (logEntry == null)
        {
            _logger.LogWarning("Attempted to enqueue null log entry");
            return false;
        }

        try
        {
            var written = await _logChannel.Writer.WaitToWriteAsync(cancellationToken);
            if (written)
            {
                await _logChannel.Writer.WriteAsync(logEntry, cancellationToken);
                return true;
            }
            else
            {
                // Channel is closed
                Interlocked.Increment(ref _droppedCount);
                _logger.LogWarning("Log channel is closed, log entry dropped");
                return false;
            }
        }
        catch (ChannelClosedException)
        {
            Interlocked.Increment(ref _droppedCount);
            _logger.LogWarning("Log channel is closed, log entry dropped");
            return false;
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _droppedCount);
            _logger.LogDebug("Log enqueue operation cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _droppedCount);
            _logger.LogError(ex, "Error enqueueing log entry");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> EnqueueBatchAsync(IEnumerable<ClientLogDto> logEntries, CancellationToken cancellationToken = default)
    {
        if (logEntries == null)
        {
            _logger.LogWarning("Attempted to enqueue null log batch");
            return 0;
        }

        var successCount = 0;
        foreach (var logEntry in logEntries)
        {
            if (await EnqueueAsync(logEntry, cancellationToken))
            {
                successCount++;
            }
        }

        return successCount;
    }

    /// <inheritdoc/>
    public LogIngestionHealthStatus GetHealthStatus()
    {
        var backlogSize = _logChannel.Reader.Count;
        var droppedCount = Interlocked.Read(ref _droppedCount);
        
        double averageLatency;
        DateTime? lastProcessed;
        string circuitState;
        
        lock (_metricsLock)
        {
            averageLatency = _latencySamples.Count > 0 ? _latencySamples.Average() : 0;
            lastProcessed = _lastProcessedAt;
            circuitState = _circuitBreakerState;
        }

        // Determine health status based on metrics
        var status = HealthStatus.Healthy;
        
        if (circuitState == "Open")
        {
            status = HealthStatus.Unhealthy;
        }
        else if (backlogSize > DefaultChannelCapacity * 0.8 || droppedCount > 0)
        {
            status = HealthStatus.Degraded;
        }

        return new LogIngestionHealthStatus
        {
            Status = status,
            BacklogSize = backlogSize,
            DroppedCount = droppedCount,
            AverageLatencyMs = averageLatency,
            CircuitBreakerState = circuitState,
            LastProcessedAt = lastProcessed
        };
    }

    /// <summary>
    /// Gets the channel reader for the background service to consume logs.
    /// </summary>
    internal ChannelReader<ClientLogDto> GetChannelReader() => _logChannel.Reader;

    /// <summary>
    /// Updates processing metrics (called by background service).
    /// </summary>
    internal void UpdateMetrics(double latencyMs)
    {
        lock (_metricsLock)
        {
            _latencySamples.Enqueue(latencyMs);
            if (_latencySamples.Count > 100)
            {
                _latencySamples.Dequeue();
            }
            _lastProcessedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates circuit breaker state (called by background service).
    /// </summary>
    internal void UpdateCircuitBreakerState(string state)
    {
        lock (_metricsLock)
        {
            _circuitBreakerState = state;
        }
    }
}
