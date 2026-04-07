using EventForge.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

[ApiController]
[Route("api/v1/agent-proxy")]
[Authorize(Roles = "SuperAdmin")]
public class AgentUpdateProxyController(
    IAgentUpdateProxyService proxy,
    ILogger<AgentUpdateProxyController> logger) : ControllerBase
{
    [HttpGet("pending-installs")]
    public async Task<IActionResult> GetPendingInstalls(CancellationToken ct)
    {
        try { return Ok(await proxy.GetPendingInstallsAsync(ct)); }
        catch (AgentNotConfiguredException ex) { return StatusCode(503, new { Message = ex.Message }); }
        catch (Exception ex) { logger.LogError(ex, "Error fetching pending installs from Agent"); return StatusCode(502, new { Message = ex.Message }); }
    }

    [HttpPost("install-now")]
    public async Task<IActionResult> TriggerInstallNow([FromBody] AgentInstallNowProxyRequest request, CancellationToken ct)
    {
        try { await proxy.TriggerInstallNowAsync(request.PackageId, ct); return Accepted(); }
        catch (AgentNotConfiguredException ex) { return StatusCode(503, new { Message = ex.Message }); }
        catch (Exception ex) { logger.LogError(ex, "Error sending InstallNow to Agent"); return StatusCode(502, new { Message = ex.Message }); }
    }

    [HttpPost("unblock-queue")]
    public async Task<IActionResult> TriggerUnblockQueue([FromBody] AgentUnblockQueueProxyRequest request, CancellationToken ct)
    {
        try { await proxy.TriggerUnblockQueueAsync(request.PackageId, request.SkipAndRemove, ct); return Accepted(); }
        catch (AgentNotConfiguredException ex) { return StatusCode(503, new { Message = ex.Message }); }
        catch (Exception ex) { logger.LogError(ex, "Error sending UnblockQueue to Agent"); return StatusCode(502, new { Message = ex.Message }); }
    }
}

public record AgentInstallNowProxyRequest(Guid PackageId);
public record AgentUnblockQueueProxyRequest(Guid PackageId, bool SkipAndRemove);
