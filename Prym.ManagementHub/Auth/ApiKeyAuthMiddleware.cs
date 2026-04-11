namespace Prym.ManagementHub.Auth;

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

        var requiresAuth = path.StartsWith("/api/agent", StringComparison.OrdinalIgnoreCase) ||
                           path.StartsWith("/hubs/update", StringComparison.OrdinalIgnoreCase);

        // Optional auth: try to resolve InstallationId for package download paths so that
        // agents can authenticate, but do not block the request if no key is present
        // (admin UI access must still work without an API key).
        var isPackageDownload = path.StartsWith("/api/v1/packages", StringComparison.OrdinalIgnoreCase) &&
                                path.Contains("/download", StringComparison.OrdinalIgnoreCase);

        if (!requiresAuth && !isPackageDownload)
        {
            await next(context);
            return;
        }

        var hasKey = context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) &&
                     !string.IsNullOrWhiteSpace(apiKey);

        if (!hasKey)
        {
            if (requiresAuth)
            {
                logger.LogWarning("Missing API key for {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing X-Api-Key header.");
                return;
            }

            // No key on a download path — let the request through (admin may use cookie auth).
            await next(context);
            return;
        }

        Installation? installation;
        try
        {
            installation = await installationService.GetByApiKeyAsync(apiKey!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up API key for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Authentication service unavailable.");
            return;
        }

        if (installation is null)
        {
            if (requiresAuth)
            {
                logger.LogWarning("Invalid API key attempt for {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key.");
                return;
            }

            // Invalid key on a download path — log and continue without setting InstallationId.
            logger.LogWarning("Invalid API key on download path {Path} — continuing without agent identity", path);
            await next(context);
            return;
        }

        if (installation.IsRevoked)
        {
            logger.LogWarning("Revoked installation attempt: {InstallationId} ({Name}) for {Path}",
                installation.Id, installation.Name, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(
                $"This installation has been revoked. Reason: {installation.RevokedReason ?? "Not specified"}. " +
                "Contact the Hub administrator to reinstate access.");
            return;
        }

        context.Items["InstallationId"] = installation.Id;
        context.Items["Installation"] = installation;
        await next(context);
    }
}
