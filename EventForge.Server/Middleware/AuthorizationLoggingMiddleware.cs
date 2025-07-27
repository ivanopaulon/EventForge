using System.Security.Claims;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware to log authorization failures for debugging.
/// </summary>
public class AuthorizationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

    public AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store the original response to check for 403 status
        var originalBodyStream = context.Response.Body;
        
        await _next(context);
        
        // Check if the response is 403 Forbidden
        if (context.Response.StatusCode == 403)
        {
            LogAuthorizationFailure(context);
        }
    }

    private void LogAuthorizationFailure(HttpContext context)
    {
        var user = context.User;
        var path = context.Request.Path;
        var method = context.Request.Method;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("user_id")?.Value;
            var username = user.FindFirst(ClaimTypes.Name)?.Value;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var permissions = user.FindAll("permission").Select(c => c.Value).ToArray();
            
            _logger.LogWarning(
                "Authorization failed for authenticated user. " +
                "User: {Username} (ID: {UserId}), " +
                "Roles: [{Roles}], " +
                "Permissions: [{Permissions}], " +
                "Request: {Method} {Path}",
                username, userId, 
                string.Join(", ", roles), 
                string.Join(", ", permissions),
                method, path);
        }
        else
        {
            _logger.LogWarning(
                "Authorization failed for unauthenticated user. " +
                "Request: {Method} {Path}",
                method, path);
        }
    }
}

/// <summary>
/// Extension methods for registering the authorization logging middleware.
/// </summary>
public static class AuthorizationLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationLoggingMiddleware>();
    }
}