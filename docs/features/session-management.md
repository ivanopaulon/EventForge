# Session Management

## Overview

EventForge implements a **sliding expiration** strategy for user sessions. As long as the user is active (navigating pages or the background keepalive is running), the session never expires. A session is only terminated by an explicit logout or after **10 hours of true inactivity**.

---

## Session Strategy: Sliding Expiration via JWT Token Refresh

The session is maintained through periodic JWT token refresh. Every time the token is refreshed, its expiry is extended by the configured `ExpirationMinutes` from the current moment — effectively "sliding" the expiration window forward on each active use.

---

## How It Works

### Background Keepalive (`SessionKeepaliveService`)

`EventForge.Client/Services/SessionKeepaliveService.cs` runs a background timer that:

1. Fires every **3 minutes** (`KEEPALIVE_INTERVAL_MINUTES = 3`).
2. Calls `IAuthService.RefreshTokenAsync()` to obtain a new JWT token.
3. On success, resets the consecutive-failure counter and emits `OnRefreshSuccess`.
4. On failure, retries up to **3 times** with exponential backoff (1 s, 2 s).
5. After all retries are exhausted, increments the consecutive-failure counter, emits `OnRefreshFailure`, and — if failures reach 5 or more — logs critically **without stopping the service** so that recovery is possible on the next timer tick.
6. If the token is within `WARNING_THRESHOLD_MINUTES` (15 minutes) of expiry and a refresh cycle has failed, emits `OnSessionWarning` with the minutes remaining.

### Navigation Refresh (`MainLayout.OnLocationChanged`)

`EventForge.Client/Layout/MainLayout.razor` subscribes to `NavigationManager.LocationChanged` and calls `IAuthService.RefreshTokenAsync()` on every page navigation, providing an additional refresh trigger beyond the background timer.

---

## Configuration Parameters

All parameters live in `EventForge.Server/appsettings.json` and can be overridden via environment variables or `appsettings.overrides.json`.

| Parameter | Location | Default | Description |
|-----------|----------|---------|-------------|
| `ExpirationMinutes` | `Authentication:Jwt:ExpirationMinutes` | `600` | JWT lifetime in minutes (10 hours). Extended on every successful refresh. |
| `ClockSkewMinutes` | `Authentication:Jwt:ClockSkewMinutes` | `5` | Allowed clock skew between client and server when validating token expiry. |
| `TokenRefreshLimit` | `RateLimiting:TokenRefreshLimit` | `30` | Maximum token refresh requests per rate-limiting window. Must be high enough to allow the keepalive interval plus retry attempts without throttling. |

### Why `TokenRefreshLimit` must be high enough

The keepalive fires every 3 minutes with up to 3 retries each cycle. In a worst-case burst scenario (e.g., server briefly unresponsive), multiple cycles can overlap. A value of `30` provides enough headroom for normal operation plus transient error spikes without blocking legitimate refresh calls.

Setting `TokenRefreshLimit: 1` (the previous default) caused all retry attempts to be rate-limited, leading to consecutive failures that permanently stopped the keepalive service and caused sessions to expire after 240 minutes.

---

## Session Warning

When token refresh fails and the token is within **15 minutes** of expiry, `SessionKeepaliveService` emits `OnSessionWarning(minutesRemaining)`.

`MainLayout.razor` handles this event and shows a **MudBlazor Snackbar warning** when `minutesRemaining <= 10`:

- **Message** (Italian/English): *"Sessione in scadenza tra N minuti. Salva il lavoro in corso. (Session expiring in N minutes. Please save your work.)"*
- **Duration**: 8 000 ms (`VisibleStateDuration = 8000`)
- **Severity**: `Warning`
- **Close icon**: visible

---

## Session Expiry

A session expires only when **all** of the following are true simultaneously:

1. The user has not navigated to any page (no `OnLocationChanged` refresh).
2. The `SessionKeepaliveService` background timer has been unable to refresh the token for the full token lifetime.
3. The JWT token has passed its expiry timestamp plus the configured `ClockSkewMinutes`.

With `ExpirationMinutes = 600`, this means true inactivity of **10 hours** is required for a session to expire.

---

## Explicit Logout

Users can log out at any time via the **UserAccountMenu** component in the application header. Logout invalidates the current token server-side and redirects to the login page.

---

## Troubleshooting

### Sessions expire after a few hours despite active use

**Symptom**: Users are unexpectedly logged out even while using the application.

**Cause**: `TokenRefreshLimit` is set too low (e.g., `1`), causing the keepalive service's refresh calls and their retries to be rate-limited. After 5 consecutive failures the service previously stopped itself permanently. The session then expired at the original token lifetime.

**Fix**: Ensure `RateLimiting:TokenRefreshLimit` is set to at least `10` (recommended: `30`) in `appsettings.json`.

### Session warning appears unexpectedly

**Symptom**: The "session expiring" snackbar appears even though the user has been active.

**Cause**: Token refresh is failing silently (network issue, server restart, rate limiting). Check server logs for `401 Unauthorized` or `429 Too Many Requests` responses to `/api/v1/auth/refresh-token`.

**Fix**: Investigate the root cause of the refresh failures. Increase `TokenRefreshLimit` if rate limiting is the issue.
