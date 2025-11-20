# Client-Side Centralized Logging Implementation

This document describes the implementation of centralized client-side logging for EventForge, allowing Blazor client logs to be sent to the existing server logging infrastructure (Serilog).

## Overview

The centralized logging system captures client-side errors, warnings, and information logs and sends them to the server where they are integrated with the existing Serilog infrastructure. This provides a unified view of both server and client logs for better debugging and monitoring.

## Architecture

### Backend Components

#### 1. ClientLogsController (`/api/ClientLogs`)

**Endpoints:**
- `POST /api/ClientLogs` - Accept single client log entry (async, non-blocking)
- `POST /api/ClientLogs/batch` - Accept multiple client log entries for efficient processing (rate-limited)
- `GET /api/ClientLogs/ingestion/health` - Get ingestion pipeline health status

**Features:**
- **Resilient Log Ingestion Pipeline**: Logs are queued and processed asynchronously
- **Non-blocking**: Controllers return 202 Accepted immediately
- **Polly Resilience**: Automatic retry with exponential backoff and circuit breaker
- **Fallback Logging**: Writes to file when database is unavailable
- Integrates with existing Serilog infrastructure
- No new database tables required
- Structured logging with custom properties
- **Anonymous access enabled** - Allows logging without authentication (critical for login/startup errors)
- Captures authentication context when available (UserId, UserName)
- **Rate limiting**: Batch endpoint limited to 100 requests/minute per IP

For detailed information about the log ingestion pipeline, see [LOGGING_INGESTION.md](../LOGGING_INGESTION.md).

#### 2. ClientLogDto and ClientLogBatchDto

**ClientLogDto Properties:**
- `Level`: Log level (Debug, Information, Warning, Error, Critical)
- `Message`: Log message (max 5000 chars)
- `Page`: Current page/route where log occurred
- `UserId`: User ID if authenticated
- `Exception`: Exception details if applicable
- `UserAgent`: Browser/client information
- `Properties`: Additional properties as JSON string
- `Timestamp`: Client timestamp when log was generated
- `CorrelationId`: Correlation ID for tracing
- `Category`: Logger category/name

### Frontend Components

#### 1. ClientLogService (`IClientLogService`)

**Main Features:**
- Standard logging methods: `LogDebugAsync`, `LogInformationAsync`, `LogWarningAsync`, `LogErrorAsync`, `LogCriticalAsync`
- Batch processing for efficiency
- Offline support with localStorage persistence
- Automatic retry and buffer management
- Context enrichment (current page, user info, user agent)

**Configuration Options:**
- Batch size (default: 10 logs)
- Flush interval (default: 1 minute)
- Offline mode (default: enabled)
- Maximum local logs (default: 1000)

#### 2. Global Error Handling

**Components:**
- `GlobalErrorHandler.razor`: Handles .NET exceptions in Blazor
- `JavaScriptErrorHelper.cs`: Handles JavaScript errors via JSInterop
- Automatic error capture and logging
- Fallback to console logging if service fails

#### 3. Developer UI (`/superadmin/client-logs`)

**Features:**
- View local logs with filtering and search
- Statistics dashboard (total logs, errors, warnings, info)
- Manual flush to server
- Export logs as JSON
- Clear local storage
- Test log generation
- Detailed log inspection dialog

## Usage Examples

### Basic Logging

```csharp
@inject IClientLogService ClientLogService

// Information log
await ClientLogService.LogInformationAsync("User clicked button", "UI");

// Error with exception
try 
{
    // Some operation
}
catch (Exception ex)
{
    await ClientLogService.LogErrorAsync("Operation failed", ex, "BusinessLogic");
}

// Warning with custom properties
var properties = new Dictionary<string, object>
{
    ["userId"] = currentUserId,
    ["action"] = "deleteItem",
    ["itemId"] = itemId
};
await ClientLogService.LogWarningAsync("User attempted to delete protected item", "Security", properties);
```

### Advanced Configuration

```csharp
// In component or service
ClientLogService.SetBatchSize(20);
ClientLogService.SetFlushInterval(TimeSpan.FromMinutes(2));
ClientLogService.EnableOfflineMode(true);

// Manual flush
await ClientLogService.FlushAsync();

// Get local logs for inspection
var localLogs = await ClientLogService.GetLocalLogsAsync();
```

## Integration with Existing Infrastructure

### Serilog Integration

Client logs are logged to the existing Serilog infrastructure with these custom properties:

```json
{
  "Source": "Client",
  "UserId": "user-guid",
  "UserName": "username",
  "Page": "/current-page",
  "ClientTimestamp": "2024-01-01T12:00:00Z",
  "CorrelationId": "correlation-id",
  "UserAgent": "browser-info",
  "Category": "logger-category",
  "RemoteIpAddress": "client-ip",
  "RequestPath": "/api/ClientLogs",
  "ClientProperties": "{custom-properties-json}"
}
```

