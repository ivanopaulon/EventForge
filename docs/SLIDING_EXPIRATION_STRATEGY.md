# Sliding Expiration Strategy

## Overview

EventForge implements a **true sliding expiration** pattern for JWT authentication, ensuring users never have to re-login during active work sessions.

## How It Works

### Token Refresh Triggers

The JWT token is automatically refreshed in the following scenarios:

1. **Every 3 minutes** (SessionKeepaliveService background timer)
2. **On page navigation** (MainLayout.OnLocationChanged)
3. **On API calls** (HttpClient interceptor - if implemented)

### Token Lifetime

- **Initial Login**: Token valid for 240 minutes (4 hours)
- **Every Refresh**: New token valid for 240 minutes from refresh time
- **Effective Lifetime**: Unlimited, as long as user is active

### Session Expiration

A session expires ONLY when:
- User is inactive (no navigation, no actions) for 4 consecutive hours
- SessionKeepaliveService fails to refresh 5 consecutive times
- User explicitly logs out

### Benefits

✅ **No Interruptions**: Users can work continuously without re-authentication  
✅ **True Sliding**: Each activity extends the session indefinitely  
✅ **Safety Buffer**: 4-hour expiration protects against keepalive failures  
✅ **Enterprise-Ready**: Matches user expectations for production applications  

### Security Considerations

- Token refresh requires valid authentication (can't refresh expired token)
- All refresh operations are logged for audit trail
- Failed refresh attempts trigger warnings and eventual service shutdown
- Session timeout on server side (4 hours) provides additional protection

## Configuration

### Server Configuration

```json
// appsettings.json
"Authentication": {
  "Jwt": {
    "ExpirationMinutes": 240  // 4 hours
  }
}
```

### Client Configuration

```csharp
// SessionKeepaliveService.cs
private const int KEEPALIVE_INTERVAL_MINUTES = 3;  // Refresh every 3 minutes
```

## Monitoring

Check browser console logs for:
- `Token refreshed successfully` - Normal operation
- `Sliding expiration refresh` - Indicates refresh trigger
- `Token refresh failed` - May require attention

## Troubleshooting

**Issue**: Users still being logged out during work  
**Solution**: Check browser console for failed refresh attempts. Verify server `/api/v1/auth/refresh-token` endpoint is responding.

**Issue**: Too many refresh requests  
**Solution**: Verify KEEPALIVE_INTERVAL_MINUTES is set to appropriate value (3-5 minutes recommended).
