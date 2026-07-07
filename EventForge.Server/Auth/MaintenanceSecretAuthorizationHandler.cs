using Microsoft.AspNetCore.Authorization;

namespace EventForge.Server.Auth;

/// <summary>
/// Marker requirement for the <c>MaintenanceSecret</c> authorization policy.
/// </summary>
public class MaintenanceSecretRequirement : IAuthorizationRequirement { }

/// <summary>
/// Authorization handler that validates the <c>X-Maintenance-Secret</c> request header
/// against <c>UpdateHub:MaintenanceSecret</c> in configuration.
/// Allows the EventForge Agent to call internal endpoints without a JWT bearer token,
/// using a shared secret already required for maintenance phase notifications.
/// </summary>
public class MaintenanceSecretAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<MaintenanceSecretAuthorizationHandler> logger)
    : AuthorizationHandler<MaintenanceSecretRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MaintenanceSecretRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            logger.LogWarning("MaintenanceSecret authorization: no HttpContext available");
            context.Fail();
            return Task.CompletedTask;
        }

        var expectedSecret = configuration["UpdateHub:MaintenanceSecret"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            logger.LogWarning("MaintenanceSecret authorization: UpdateHub:MaintenanceSecret is not configured");
            context.Fail();
            return Task.CompletedTask;
        }

        httpContext.Request.Headers.TryGetValue("X-Maintenance-Secret", out var provided);
        if (string.Equals(provided, expectedSecret, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning(
                "MaintenanceSecret authorization failed for {Ip}",
                httpContext.Connection.RemoteIpAddress);
        }

        return Task.CompletedTask;
    }
}
