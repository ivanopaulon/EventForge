# HealthStatusDialog Enhancement Documentation

## Overview

The HealthStatusDialog has been completely refactored to provide true fullscreen parity with AuditHistoryDialog, implement public read-only log access with sanitization, and introduce an auto-refresh feature. This document describes the new features, security considerations, and usage guidelines.

## Key Enhancements

### 1. Full

screen Pattern Matching AuditHistoryDialog

The dialog now follows the established fullscreen pattern used throughout EventForge:

- **MudAppBar**: Replaces the simple title content with a full application bar
- **Action Buttons**: Refresh, export, and close buttons in the app bar
- **Collapsible Filters**: Filters are in a collapsible MudPaper with expand/collapse functionality
- **Responsive Grid**: Consistent responsive layout with proper spacing
- **Accessibility**: All interactive elements have proper aria-labels

### 2. Public Log Access with Sanitization

#### For All Authenticated Users

Non-admin users can now view system logs with sensitive information automatically masked:

- **Endpoint**: `GET /api/v1/LogManagement/logs/public`
- **Authentication**: Required (any authenticated user)
- **Authorization**: No role restriction

#### Sanitization Features

The `LogSanitizationService` automatically masks:

- **IP Addresses**: `192.168.1.1` → `***.***.***.**`
- **GUIDs**: `550e8400-e29b-41d4-a716-446655440000` → `********-****-****-****-************`
- **Email Addresses**: `user@example.com` → `***@***.***`
- **Tokens**: Long alphanumeric strings → `***TOKEN***`
- **File Paths**: `C:\Windows\System32` → `[PATH]`
- **Sensitive Properties**: Password, secret, token, API keys, session IDs, etc.
- **Exception Details**: Hidden (only indicates exception occurred)

#### Admin Access

Admin and SuperAdmin users continue to have full access to unsanitized logs:

- **Endpoint**: `GET /api/v1/LogManagement/logs` (existing)
- **Authorization**: SuperAdmin or Admin roles required
- **Data**: Complete log information including exception details, IPs, user IDs

### 3. Auto-Refresh Feature

#### Configuration

- **Toggle Switch**: Enable/disable auto-refresh in the app bar
- **Interval Setting**: Configure refresh interval (5-300 seconds)
- **Default**: Disabled (30 seconds when enabled)

#### Behavior

When enabled, the dialog automatically:
1. Refreshes health status data
2. Reloads log entries
3. Updates the "Last Updated" timestamp
4. Maintains current filter settings
5. Keeps the current page position

#### Resource Management

- Uses `System.Timers.Timer` for periodic refresh
- Automatically disposes timer on dialog close
- Implements `IDisposable` for proper cleanup
- Timer stops when dialog is closed

### 4. Enhanced Filters

Filters are now collapsible and use a consistent grid layout:

- **Log Level**: Debug, Information, Warning, Error, Critical
- **Date Range**: From date and to date pickers
- **Text Search**: Search by log message content
- **Auto-Apply**: Filters apply immediately on change (debounced)
- **Clear Filters**: Reset all filters to default values

### 5. Detail Expansion Panels

#### Admin View
Shows complete log information:
- Timestamp (with milliseconds)
- Level (with color coding)
- Full message
- Source and category
- User information
- Complete exception stack trace
- All properties (as JSON)

#### Public View
Shows sanitized information:
- Timestamp (with milliseconds)
- Level (with color coding)
- Sanitized message
- Sanitized source and category
- Exception indicator (no details)
- Sanitized properties only

## Security Considerations

### Sanitization Strategy

The sanitization approach follows defense-in-depth principles:

1. **Pattern Matching**: Uses compiled regex patterns for efficient detection
2. **Whitelist Approach**: Only non-sensitive properties are included
3. **Truncation**: Long messages/values are truncated to prevent information leakage
4. **Exception Hiding**: Exception details are never exposed to non-admin users

### Threat Mitigation

| Threat | Mitigation |
|--------|------------|
| IP Address Exposure | Masked with `***.***.***.***` |
| User ID Leakage | GUIDs masked, usernames removed |
| Session Hijacking | Session IDs removed from properties |
| Token Theft | Tokens and keys masked/removed |
| Path Disclosure | File paths replaced with `[PATH]` |
| Stack Trace Analysis | Exceptions hidden from public view |

### Compliance

- **GDPR**: Personal data (emails, IPs) masked
- **Least Privilege**: Users only see what they need
- **Audit Trail**: All log access is logged server-side
- **Data Minimization**: Sensitive data removed, not just hidden

## Usage Guidelines

### For Administrators

1. **Full Access**: Admin users automatically see complete logs
2. **Export**: Use export button (when implemented) for bulk operations
3. **Auto-Refresh**: Recommended for monitoring production issues
4. **Filter Efficiently**: Use date ranges to limit results

