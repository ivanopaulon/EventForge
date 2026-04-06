using EventForge.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Controllers;

/// <summary>
/// Receives maintenance notifications from co-located (or remote) UpdateAgents
/// and broadcasts them to all connected browser clients via <see cref="UpdateNotificationHub"/>.
///
/// Authentication: shared secret in <c>X-Maintenance-Secret</c> header,
/// compared against <c>UpdateHub:MaintenanceSecret</c> from configuration.
/// This is intentionally NOT JWT-authenticated so the Agent can call it
/// even before/after the Server restarts (no session state required).
/// </summary>
[ApiController]
[Route("api/v1/system/maintenance")]
public class SystemMaintenanceController(
    IHubContext<UpdateNotificationHub> hub,
    IConfiguration configuration,
    ILogger<SystemMaintenanceController> logger) : ControllerBase
{
    /// <summary>
    /// Receives a maintenance phase notification from the Agent and broadcasts it to clients.
    ///
    /// Expected payload:
    /// <code>
    /// { "phase": "Starting|Started|ClientDeployed", "component": "Server|Client", "version": "1.2.3" }
    /// </code>
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveMaintenanceNotification(
        [FromBody] MaintenanceNotificationRequest request)
    {
        if (!IsAuthorized())
        {
            logger.LogWarning("Maintenance notification rejected — invalid or missing X-Maintenance-Secret from {Ip}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized();
        }

        logger.LogInformation("Maintenance notification: Phase={Phase} Component={Component} Version={Version}",
            request.Phase, request.Component, request.Version);

        switch (request.Phase?.ToLowerInvariant())
        {
            case "starting":
                await hub.Clients.Group("all_clients").SendAsync("MaintenanceStarted", new
                {
                    request.Component,
                    request.Version,
                    StartedAt = DateTime.UtcNow
                });
                break;

            case "started":
                await hub.Clients.Group("all_clients").SendAsync("MaintenanceEnded", new
                {
                    request.Component,
                    request.Version,
                    EndedAt = DateTime.UtcNow
                });
                break;

            case "clientdeployed":
                await hub.Clients.Group("all_clients").SendAsync("ClientUpdateDeployed", new
                {
                    request.Component,
                    request.Version,
                    DeployedAt = DateTime.UtcNow
                });
                break;

            case "progress":
                await hub.Clients.Group("all_clients").SendAsync("UpdateProgress", new
                {
                    request.Component,
                    request.Version,
                    request.Phase,
                    request.PercentComplete,
                    request.FormattedDownloaded,
                    request.FormattedTotal,
                    request.FormattedSpeed,
                    request.Eta,
                    SentAt = DateTime.UtcNow
                });
                break;

            default:
                logger.LogWarning("Unknown maintenance phase: {Phase}", request.Phase);
                return BadRequest($"Unknown phase: {request.Phase}");
        }

        return Ok();
    }

    private bool IsAuthorized()
    {
        var expectedSecret = configuration["UpdateHub:MaintenanceSecret"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedSecret)) return false;
        Request.Headers.TryGetValue("X-Maintenance-Secret", out var provided);
        return string.Equals(provided, expectedSecret, StringComparison.Ordinal);
    }
}

public record MaintenanceNotificationRequest(
    string? Phase,
    string? Component,
    string? Version,
    int? PercentComplete = null,
    string? FormattedDownloaded = null,
    string? FormattedTotal = null,
    string? FormattedSpeed = null,
    string? Eta = null);
