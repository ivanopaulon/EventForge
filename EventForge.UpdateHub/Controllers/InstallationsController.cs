using EventForge.UpdateHub.Configuration;
using EventForge.UpdateHub.Hubs;
using EventForge.UpdateHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.UpdateHub.Controllers;

/// <summary>Installation registry and update dispatch API.</summary>
[ApiController]
[Route("api/v1/installations")]
public class InstallationsController(
    IInstallationService installationService,
    IPackageService packageService,
    IConnectionTracker connectionTracker,
    IHubContext<AgentHub> hubContext,
    UpdateHubOptions hubOptions,
    ILogger<InstallationsController> logger) : ControllerBase
{
    private bool IsAdminAuthorized()
    {
        Request.Headers.TryGetValue("X-Admin-Key", out var key);
        return !string.IsNullOrWhiteSpace(hubOptions.AdminApiKey) && key == hubOptions.AdminApiKey;
    }

    /// <summary>List all registered installations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        var installations = await installationService.GetAllAsync();
        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        return Ok(installations.Select(i => new
        {
            i.Id, i.Name, i.Location, i.Components,
            i.InstalledVersionServer, i.InstalledVersionClient,
            i.Status, i.LastSeen, i.RegisteredAt,
            IsConnected = onlineIds.Contains(i.Id)
        }));
    }

    /// <summary>Register a new installation (admin only, returns API key).</summary>
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterInstallationRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var apiKey = Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var installation = new Installation
        {
            Name = request.Name,
            Location = request.Location,
            Components = request.Components,
            ApiKey = apiKey,
            Notes = request.Notes
        };

        var created = await installationService.CreateAsync(installation);
        logger.LogInformation("Installation registered: {Name} ({Id})", created.Name, created.Id);

        return CreatedAtAction(nameof(GetAll), new { }, new
        {
            created.Id, created.Name, ApiKey = apiKey
        });
    }

    /// <summary>Send an update to a specific installation.</summary>
    [HttpPost("{id:guid}/update")]
    public async Task<IActionResult> SendUpdate(Guid id, [FromBody] SendUpdateRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var installation = await installationService.GetByIdAsync(id);
        if (installation is null) return NotFound("Installation not found.");

        var package = await packageService.GetByIdAsync(request.PackageId);
        if (package is null) return NotFound("Package not found.");

        var connectionId = connectionTracker.GetConnectionId(id);
        if (connectionId is null)
            return Conflict("Installation is not currently connected.");

        var history = await installationService.StartUpdateHistoryAsync(
            id, request.PackageId,
            installation.InstalledVersionServer,
            installation.InstalledVersionClient);

        var downloadUrl = $"{Request.Scheme}://{Request.Host}/api/v1/packages/{package.Id}/download";
        var command = new StartUpdateCommand(
            history.Id,
            package.Id,
            package.Version,
            package.Component.ToString(),
            downloadUrl,
            package.Checksum);

        await hubContext.Clients.Client(connectionId).SendAsync("StartUpdate", command);
        logger.LogInformation("Update command sent to Installation={InstallationId} Package={PackageId}", id, request.PackageId);

        return Accepted(new { HistoryId = history.Id });
    }

    /// <summary>Broadcast update notification to all connected agents.</summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastUpdateRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var package = await packageService.GetByIdAsync(request.PackageId);
        if (package is null) return NotFound("Package not found.");

        var downloadUrl = $"{Request.Scheme}://{Request.Host}/api/v1/packages/{package.Id}/download";
        var notification = new UpdateAvailableMessage(
            package.Id, package.Version, package.Component.ToString(),
            downloadUrl, package.Checksum, package.ReleaseNotes);

        await hubContext.Clients.All.SendAsync("UpdateAvailable", notification);
        logger.LogInformation("Update broadcast: Package={PackageId} Version={Version}", package.Id, package.Version);

        return Accepted(new { PackageId = package.Id, package.Version });
    }

    /// <summary>Revoke an installation's API key. The agent will be blocked with 403 until reinstated.</summary>
    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeInstallationRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var ok = await installationService.RevokeAsync(id, request.Reason);
        if (!ok) return NotFound($"Installation {id} not found.");

        // Disconnect the agent if currently connected
        var connectionId = connectionTracker.GetConnectionId(id);
        if (connectionId is not null)
            await hubContext.Clients.Client(connectionId).SendAsync("Disconnected", "API key revoked by administrator.");

        logger.LogWarning("Installation revoked: Id={Id} Reason={Reason}", id, request.Reason);
        return Ok(new { InstallationId = id, Revoked = true });
    }

    /// <summary>Reinstate a previously revoked installation.</summary>
    [HttpPost("{id:guid}/reinstate")]
    public async Task<IActionResult> Reinstate(Guid id)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var ok = await installationService.ReinstateAsync(id);
        if (!ok) return NotFound($"Installation {id} not found.");

        logger.LogInformation("Installation reinstated: Id={Id}", id);
        return Ok(new { InstallationId = id, Reinstated = true });
    }

    /// <summary>
    /// Returns all packages that are downloaded and awaiting manual operator approval
    /// (UpdateHistory Status=InProgress, Phase=AwaitingMaintenanceWindow), across all installations.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingInstalls()
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        var pending = await installationService.GetPendingInstallsAsync();
        return Ok(pending.Select(p => new
        {
            p.InstallationId, p.InstallationName, p.IsConnected,
            p.HistoryId, p.PackageId, p.Component, p.Version,
            p.IsManualInstall, p.QueuedAt
        }));
    }

    /// <summary>Sends an InstallNow command to the agent for the specified queued package.</summary>
    [HttpPost("{id:guid}/install-now")]
    public async Task<IActionResult> TriggerInstallNow(Guid id, [FromBody] TriggerInstallNowRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var connectionId = connectionTracker.GetConnectionId(id);
        if (connectionId is null)
            return Conflict("Installation is not currently connected.");

        await hubContext.Clients.Client(connectionId).SendAsync("InstallNow", new InstallNowCommand(request.PackageId));
        logger.LogInformation("InstallNow sent to Installation={InstallationId} Package={PackageId}", id, request.PackageId);
        return Accepted();
    }

    /// <summary>Sends an UnblockQueue command to the agent to retry or skip the blocking entry.</summary>
    [HttpPost("{id:guid}/unblock-queue")]
    public async Task<IActionResult> TriggerUnblockQueue(Guid id, [FromBody] TriggerUnblockQueueRequest request)
    {
        if (!IsAdminAuthorized()) return Unauthorized();

        var connectionId = connectionTracker.GetConnectionId(id);
        if (connectionId is null)
            return Conflict("Installation is not currently connected.");

        await hubContext.Clients.Client(connectionId).SendAsync("UnblockQueue", new UnblockQueueCommand(request.PackageId, request.SkipAndRemove));
        logger.LogInformation("UnblockQueue sent to Installation={InstallationId} Package={PackageId} Skip={Skip}",
            id, request.PackageId, request.SkipAndRemove);
        return Accepted();
    }
}

public record RegisterInstallationRequest(string Name, string? Location, InstallationComponents Components, string? Notes);
public record SendUpdateRequest(Guid PackageId);
public record BroadcastUpdateRequest(Guid PackageId);
public record RevokeInstallationRequest(string? Reason);
public record TriggerInstallNowRequest(Guid PackageId);
public record TriggerUnblockQueueRequest(Guid PackageId, bool SkipAndRemove);
