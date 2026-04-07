using Prym.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

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
    ILogger<UpdateHubProxyController> logger) : ControllerBase
{
    /// <summary>Returns all packages stored in the UpdateHub.</summary>
    [HttpGet("packages")]
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching packages from UpdateHub");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = "Unable to reach UpdateHub", Detail = ex.Message });
        }
    }

    /// <summary>Returns all registered installations with their online/offline state.</summary>
    [HttpGet("installations")]
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching installations from UpdateHub");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = "Unable to reach UpdateHub", Detail = ex.Message });
        }
    }

    /// <summary>Dispatches an update command to a specific installation.</summary>
    [HttpPost("installations/{installationId:guid}/update")]
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending update Installation={InstallationId}", installationId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = "Unable to reach UpdateHub", Detail = ex.Message });
        }
    }
}

public record SendUpdateToInstallationRequest(Guid PackageId);
