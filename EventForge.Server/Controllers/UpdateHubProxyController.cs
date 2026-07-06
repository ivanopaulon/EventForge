using EventForge.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Server-side proxy for UpdateHub REST APIs.
/// Only accessible to users with the SuperAdmin role.
/// Returns 503 when the UpdateHub is not configured (BaseUrl or AdminApiKey empty).
/// </summary>
[ApiController]
[Route("api/v1/updatehub-proxy")]
[Authorize(Roles = "SuperAdmin")]
public class UpdateHubProxyController(
    IUpdateHubProxyService proxy,
    IConfiguration configuration,
    ILogger<UpdateHubProxyController> logger) : BaseApiController
{
    /// <summary>Returns the configured UpdateHub base URL (empty string when not configured).</summary>
    /// <returns>The UpdateHub base URL</returns>
    /// <response code="200">Returns the UpdateHub base URL</response>
    [HttpGet("hub-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHubUrl()
    {
        var url = (configuration["UpdateHub:BaseUrl"] ?? string.Empty).TrimEnd('/');
        return Ok(new { HubUrl = url });
    }

    /// <summary>Returns all packages stored in the UpdateHub.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of packages</returns>
    /// <response code="200">Returns list of packages</response>
    /// <response code="503">UpdateHub is not configured</response>
    /// <response code="502">UpdateHub returned an unexpected error</response>
    [HttpGet("packages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetPackages(CancellationToken ct)
    {
        try
        {
            var packages = await proxy.GetPackagesAsync(ct);
            return Ok(packages);
        }
        catch (UpdateHubNotConfiguredException ex)
        {
            logger.LogWarning(ex, "UpdateHub not configured");
            return CreateServiceUnavailableProblem(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching packages from UpdateHub");
            return CreateBadGatewayProblem(ex.Message);
        }
    }

    /// <summary>Returns all registered installations with their online/offline state.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of registered installations</returns>
    /// <response code="200">Returns list of installations</response>
    /// <response code="503">UpdateHub is not configured</response>
    /// <response code="502">UpdateHub returned an unexpected error</response>
    [HttpGet("installations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetInstallations(CancellationToken ct)
    {
        try
        {
            var installations = await proxy.GetInstallationsAsync(ct);
            return Ok(installations);
        }
        catch (UpdateHubNotConfiguredException ex)
        {
            logger.LogWarning(ex, "UpdateHub not configured");
            return CreateServiceUnavailableProblem(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching installations from UpdateHub");
            return CreateBadGatewayProblem(ex.Message);
        }
    }

    /// <summary>Dispatches an update command to a specific installation.</summary>
    /// <param name="installationId">Target installation identifier</param>
    /// <param name="request">Request containing the package identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Accepted if the command was dispatched</returns>
    /// <response code="202">Command accepted by UpdateHub</response>
    /// <response code="503">UpdateHub is not configured</response>
    /// <response code="502">UpdateHub returned an unexpected error</response>
    [HttpPost("installations/{installationId:guid}/update")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendUpdate(Guid installationId, [FromBody] SendUpdateToInstallationRequest request, CancellationToken ct)
    {
        try
        {
            await proxy.SendUpdateAsync(installationId, request.PackageId, ct);
            logger.LogInformation("Update dispatched: Installation={InstallationId} Package={PackageId}",
                installationId, request.PackageId);
            return Accepted();
        }
        catch (UpdateHubNotConfiguredException ex)
        {
            logger.LogWarning(ex, "UpdateHub not configured");
            return CreateServiceUnavailableProblem(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending update Installation={InstallationId}", installationId);
            return CreateBadGatewayProblem(ex.Message);
        }
    }
}

public record SendUpdateToInstallationRequest(Guid PackageId);
