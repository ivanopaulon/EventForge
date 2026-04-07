using System.Net.Http.Headers;
using System.Text;
using Prym.Agent.Security;

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
        // these are trusted internal calls from the co-located Prym.Server
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
                "The Agent UI is disabled. Set UpdateAgent:UI:Username and UpdateAgent:UI:Password in appsettings.json.");
            return;
        }

        if (!TryAuthenticate(context, username, password))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Prym UpdateAgent\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static bool TryAuthenticate(HttpContext context, string expectedUser, string expectedPass)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var headerValue))
            return false;

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(headerValue!);
            if (!string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
                return false;

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2) return false;

            return credentials[0] == expectedUser &&
                   PasswordHasher.Verify(credentials[1], expectedPass);
        }
        catch
        {
            return false;
        }
    }
}
