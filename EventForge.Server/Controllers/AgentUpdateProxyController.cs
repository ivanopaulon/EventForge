using EventForge.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Server-side proxy for UpdateAgent REST APIs.
/// Only accessible to users with the SuperAdmin role.
/// Returns 503 when the Agent is not configured; 502 when the Agent call fails.
/// </summary>
[ApiController]
[Route("api/v1/agent-proxy")]
[Authorize(Roles = "SuperAdmin")]
public class AgentUpdateProxyController(
    IAgentUpdateProxyService proxy,
    ILogger<AgentUpdateProxyController> logger) : BaseApiController
{
    /// <summary>Returns the list of packages pending installation on the co-located Agent.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of pending install packages</returns>
    /// <response code="200">Returns pending install packages</response>
    /// <response code="503">Agent is not configured</response>
    /// <response code="502">Agent returned an unexpected error</response>
    [HttpGet("pending-installs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetPendingInstalls(CancellationToken ct)
    {
        try { return Ok(await proxy.GetPendingInstallsAsync(ct)); }
        catch (AgentNotConfiguredException ex) { return CreateServiceUnavailableProblem(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching pending installs from Agent");
            return CreateBadGatewayProblem(ex.Message);
        }
    }

    /// <summary>Triggers an immediate package installation on the co-located Agent.</summary>
    /// <param name="request">Install request containing the package identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Accepted if the command was dispatched</returns>
    /// <response code="202">Command accepted by the Agent</response>
    /// <response code="503">Agent is not configured</response>
    /// <response code="502">Agent returned an unexpected error</response>
    [HttpPost("install-now")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> TriggerInstallNow([FromBody] AgentInstallNowProxyRequest request, CancellationToken ct)
    {
        try { await proxy.TriggerInstallNowAsync(request.PackageId, ct); return Accepted(); }
        catch (AgentNotConfiguredException ex) { return CreateServiceUnavailableProblem(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending InstallNow to Agent");
            return CreateBadGatewayProblem(ex.Message);
        }
    }

    /// <summary>Unblocks the Agent's installation queue for a specific package.</summary>
    /// <param name="request">Unblock request containing the package identifier and skip flag</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Accepted if the command was dispatched</returns>
    /// <response code="202">Command accepted by the Agent</response>
    /// <response code="503">Agent is not configured</response>
    /// <response code="502">Agent returned an unexpected error</response>
    [HttpPost("unblock-queue")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> TriggerUnblockQueue([FromBody] AgentUnblockQueueProxyRequest request, CancellationToken ct)
    {
        try { await proxy.TriggerUnblockQueueAsync(request.PackageId, request.SkipAndRemove, ct); return Accepted(); }
        catch (AgentNotConfiguredException ex) { return CreateServiceUnavailableProblem(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending UnblockQueue to Agent");
            return CreateBadGatewayProblem(ex.Message);
        }
    }
}

public record AgentInstallNowProxyRequest(Guid PackageId, Guid? InstallationId = null);
public record AgentUnblockQueueProxyRequest(Guid PackageId, bool SkipAndRemove);
