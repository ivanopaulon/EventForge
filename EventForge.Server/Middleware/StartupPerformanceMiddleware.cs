using System.Diagnostics;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware to track and log application startup performance metrics.
/// </summary>
public class StartupPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StartupPerformanceMiddleware> _logger;
    private static readonly Stopwatch _startupStopwatch = Stopwatch.StartNew();
    private static bool _firstRequestLogged = false;
    private static readonly object _lock = new object();

    public StartupPerformanceMiddleware(RequestDelegate next, ILogger<StartupPerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log time to first request only once
        if (!_firstRequestLogged)
        {
            lock (_lock)
            {
                if (!_firstRequestLogged)
                {
                    _startupStopwatch.Stop();
                    var startupTime = _startupStopwatch.Elapsed;
                    
                    _logger.LogInformation("🚀 APPLICATION STARTUP COMPLETE - Time to first request: {StartupTimeMs}ms ({StartupTimeSec:F2}s)", 
                        startupTime.TotalMilliseconds, 
                        startupTime.TotalSeconds);

                    // Log performance categorization
                    if (startupTime.TotalSeconds < 3)
                    {
                        _logger.LogInformation("✅ Startup Performance: EXCELLENT (< 3s)");
                    }
                    else if (startupTime.TotalSeconds < 5)
                    {
                        _logger.LogInformation("✅ Startup Performance: GOOD (3-5s)");
                    }
                    else if (startupTime.TotalSeconds < 10)
                    {
                        _logger.LogWarning("⚠️ Startup Performance: ACCEPTABLE (5-10s)");
                    }
                    else if (startupTime.TotalSeconds < 15)
                    {
                        _logger.LogWarning("⚠️ Startup Performance: SLOW (10-15s)");
                    }
                    else
                    {
                        _logger.LogError("❌ Startup Performance: VERY SLOW (> 15s) - Consider optimization");
                    }

                    _firstRequestLogged = true;
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add startup performance middleware.
/// </summary>
public static class StartupPerformanceMiddlewareExtensions
{
    public static IApplicationBuilder UseStartupPerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StartupPerformanceMiddleware>();
    }
}
