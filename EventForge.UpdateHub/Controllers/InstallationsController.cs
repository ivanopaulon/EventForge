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
    IConfiguration configuration,
    ILogger<InstallationsController> logger) : ControllerBase
{
    private string AdminApiKey => configuration["UpdateHub:AdminApiKey"] ?? string.Empty;

    private bool IsAdminAuthorized()
    {
        Request.Headers.TryGetValue("X-Admin-Key", out var key);
        return !string.IsNullOrWhiteSpace(AdminApiKey) && key == AdminApiKey;
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

        var apiKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();
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
}

public record RegisterInstallationRequest(string Name, string? Location, InstallationComponents Components, string? Notes);
public record SendUpdateRequest(Guid PackageId);
public record BroadcastUpdateRequest(Guid PackageId);
