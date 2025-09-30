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

The configuration uses the `LogDb` connection string from `appsettings.json`:

```json
"ConnectionStrings": {
  "LogDb": "Server=localhost\\SQLEXPRESS;Database=EventLogger;User Id=vsapp;Password=pass123!;TrustServerCertificate=True;",
  ...
}
```

**Note**: The `Serilog` section in `appsettings.json` is NOT used for configuration. It exists only as a reference. The actual configuration is done programmatically in `ServiceCollectionExtensions.cs`.

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
SELECT 
    [TimeStamp],
    [Level],
    [Message],
    [Exception],
    [Properties]
FROM [EventLogger].[dbo].[Logs]
WHERE 
    [Properties] LIKE '%"Source":"Client"%'
    AND [TimeStamp] > DATEADD(hour, -1, GETDATE())
ORDER BY [TimeStamp] DESC;
```

### Client Log Management UI

Navigate to `/superadmin/client-logs` to view and manage client logs directly from the UI.