### Dashboard and Query Integration

Since client logs use the existing Serilog infrastructure, they automatically appear in:

- File logs (`Logs/log-*.txt`)
- SQL Server logs (if configured)
- Any existing monitoring dashboards (Grafana, PowerBI, Kibana)
- Log aggregation queries

**Example Queries:**

```sql
-- Get all client errors from last 24 hours
SELECT * FROM Logs 
WHERE Properties LIKE '%"Source":"Client"%' 
  AND Level = 'Error' 
  AND TimeStamp > DATEADD(hour, -24, GETUTCDATE())

-- Get client logs by page
SELECT * FROM Logs 
WHERE Properties LIKE '%"Source":"Client"%' 
  AND Properties LIKE '%"Page":"/specific-page"%'
```

## Security and Privacy

### Authentication
- Client logs endpoint allows both authenticated and anonymous requests
- Authenticated users have their `UserId` automatically captured
- Anonymous logs are accepted but marked as `UserId: "anonymous"`

### Validation and Limits
- Maximum message length: 5000 characters
- Maximum exception length: 10000 characters
- Maximum properties length: 5000 characters
- Batch size limit: 100 logs per request
- Rate limiting ready (simplified for compatibility)

### Privacy Considerations
- User agent information is captured for debugging
- Client IP address is logged on server side
- Sensitive information should not be logged in messages or properties
- Local storage is cleared periodically to prevent data accumulation

## Performance Considerations

### Client-Side
- Logs are batched for efficient network usage
- Automatic background flushing every minute
- Critical/Error logs are sent immediately for urgent issues
- Local storage used for offline scenarios
- Maximum 1000 logs stored locally to prevent storage bloat

### Server-Side
- Lightweight controller with minimal processing
- Integrates with existing Serilog infrastructure
- No additional database tables or schemas required
- Uses structured logging for efficient storage and querying

## Monitoring and Alerting

### Built-in Monitoring
- Health check endpoint: `GET /api/ClientLogs/health`
- Client-side error counting and statistics in developer UI
- Automatic correlation IDs for cross-system tracing

### Alerting Examples
Configure alerts in your existing monitoring system:

```sql
-- Alert on high client error rate
SELECT COUNT(*) as ErrorCount 
FROM Logs 
WHERE Properties LIKE '%"Source":"Client"%' 
  AND Level IN ('Error', 'Critical')
  AND TimeStamp > DATEADD(minute, -5, GETUTCDATE())
HAVING COUNT(*) > 10
```

## Deployment Notes

### Backend Deployment
- No database migrations required
- No new configuration needed beyond existing Serilog setup
- API endpoints are automatically available once deployed

### Frontend Deployment
- Global error handler is automatically initialized on app startup
- Service is registered in `Program.cs`
- No additional JavaScript libraries required

### Testing
- Use developer UI at `/superadmin/client-logs` to test functionality
- Generate test logs to verify end-to-end flow
- Monitor server logs to confirm client logs are being received

## Troubleshooting

### Common Issues

1. **Logs not appearing on server** - FIXED in latest version
   - **ISSUE**: The `ClientLogsController` had `[Authorize]` attribute requiring authentication
   - **FIX**: Changed to `[AllowAnonymous]` to allow logging without authentication
   - Check network connectivity
   - Verify API endpoints are accessible at `/api/ClientLogs`
   - Check server logs for any errors in `ClientLogsController`
   - Verify client is using correct base URL in `Program.cs`

2. **Local storage filling up**
   - Logs are automatically limited to 1000 entries
   - Use developer UI to clear local storage manually
   - Check if flush to server is working correctly

3. **Authentication context not captured**
   - Client logs work with or without authentication
   - Unauthenticated logs have no UserId/UserName
   - Authenticated logs capture user context automatically
   - Check CORS configuration if logs fail to send

### Debug Steps
1. Open browser developer tools (F12)
2. Navigate to `/superadmin/client-logs`
3. Generate test logs using the test feature
4. Verify logs appear in local storage
5. Manually flush logs to server
6. Check browser Network tab for requests to `/api/ClientLogs`
7. Verify server logs show received client logs
6. Check server logs for received client logs

## Future Enhancements

### Planned Features
- Enhanced rate limiting with user-specific quotas
- Real-time log streaming via SignalR
- Client-side log filtering and sampling
- Integration with external monitoring services
- Advanced correlation across client and server logs

### Configuration Enhancements
- Configurable log levels per environment
- Dynamic configuration updates
- Client-side log routing based on log level
- Compression for large log batches

This implementation provides a robust, scalable foundation for centralized client logging while maintaining minimal impact on existing infrastructure.