namespace EventForge.UpdateHub.Auth;

/// <summary>
/// Validates the X-Api-Key header for agent endpoints.
/// Sets HttpContext.Items["InstallationId"] on success.
/// </summary>
public class ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
{
    public const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, IInstallationService installationService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only enforce on /api/agent/* and /hubs/update negotiation
        if (!path.StartsWith("/api/agent", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/hubs/update", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Missing API key for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing X-Api-Key header.");
            return;
        }

        var installation = await installationService.GetByApiKeyAsync(apiKey!);
        if (installation is null)
        {
            logger.LogWarning("Invalid API key attempt for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        context.Items["InstallationId"] = installation.Id;
        context.Items["Installation"] = installation;
        await next(context);
    }
}
