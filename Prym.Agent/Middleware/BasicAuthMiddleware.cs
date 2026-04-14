using System.Text;
using Prym.UpdateShared.Auth;

namespace Prym.Agent.Middleware;

/// <summary>
/// HTTP Basic Authentication middleware for the local Agent web UI.
/// Credentials are read from <see cref="AgentOptions.UI"/> on every request so changes
/// made via the Settings page take effect immediately without a restart.
/// Returns 503 if both username and password are empty (UI intentionally disabled).
/// Returns 401 + WWW-Authenticate if the request is missing or has wrong credentials.
/// </summary>
public class BasicAuthMiddleware(RequestDelegate next, AgentOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for static files (CSS, JS, images) to avoid redirect loops.
        // Also skip for /api/agent/health and the update-queue management endpoints:
        // these are trusted internal calls from the co-located EventForge.Server
        // (same localhost-only trust model as health). No external access is possible.
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/css/", StringComparison.Ordinal) ||
            path.StartsWith("/js/", StringComparison.Ordinal) ||
            path.StartsWith("/images/", StringComparison.Ordinal) ||
            path.Equals("/api/agent/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/agent/pending-installs", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/agent/install-now", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/agent/unblock-queue", StringComparison.OrdinalIgnoreCase))
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
                "The Agent UI is disabled. Set PrymAgent:UI:Username and PrymAgent:UI:Password in appsettings.json.");
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!BasicAuthHelper.TryAuthenticate(authHeader, username, password))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Prym Agent\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
