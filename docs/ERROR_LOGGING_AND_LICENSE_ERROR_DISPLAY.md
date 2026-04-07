# Error Logging and License Error Display Implementation

## Overview

This document describes the implementation of comprehensive error logging and user-friendly error display for browser errors and license-related errors in EventForge.

## Problem Statement

Previously, the application had the following issues:
1. Browser errors were not being logged to the database via Serilog
2. License-related errors (403 Forbidden, 429 Too Many Requests) were not properly displayed to users
3. JavaScript errors were caught but not shown to users
4. HTTP errors needed better tracking and user feedback

## Implementation

### 1. Server-Side: ClientLogsController

**File**: `EventForge.Server/Controllers/ClientLogsController.cs`

A new controller was created to receive client-side logs and forward them to the Serilog infrastructure:

- **Endpoint**: `POST /api/ClientLogs` - Single log entry
- **Endpoint**: `POST /api/ClientLogs/batch` - Batch of up to 100 log entries
- **Features**:
  - Validates incoming log entries
  - Enriches logs with HTTP context (IP address, request path)
  - Adds user information (username, user ID)
  - Logs to Serilog with appropriate severity levels
  - Returns 202 Accepted for successful processing

**Log Properties Added**:
```json
{
  "Source": "Client",
  "Page": "/current-page",
  "UserAgent": "browser-info",
  "ClientTimestamp": "2024-01-01T12:00:00Z",
  "CorrelationId": "correlation-id",
  "Category": "logger-category",
  "RemoteIpAddress": "client-ip",
  "RequestPath": "/api/ClientLogs",
  "UserName": "username",
  "UserId": "user-guid",
  "ClientProperties": "{custom-properties-json}"
}
```

### 2. Client-Side: Enhanced HttpClientService

**File**: `EventForge.Client/Services/HttpClientService.cs`

Enhanced the HTTP client service to:

1. **Inject Dependencies**:
   - `IClientLogService` - For logging errors
   - `ISnackbar` - For displaying user notifications

2. **Enhanced Error Handling**:
   - Parses `ProblemDetails` responses from the server
   - Shows user-friendly Snackbar notifications for critical errors (403, 429, 401)
   - Logs all HTTP errors to the client logging service
   - Provides specific Italian error messages based on status codes:
     - 401: "Non autorizzato. Effettua l'accesso e riprova."
     - 403: "Non hai i permessi necessari per questa operazione..."
     - 404: "La risorsa richiesta non è stata trovata."
     - 429: "Limite di chiamate API superato..."
     - 500: "Errore interno del server. Riprova più tardi."

3. **Error Logging**:
   - All HTTP errors are logged with structured properties:
     - Endpoint
     - Status code
     - ProblemDetails (if available)

### 3. Client-Side: JavaScript Error Handler

**File**: `EventForge.Client/Shared/JavaScriptErrorHelper.cs`

Updated to:
- Display JavaScript errors to users via Snackbar
- Show Italian message: "Si è verificato un errore nell'applicazione. L'errore è stato registrato."
- Continue logging errors to the server

**File**: `EventForge.Client/Shared/GlobalErrorHandler.razor`

Enhanced to:
- Prevent default browser error handling for unhandled promises
- Return `true` from error handler to suppress browser error console

### 4. Server-Side: License Error Messages

**File**: `EventForge.Server/Filters/RequireLicenseFeatureAttribute.cs`

Completely refactored to return proper `ProblemDetails` responses instead of plain text:

#### No Active License
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Nessuna licenza attiva trovata per il tenant. Contatta l'amministratore.",
  "instance": "/api/endpoint"
}
```

#### Expired License
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "La licenza è scaduta o non ancora attiva. Contatta l'amministratore per rinnovarla.",
  "instance": "/api/endpoint"
}
```

#### Feature Not Available
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "La funzionalità 'FeatureName' non è disponibile nella licenza corrente. Aggiorna la licenza per accedere a questa funzionalità.",
  "instance": "/api/endpoint"
}
```

#### API Limit Exceeded
```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Limite mensile di chiamate API superato (1000). Attendi il prossimo mese o aggiorna la licenza.",
  "instance": "/api/endpoint",
  "currentUsage": 1001,
  "limit": 1000,
  "resetDate": "2024-02-01"
}
```

#### Missing Permissions
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Permessi mancanti per la funzionalità 'FeatureName': Permission1, Permission2. Contatta l'amministratore per ottenere i permessi necessari.",
  "instance": "/api/endpoint",
  "missingPermissions": ["Permission1", "Permission2"],
  "featureName": "FeatureName"
}
```

### 5. Client-Side: UI Component Error Handling

**File**: `EventForge.Client/Shared/Components/LicenseDrawer.razor`

Enhanced to handle specific HTTP error codes:

```csharp
catch (HttpRequestException httpEx) when (httpEx.StatusCode == HttpStatusCode.Forbidden)
{
    Logger.LogError(httpEx, "Forbidden error saving license");
    Snackbar.Add(
        "Non hai i permessi necessari per questa operazione",
        Severity.Error,
        config => config.VisibleStateDuration = 5000
    );
}
catch (HttpRequestException httpEx) when (httpEx.StatusCode == (HttpStatusCode)429)
{
    Logger.LogError(httpEx, "API limit exceeded saving license");
    Snackbar.Add(
        "Limite di chiamate API superato. Riprova più tardi.",
        Severity.Error,
        config => config.VisibleStateDuration = 5000
    );
}
```

## User Experience Improvements

### Before
- Errors appeared only in browser console
- No feedback to users about what went wrong
- License errors showed generic messages or status codes
- No central logging of client-side errors

### After
- All errors are logged to the database via Serilog
- Users see friendly Italian error messages via Snackbar
- License errors provide specific guidance on how to resolve the issue
- Structured error information helps debugging
- Error notifications are non-intrusive and dismissible

## Testing

All existing tests pass (94/94):
```bash
Passed!  - Failed:     0, Passed:    94, Skipped:     0, Total:    94
```

## Error Flow

```
┌─────────────────┐
│  Browser Error  │
└────────┬────────┘
         │
         ▼
┌──────────────────────┐
│ GlobalErrorHandler   │
│ - Catches error      │
│ - Shows Snackbar     │
│ - Logs to service    │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ ClientLogService     │
│ - Enriches log       │
│ - Buffers locally    │
│ - Sends to server    │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ ClientLogsController │
│ - Validates log      │
│ - Enriches context   │
│ - Logs via Serilog   │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ Serilog → Database   │
└──────────────────────┘
```

## Configuration

No configuration changes required. The implementation:
- Uses existing Serilog infrastructure
- Uses existing authentication and authorization
- Works with existing client logging service
- Requires no database migrations

## Deployment Notes

1. **Backend**: Deploy server changes first to ensure API endpoints are available
2. **Frontend**: Deploy client changes after server deployment
3. **No breaking changes**: All changes are backward compatible
4. **Immediate effect**: Error logging and display work immediately upon deployment

## Monitoring

After deployment, you can:
1. Check Serilog logs for client-side errors (filter by `Source = "Client"`)
2. Monitor API usage and license violations
3. Track user-facing errors via error severity and frequency
4. Use correlation IDs to trace errors across client and server

## Future Enhancements

Potential improvements:
1. Add retry logic for failed log submissions
2. Implement error aggregation and deduplication
3. Add user feedback mechanism for errors
4. Create dashboard for error monitoring
5. Add automated alerts for critical errors
