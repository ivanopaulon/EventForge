using System.Diagnostics;
using System.Security.Claims;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware for tracking request performance and logging slow requests
/// </summary>
public class PerformanceTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTelemetryMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;
    private readonly int _verySlowRequestThresholdMs;
    private readonly int _startupGracePeriodMs;
    private static readonly long _startupTickCount = Environment.TickCount64;

    public PerformanceTelemetryMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTelemetryMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = configuration.GetValue<int>("Performance:SlowRequestThresholdMs", 200);
        _verySlowRequestThresholdMs = configuration.GetValue<int>("Performance:VerySlowRequestThresholdMs", 2000);
        _startupGracePeriodMs = configuration.GetValue<int>("Performance:StartupGracePeriodMs", 60000);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Skip SignalR hubs: the middleware measures the entire WebSocket connection lifetime,
        // not just the HTTP handshake, producing false-positive slow-request alerts.
        if (path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        // Register callback to add header BEFORE response starts
        context.Response.OnStarting(() =>
        {
            var elapsed = sw.ElapsedMilliseconds;
            if (!context.Response.Headers.ContainsKey("X-Response-Time-Ms"))
            {
                context.Response.Headers["X-Response-Time-Ms"] = elapsed.ToString();
            }
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            // Skip slow-request alerts during startup grace period: the first requests after a cold
            // start are inherently slow (JIT, migrations, bootstrap) and would generate misleading
            // Error/Warning entries that obscure real performance issues.
            var isInGracePeriod = (Environment.TickCount64 - _startupTickCount) < _startupGracePeriodMs;

            // Log slow requests (can happen after response is sent)
            if (!isInGracePeriod && elapsed > _verySlowRequestThresholdMs)
            {
                var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : "N/A";
                var userName = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(ClaimTypes.Name)?.Value
                    : null;
                var userId   = context.User?.FindFirst("user_id")?.Value;
                var tenantId = context.User?.FindFirst("tenant_id")?.Value;

                // Very slow requests (>2000ms by default)
                _logger.LogError(
                    "Very Slow Request: {Method} {Path} took {Elapsed}ms - Status: {StatusCode} " +
                    "User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}",
                    method,
                    path,
                    elapsed,
                    context.Response.StatusCode,
                    userName ?? "Anonymous",
                    userId ?? "N/A",
                    tenantId ?? "N/A",
                    correlationId);
            }
            else if (!isInGracePeriod && elapsed > _slowRequestThresholdMs)
            {
                var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : "N/A";
                var userName = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(ClaimTypes.Name)?.Value
                    : null;
                var userId   = context.User?.FindFirst("user_id")?.Value;
                var tenantId = context.User?.FindFirst("tenant_id")?.Value;

                // Slow requests (>200ms by default)
                _logger.LogWarning(
                    "Slow Request: {Method} {Path} took {Elapsed}ms (threshold: {Threshold}ms) - Status: {StatusCode} " +
                    "User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}",
                    method,
                    path,
                    elapsed,
                    _slowRequestThresholdMs,
                    context.Response.StatusCode,
                    userName ?? "Anonymous",
                    userId ?? "N/A",
                    tenantId ?? "N/A",
                    correlationId);
            }
        }
    }
}

/// <summary>
/// Extension methods for registering PerformanceTelemetryMiddleware
/// </summary>
public static class PerformanceTelemetryMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceTelemetry(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceTelemetryMiddleware>();
    }
}
