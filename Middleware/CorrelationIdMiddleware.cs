using Serilog.Context;

namespace EventForge.Middleware;

/// <summary>
/// Middleware for handling X-Correlation-ID headers.
/// Reads existing correlation ID or generates a new one, and ensures it's returned in response headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to logging context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add correlation ID to HttpContext for access throughout the request
            context.Items["CorrelationId"] = correlationId;

            _logger.LogDebug("Processing request with Correlation ID: {CorrelationId}", correlationId);

            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdFromHeader))
        {
            var correlationId = correlationIdFromHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }
        }

        // Generate new correlation ID if not provided
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for registering CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationId middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}