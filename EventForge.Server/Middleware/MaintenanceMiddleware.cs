using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware to handle maintenance mode.
/// </summary>
public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, EventForgeDbContext dbContext)
    {
        try
        {
            var maintenanceMode = await dbContext.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == "System.MaintenanceMode");

            if (maintenanceMode != null && maintenanceMode.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var user = context.User;
                var isSuperAdmin = user?.IsInRole("SuperAdmin") ?? false;

                if (!isSuperAdmin)
                {
                    _logger.LogDebug("Maintenance mode active, blocking request from non-SuperAdmin user");
                    
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.Headers.Append("Retry-After", "300");
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "Service Unavailable",
                        message = "The system is currently under maintenance. Please try again later.",
                        retryAfter = 300
                    };

                    await context.Response.WriteAsJsonAsync(response);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking maintenance mode, allowing request to continue");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for MaintenanceMiddleware.
/// </summary>
public static class MaintenanceMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MaintenanceMiddleware>();
    }
}
