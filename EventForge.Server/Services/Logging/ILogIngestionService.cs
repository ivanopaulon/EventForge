namespace EventForge.Server.Services.Logging;

/// <summary>
/// Service interface for ingesting client logs into a processing pipeline.
/// Provides a resilient, non-blocking log ingestion mechanism with health monitoring.
/// </summary>
public interface ILogIngestionService
{
    /// <summary>
    /// Enqueues a single client log entry for asynchronous processing.
    /// </summary>
    /// <param name="logEntry">The client log entry to enqueue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if enqueued successfully, false if queue is full</returns>
    Task<bool> EnqueueAsync(ClientLogDto logEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a batch of client log entries for asynchronous processing.
    /// </summary>
    /// <param name="logEntries">The collection of client log entries to enqueue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs successfully enqueued</returns>
    Task<int> EnqueueBatchAsync(IEnumerable<ClientLogDto> logEntries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of the log ingestion pipeline.
    /// </summary>
    /// <returns>Health status information including backlog size, metrics, and circuit breaker state</returns>
    LogIngestionHealthStatus GetHealthStatus();
}

/// <summary>
/// Represents the health status of the log ingestion pipeline.
/// </summary>
public class LogIngestionHealthStatus
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Current number of logs waiting to be processed
    /// </summary>
    public int BacklogSize { get; set; }

    /// <summary>
    /// Total number of logs dropped due to queue overflow
    /// </summary>
    public long DroppedCount { get; set; }

    /// <summary>
    /// Average processing latency in milliseconds
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public string CircuitBreakerState { get; set; } = "Closed";

    /// <summary>
    /// Timestamp of the last successfully processed log
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is operating normally
    /// </summary>
    Healthy,

    /// <summary>
    /// System is operational but experiencing issues
    /// </summary>
    Degraded,

    /// <summary>
    /// System is not functioning properly
    /// </summary>
    Unhealthy
}
