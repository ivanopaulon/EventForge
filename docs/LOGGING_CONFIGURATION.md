# Logging Configuration

## Overview

EventForge uses Serilog for centralized logging of both server-side and client-side logs.

## Server-Side Logging

Server logs are configured in `ServiceCollectionExtensions.cs` using the `AddCustomSerilogLogging` method.

### Configuration Details

- **Primary Sink**: Microsoft SQL Server (LogDb connection string)
- **Fallback Sink**: File logging (if database is unavailable)
- **Table Name**: `Logs`
- **Auto-Create Table**: Yes
- **Enrichment**: FromLogContext enabled to capture scope properties
- **Console Output**: Enabled for development debugging

The configuration uses the `LogDb` connection string from `appsettings.json`:

```json
"ConnectionStrings": {
  "LogDb": "Server=localhost\\SQLEXPRESS;Database=EventLogger;User Id=vsapp;Password=pass123!;TrustServerCertificate=True;",
  ...
}
```

**Note**: The `Serilog` section in `appsettings.json` is NOT used for configuration. It exists only as a reference. The actual configuration is done programmatically in `ServiceCollectionExtensions.cs`.

### Database Schema

The `Logs` table includes the following custom columns for client log enrichment:

| Column Name | Data Type | Description |
|-------------|-----------|-------------|
| `Source` | nvarchar(50) | Identifies if log is from "Client" or server |
| `Page` | nvarchar(500) | Page/component where log was generated |
| `UserAgent` | nvarchar(500) | Client browser/device information |
| `ClientTimestamp` | datetimeoffset | When log was created on client |
| `CorrelationId` | nvarchar(50) | For tracing related logs |
| `Category` | nvarchar(100) | Log category |
| `UserId` | uniqueidentifier | User identifier |
| `UserName` | nvarchar(100) | Username |
| `RemoteIpAddress` | nvarchar(50) | Client IP address |
| `RequestPath` | nvarchar(500) | API endpoint path |
| `ClientProperties` | nvarchar(max) | Additional custom properties as JSON |

These columns are automatically created when Serilog creates the table or can be added to an existing table.

## Client-Side Logging

Client logs are sent to the server via the `/api/ClientLogs` endpoint, which is handled by `ClientLogsController`.

### How It Works

1. Client-side components use `IClientLogService` to log events
2. Logs are batched and sent to the server via HTTP POST
3. `ClientLogsController` receives the logs and logs them using the server's `ILogger`
4. The server's `ILogger` is configured to use Serilog
5. Serilog writes the logs to the same `LogDb` database

### Key Points

- **Both client and server logs go to the same database (LogDb)**
- Client logs are enriched with additional context (Source: "Client", Page, UserAgent, etc.)
- Logs are batched for efficiency
- Offline support: logs are stored in browser localStorage if the server is unavailable

## Usage in Components

### Client-Side Example

```razor
@inject IClientLogService ClientLogService

// Information log
await ClientLogService.LogInformationAsync("User action performed", "ComponentName");

// Error log with exception
catch (Exception ex)
{
    await ClientLogService.LogErrorAsync("Operation failed", ex, "ComponentName");
}
```

### Server-Side Example

```csharp
private readonly ILogger<MyService> _logger;

// Log with structured data
_logger.LogInformation("Product created with ID {ProductId} by user {User}", product.Id, currentUser);

// Log error with exception
_logger.LogError(ex, "Error creating product for user {User}", currentUser);
```

## Debugging Client-Side Issues

To debug issues in client-side components like `CreateProductDialog`:

1. Inject `IClientLogService` into the component
2. Add log statements at key points (initialization, data loading, save operations)
3. Use different log levels (Debug, Information, Warning, Error, Critical)
4. Check the `LogDb` database for the logs
5. Look for logs with `Source = "Client"` and the component's category

Example from `CreateProductDialog.razor`:

```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        await ClientLogService.LogInformationAsync("CreateProductDialog: Opening dialog", "CreateProductDialog");
        // ... component initialization ...
        await ClientLogService.LogInformationAsync("CreateProductDialog: Dialog opened successfully", "CreateProductDialog");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error initializing CreateProductDialog");
        await ClientLogService.LogErrorAsync("CreateProductDialog: Error during initialization", ex, "CreateProductDialog");
        throw;
    }
}
```

## Viewing Logs

### Database Query

```sql
-- View all client logs from the last hour with enriched properties
SELECT 
    [TimeStamp],
    [Level],
    [Message],
    [Exception],
    [Source],
    [Page],
    [Category],
    [UserName],
    [RemoteIpAddress],
    [CorrelationId],
    [Properties]
FROM [EventLogger].[dbo].[Logs]
WHERE 
    [Source] = 'Client'
    AND [TimeStamp] > DATEADD(hour, -1, GETDATE())
ORDER BY [TimeStamp] DESC;

-- View all logs (client and server) with correlation
SELECT 
    [TimeStamp],
    [Level],
    [Message],
    [Source],
    [UserName],
    [Category],
    [CorrelationId]
FROM [EventLogger].[dbo].[Logs]
WHERE 
    [TimeStamp] > DATEADD(hour, -1, GETDATE())
ORDER BY [TimeStamp] DESC;
```

### Client Log Management UI

Navigate to `/superadmin/client-logs` to view and manage client logs directly from the UI.

## Troubleshooting

### Logs Not Appearing in Database

If logs are not appearing in the database:

1. **Check database connection**: Verify the `LogDb` connection string in `appsettings.json` is correct
2. **Check table exists**: Run `SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Logs'` to verify the table was created
3. **Check console output**: In development, logs should appear in the console even if database logging fails
4. **Check fallback logs**: If database is unavailable, check the `Logs/fallback-log-*.log` files
5. **Verify Serilog configuration**: Look for "Serilog configurato per SQL Server con enrichment" message at startup

### Client Logs Not Reaching Server

If client logs are not being sent to the server:

1. **Check authentication**: Client must be authenticated to send logs (endpoint has `[Authorize]` attribute)
2. **Check network**: Open browser DevTools Network tab and look for POST requests to `/api/ClientLogs`
3. **Check localStorage**: Client logs are buffered in localStorage if server is unavailable
4. **Check ClientLogService**: Verify `IClientLogService` is properly injected in client components

### Enriched Properties Not Captured

If enriched properties (Source, Page, etc.) are not appearing in the database:

1. **Verify Serilog version**: Requires `Serilog.AspNetCore` 9.0.0+ and `Serilog.Sinks.MSSqlServer` 8.2.2+
2. **Check column configuration**: Custom columns should be auto-created by Serilog
3. **Manually add columns**: If upgrading from old version, you may need to add columns manually:

```sql
-- Add missing columns to existing Logs table
USE EventLogger;
GO

ALTER TABLE Logs ADD [Source] nvarchar(50) NULL;
ALTER TABLE Logs ADD [Page] nvarchar(500) NULL;
ALTER TABLE Logs ADD [UserAgent] nvarchar(500) NULL;
ALTER TABLE Logs ADD [ClientTimestamp] datetimeoffset NULL;
ALTER TABLE Logs ADD [CorrelationId] nvarchar(50) NULL;
ALTER TABLE Logs ADD [Category] nvarchar(100) NULL;
ALTER TABLE Logs ADD [UserId] uniqueidentifier NULL;
ALTER TABLE Logs ADD [UserName] nvarchar(100) NULL;
ALTER TABLE Logs ADD [RemoteIpAddress] nvarchar(50) NULL;
ALTER TABLE Logs ADD [RequestPath] nvarchar(500) NULL;
ALTER TABLE Logs ADD [ClientProperties] nvarchar(max) NULL;
GO
```

### MockLogger in Tests

The `MockLogger` class found in test files is intentional and only used for unit testing. It does NOT affect production logging. Production code uses the real `ILogger<T>` implementation provided by ASP.NET Core's dependency injection with Serilog as the logging provider.
