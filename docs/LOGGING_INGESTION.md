# Log Ingestion Pipeline

## Overview

The EventForge log ingestion pipeline provides a resilient, non-blocking mechanism for processing client-side logs. It uses a bounded channel for queuing, background processing with retry logic, circuit breaker patterns, and fallback file logging to ensure reliability even under high load or system failures.

## Architecture

### Components

#### 1. LogIngestionService

A singleton service that manages a bounded channel for enqueueing client logs.

**Key Features:**
- **Bounded Channel**: Capacity of 10,000 logs with `DropOldest` behavior
- **Non-blocking**: Enqueue operations return immediately
- **Metrics Tracking**: Monitors backlog size, dropped logs, processing latency
- **Thread-safe**: Supports concurrent writes from multiple API requests

**API Methods:**
```csharp
Task<bool> EnqueueAsync(ClientLogDto logEntry, CancellationToken cancellationToken = default)
Task<int> EnqueueBatchAsync(IEnumerable<ClientLogDto> logEntries, CancellationToken cancellationToken = default)
LogIngestionHealthStatus GetHealthStatus()
```

#### 2. LogIngestionBackgroundService

A hosted background service that processes logs from the channel.

**Key Features:**
- **Batch Processing**: Processes up to 200 logs per batch for efficiency
- **Polly Resilience**: 
  - Retry with exponential backoff: 1s, 3s, 10s
  - Circuit breaker: Opens after 5 consecutive failures for 30 seconds
- **Fallback Mechanism**: Writes to daily file (`logs/client-fallback-YYYYMMDD.log`) when circuit breaker opens
- **Graceful Shutdown**: Processes remaining logs on application shutdown

**Fallback File Format:**
Each line is a JSON object containing:
```json
{
  "Timestamp": "2025-11-20T17:45:00Z",
  "Reason": "CircuitBreakerOpen",
  "ClientLog": { /* ClientLogDto properties */ }
}
```

#### 3. ClientLogsController

Updated to enqueue logs asynchronously instead of synchronous logging.

**Endpoints:**
- `POST /api/ClientLogs` - Single log entry
- `POST /api/ClientLogs/batch` - Batch of log entries (rate limited)
- `GET /api/ClientLogs/ingestion/health` - Health status endpoint

**Rate Limiting:**
- Applied to batch endpoint
- Sliding window: 100 requests per minute
- 6 segments of 10 seconds each
- Queues up to 10 additional requests
- Partitioned by remote IP address

## Health Monitoring

### Health Endpoint

**GET** `/api/ClientLogs/ingestion/health`

Returns ingestion pipeline health status:

```json
{
  "status": "Healthy",
  "backlogSize": 42,
  "droppedCount": 0,
  "averageLatencyMs": 12.5,
  "circuitBreakerState": "Closed",
  "lastProcessedAt": "2025-11-20T17:45:30Z"
}
```

**Status Values:**
- `Healthy` - System operating normally
- `Degraded` - Backlog > 80% capacity or logs have been dropped
- `Unhealthy` - Circuit breaker is open

## Configuration

### Channel Capacity

Currently hardcoded at 10,000. Can be made configurable via `appsettings.json` in future:

```json
{
  "LogIngestion": {
    "ChannelCapacity": 10000,
    "BatchSize": 200
  }
}
```

### Rate Limiting

Configured in `Program.cs`:
- 100 requests per minute per IP
- Sliding window with 6 segments
- Queue limit of 10 requests

Can be adjusted based on load requirements.

### Retry Policy

Exponential backoff with 3 attempts:
1. 1 second delay
2. 3 second delay  
3. 10 second delay

### Circuit Breaker

- Breaks after 5 consecutive failures
- Remains open for 30 seconds
- Automatically resets when service recovers

## Fallback Logging

When the circuit breaker opens (indicating persistent logging failures), logs are written to:

```
logs/client-fallback-YYYYMMDD.log
```

Each entry includes:
- UTC timestamp
- Failure reason
- Full client log data as JSON

**Recovery:** Once the system recovers, these fallback files can be:
1. Manually reviewed
2. Re-processed by a recovery tool (future enhancement)
3. Archived for audit purposes

## Benefits

### For Developers

- **Non-blocking API**: Controllers return immediately, improving response times
- **Resilience**: System continues to accept logs even when processing is slow
- **Observability**: Health endpoint provides real-time pipeline status

### For Operations

- **Graceful Degradation**: Falls back to file logging when database is unavailable
- **No Log Loss**: Bounded channel with metrics ensures visibility into dropped logs
- **Self-Healing**: Circuit breaker automatically recovers when system stabilizes

### For System Reliability

- **Backpressure Handling**: DropOldest strategy prioritizes recent logs
- **Resource Protection**: Limits memory usage with bounded channel
- **Failure Isolation**: Circuit breaker prevents cascading failures

## Usage Examples

### Client (JavaScript/TypeScript)

No changes required - existing client code continues to work:

```typescript
await fetch('/api/ClientLogs/batch', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ logs: batchOfLogs })
});
```

### Monitoring Health

```bash
curl https://localhost:7241/api/ClientLogs/ingestion/health
```

### Viewing Fallback Logs

```bash
cat logs/client-fallback-20251120.log | jq
```

## Future Enhancements

1. **Configuration**: Move hardcoded values to `appsettings.json`
2. **Metrics Export**: Export metrics to Prometheus/Application Insights
3. **Fallback Recovery**: Tool to replay fallback logs when system recovers
4. **Adaptive Rate Limiting**: Adjust limits based on system health
5. **Partitioning**: Multiple channels for different log priorities

## Troubleshooting

### High Backlog Size

**Symptom:** `backlogSize` consistently high (> 8000)

**Possible Causes:**
- Database is slow
- Processing batch size too small
- High volume of incoming logs

**Solutions:**
- Increase batch size
- Optimize database indexes
- Scale horizontally with multiple instances

### Circuit Breaker Frequently Opening

**Symptom:** `circuitBreakerState` frequently "Open"

**Possible Causes:**
- Database connectivity issues
- Network problems
- Resource constraints

**Solutions:**
- Check database health
- Review network configuration
- Increase resource allocation

### Logs Being Dropped

**Symptom:** `droppedCount` increasing

**Possible Causes:**
- Channel capacity too low
- Processing too slow
- Burst of logs exceeding capacity

**Solutions:**
- Increase channel capacity
- Optimize processing performance
- Implement client-side rate limiting

## Related Documentation

- [LOGGING_CONFIGURATION.md](./LOGGING_CONFIGURATION.md) - Overall logging setup
- [CLIENT_LOGGING_IMPLEMENTATION.md](./migration/CLIENT_LOGGING_IMPLEMENTATION.md) - Client-side logging guide
