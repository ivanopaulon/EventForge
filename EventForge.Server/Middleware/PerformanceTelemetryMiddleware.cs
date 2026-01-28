using System.Diagnostics;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware for tracking request performance and logging slow requests
/// </summary>
public class PerformanceTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTelemetryMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public PerformanceTelemetryMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTelemetryMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = configuration.GetValue<int>("Performance:SlowRequestThresholdMs", 200);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            
            // Add response time header for all requests
            context.Response.Headers.TryAdd("X-Response-Time-Ms", elapsed.ToString());
            
            // Log slow requests
            if (elapsed > _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow Request: {Method} {Path} took {Elapsed}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                    method,
                    path,
                    elapsed,
                    _slowRequestThresholdMs,
                    context.Response.StatusCode);
            }
            
            // Log very slow requests as error
            if (elapsed > _slowRequestThresholdMs * 2)
            {
                _logger.LogError(
                    "Very Slow Request: {Method} {Path} took {Elapsed}ms - Status: {StatusCode}",
                    method,
                    path,
                    elapsed,
                    context.Response.StatusCode);
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
