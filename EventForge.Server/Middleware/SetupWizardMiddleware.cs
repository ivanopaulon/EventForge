using EventForge.Server.Services.Setup;

namespace EventForge.Server.Middleware;

/// <summary>
/// Middleware to redirect to setup wizard on first run.
/// </summary>
public class SetupWizardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupWizardMiddleware> _logger;

    public SetupWizardMiddleware(RequestDelegate next, ILogger<SetupWizardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IFirstRunDetectionService firstRunDetection)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var isSetupComplete = await firstRunDetection.IsSetupCompleteAsync();
        
        if (!isSetupComplete)
        {
            _logger.LogDebug("Setup not complete, redirecting to /setup");
            context.Response.Redirect("/setup");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for SetupWizardMiddleware.
/// </summary>
public static class SetupWizardMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupWizard(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SetupWizardMiddleware>();
    }
}
