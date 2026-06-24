using Prym.UpdateShared.Auth;
using System.Security.Cryptography;
using System.Text;

namespace Prym.Agent.Middleware;

/// <summary>
/// HTTP Basic Authentication middleware for the local Agent web UI.
/// Credentials are read from <see cref="AgentOptions.UI"/> on every request so changes
/// made via the Settings page take effect immediately without a restart.
/// Returns 503 if both username and password are empty (UI intentionally disabled).
/// Returns 401 + WWW-Authenticate if the request is missing or has wrong credentials.
/// <para>
/// Internal queue-management endpoints (<c>/api/agent/pending-installs</c>,
/// <c>/api/agent/install-now</c>, <c>/api/agent/unblock-queue</c>) require a matching
/// <c>X-Agent-Internal-Token</c> header when <see cref="AgentOptions.InternalApiToken"/>
/// is configured. When the token is left empty, the legacy localhost-only trust model applies.
/// </para>
/// </summary>
public class BasicAuthMiddleware(RequestDelegate next, AgentOptions options)
{
    // Header name used by EventForge.Server to authenticate internal Agent calls.
    private const string InternalTokenHeader = "X-Agent-Internal-Token";

    // Paths that bypass UI Basic Auth entirely (static assets, health probe).
    private static readonly string[] _staticPrefixes = ["/_", "/css/", "/js/", "/images/"];

    // Paths that are internal-only — validated via InternalApiToken, not Basic Auth.
    private static readonly string[] _internalPaths =
    [
        "/api/agent/pending-installs",
        "/api/agent/install-now",
        "/api/agent/unblock-queue"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for static files (CSS, JS, images) to avoid redirect loops.
        if (_staticPrefixes.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
        {
            await next(context);
            return;
        }

        // Unauthenticated health probe — used by EventForge.Server to include
        // Agent status in its own /health response. Safe because the Agent binds
        // to localhost only.
        if (path.Equals("/api/agent/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Internal queue-management endpoints called by EventForge.Server.
        // When InternalApiToken is configured: require a matching X-Agent-Internal-Token header.
        // When InternalApiToken is empty: fall back to legacy unauthenticated localhost trust.
        if (_internalPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            var configuredToken = options.InternalApiToken;
            if (!string.IsNullOrWhiteSpace(configuredToken))
            {
                var suppliedToken = context.Request.Headers[InternalTokenHeader].ToString();
                var configuredBytes = Encoding.UTF8.GetBytes(configuredToken);
                var suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);
                var tokenOk = suppliedBytes.Length == configuredBytes.Length
                    && CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
                if (!tokenOk)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
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
