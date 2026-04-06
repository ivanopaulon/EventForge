using System.Net.Http.Headers;
using System.Text;

namespace EventForge.UpdateAgent.Middleware;

/// <summary>
/// HTTP Basic Authentication middleware for the local Agent web UI.
/// Returns 503 if credentials are not configured (UI intentionally disabled).
/// Returns 401 + WWW-Authenticate if the request is missing or has wrong credentials.
/// </summary>
public class BasicAuthMiddleware(RequestDelegate next, AgentOptions options)
{
    private readonly bool _enabled =
        !string.IsNullOrWhiteSpace(options.UI.Username) &&
        !string.IsNullOrWhiteSpace(options.UI.Password);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(
                "The Agent UI is disabled. Configure UpdateAgent:UI:Username and UpdateAgent:UI:Password in appsettings.json.");
            return;
        }

        if (!TryAuthenticate(context))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"EventForge UpdateAgent\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private bool TryAuthenticate(HttpContext context)
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

            return credentials[0] == options.UI.Username &&
                   credentials[1] == options.UI.Password;
        }
        catch
        {
            return false;
        }
    }
}