### For Regular Users

1. **View Access**: All authenticated users can view sanitized logs
2. **Troubleshooting**: Use logs to understand application behavior
3. **Report Issues**: Reference timestamps when reporting problems
4. **Privacy**: Sensitive information is automatically protected

### Performance Tips

1. **Date Range**: Narrow date ranges improve query performance
2. **Filters**: Use specific filters to reduce result sets
3. **Auto-Refresh Interval**: Longer intervals (60+ seconds) for production
4. **Export**: For large datasets, use export feature instead of pagination

## API Endpoints

### Public Logs (Authenticated Users)

```http
GET /api/v1/LogManagement/logs/public
Authorization: Bearer {token}

Query Parameters:
- Level: string (optional) - Filter by log level
- FromDate: datetime (optional) - Start date filter
- ToDate: datetime (optional) - End date filter
- Message: string (optional) - Search in message
- Page: int (default: 1) - Page number
- PageSize: int (default: 20, max: 100) - Items per page
- SortBy: string (default: "Timestamp") - Sort field
- SortDirection: string (default: "desc") - Sort direction

Response: PagedResult<SanitizedSystemLogDto>
```

### Admin Logs (Admin/SuperAdmin Only)

```http
GET /api/v1/LogManagement/logs
Authorization: Bearer {token}
Roles: SuperAdmin, Admin

Query Parameters: (same as public endpoint)

Response: PagedResult<SystemLogDto>
```

## Configuration

### Client-Side

No configuration required. The dialog automatically detects user role and uses the appropriate endpoint.

### Server-Side

Ensure the `LogSanitizationService` is registered in DI:

```csharp
services.AddScoped<ILogSanitizationService, LogSanitizationService>();
```

## Translation Keys

### English (en.json)

All translation keys are in the `health` namespace:
- `health.autoRefresh`: "Auto-refresh"
- `health.intervalSeconds`: "Interval (s)"
- `health.logFilters`: "Log Filters"
- `health.systemLogs`: "System Logs"
- `health.export`: "Export"
- And many more...

### Italian (it.json)

Italian translations provided for all keys:
- `health.autoRefresh`: "Aggiornamento automatico"
- `health.intervalSeconds`: "Intervallo (s)"
- `health.logFilters`: "Filtri Log"
- `health.systemLogs`: "Log di Sistema"
- And many more...

## Future Enhancements

### Planned Features

1. **Export Implementation**: Export logs to CSV, JSON, or Excel
2. **Advanced Filters**: Filter by source, category, user
3. **Real-Time Updates**: SignalR integration for live log streaming
4. **Log Analytics**: Basic statistics and trend visualization
5. **Bookmarking**: Save filter configurations
6. **Alerts**: Notify users of critical logs

### Extensibility Points

The architecture supports easy extension:

- **Custom Sanitizers**: Implement `ILogSanitizationService` for custom rules
- **Export Formats**: Add new export formats via strategy pattern
- **Filter Persistence**: Store user filter preferences
- **Custom Properties**: Sanitization service handles new properties automatically

## Testing Recommendations

### Unit Tests

1. **Sanitization Service**: Test all regex patterns and edge cases
2. **Log Service**: Verify admin vs. public endpoint selection
3. **Timer Disposal**: Ensure no memory leaks from auto-refresh

### Integration Tests

1. **Role-Based Access**: Verify admin users get full logs
2. **Public Access**: Verify non-admin users get sanitized logs
3. **Pagination**: Test large result sets
4. **Filter Combinations**: Test various filter combinations

### UI Tests

1. **Auto-Refresh**: Verify timer triggers refresh correctly
2. **Filter Expansion**: Test collapsible filter functionality
3. **Detail Panels**: Verify correct panel displays based on user role
4. **Responsive Design**: Test on mobile, tablet, and desktop

## Troubleshooting

### Common Issues

**Issue**: Logs not loading
- **Solution**: Check authentication token, verify API endpoint availability

**Issue**: Auto-refresh not working
- **Solution**: Check browser console for timer errors, verify dialog is not closing

**Issue**: Filters not applying
- **Solution**: Ensure filter values are valid, check network tab for API calls

**Issue**: Export button disabled
- **Solution**: Export feature is currently a stub, implementation coming soon

## Support

For questions or issues:
1. Check EventForge documentation
2. Review API endpoint Swagger documentation
3. Contact development team
4. File an issue in the repository

## Changelog

### Version 1.0 (Current)
- Initial release with full-screen pattern
- Public log access with sanitization
- Auto-refresh feature
- Collapsible filters
- EN/IT translations
- Admin/non-admin role detection
- Detail expansion panels

