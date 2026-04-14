using System.Text;
using Prym.ManagementHub.Configuration;
using Prym.UpdateShared.Auth;

namespace Prym.ManagementHub.Auth;

/// <summary>
/// HTTP Basic Authentication middleware for the local Hub web UI (Razor Pages).
/// Credentials are read from <see cref="ManagementHubOptions.UI"/> on every request so changes
/// made via the Settings page take effect immediately without a restart.
/// Returns 503 if both username and password are empty (UI auth intentionally disabled).
/// Returns 401 + WWW-Authenticate if the request is missing or has wrong credentials.
/// Agent/admin API endpoints (/api/*, /hubs/*) are NOT affected by this middleware —
/// they are protected separately by <see cref="ApiKeyAuthMiddleware"/> and admin API key validation.
/// </summary>
public class HubBasicAuthMiddleware(RequestDelegate next, ManagementHubOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for API endpoints (protected by their own mechanisms)
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Skip auth for static files to avoid redirect loops
        if (path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/css/", StringComparison.Ordinal) ||
            path.StartsWith("/js/", StringComparison.Ordinal) ||
            path.StartsWith("/images/", StringComparison.Ordinal) ||
            path.StartsWith("/lib/", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var username = options.UI.Username;
        var password = options.UI.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "The Hub UI is disabled. Set ManagementHub:UI:Username and ManagementHub:UI:Password in appsettings.json.");
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!BasicAuthHelper.TryAuthenticate(authHeader, username, password))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Prym ManagementHub\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
