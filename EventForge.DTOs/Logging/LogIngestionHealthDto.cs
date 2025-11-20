using System;

namespace EventForge.DTOs.Logging;

/// <summary>
/// Data transfer object for log ingestion health status.
/// Provides visibility into the health and performance of the log ingestion pipeline.
/// </summary>
public class LogIngestionHealthDto
{
    /// <summary>
    /// Overall health status of the ingestion pipeline
    /// </summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>
    /// Current number of logs waiting to be processed in the queue
    /// </summary>
    public int BacklogSize { get; set; }

    /// <summary>
    /// Total number of logs dropped since service start (due to queue overflow)
    /// </summary>
    public long DroppedCount { get; set; }

    /// <summary>
    /// Average processing latency in milliseconds (based on recent samples)
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Current state of the circuit breaker (Closed, Open, HalfOpen)
    /// </summary>
    public string CircuitBreakerState { get; set; } = "Closed";

    /// <summary>
    /// Timestamp of the last successfully processed log (UTC)
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }
}
