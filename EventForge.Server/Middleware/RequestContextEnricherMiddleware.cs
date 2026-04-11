using Serilog.Context;
using System.Security.Claims;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware that pushes per-request context properties into Serilog's LogContext so that
/// dedicated SQL columns (CorrelationId, UserId, UserName, TenantId, RemoteIpAddress,
/// RequestPath) are populated automatically for every log entry emitted during the request,
/// without requiring each individual logger to include the values in its message template.
/// Must be registered after UseAuthentication so that JWT claims are available.
/// </summary>
public class RequestContextEnricherMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid)
            ? cid?.ToString()
            : null;

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var path = context.Request.Path.Value;

        string? userName = null;
        string? userId = null;
        string? tenantId = null;

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            userName = context.User.FindFirst(ClaimTypes.Name)?.Value
                       ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
            userId   = context.User.FindFirst("user_id")?.Value
                       ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            tenantId = context.User.FindFirst("tenant_id")?.Value;
        }

        using (LogContext.PushProperty("CorrelationId",   correlationId))
        using (LogContext.PushProperty("UserId",          userId))
        using (LogContext.PushProperty("UserName",        userName))
        using (LogContext.PushProperty("TenantId",        tenantId))
        using (LogContext.PushProperty("RemoteIpAddress", remoteIp))
        using (LogContext.PushProperty("RequestPath",     path))
        {
            await next(context);
        }
    }
}

/// <summary>Extension to register RequestContextEnricherMiddleware.</summary>
public static class RequestContextEnricherMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextEnricher(this IApplicationBuilder app)
        => app.UseMiddleware<RequestContextEnricherMiddleware>();
}
