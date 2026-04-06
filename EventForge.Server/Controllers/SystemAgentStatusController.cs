using EventForge.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventForge.Server.Controllers;

/// <summary>
/// Exposes the co-located UpdateAgent's live status (tracked by <see cref="AgentMonitorService"/>)
/// and allows SuperAdmin users to trigger a manual restart.
/// GET: any authenticated user. POST /restart: SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/v1/system/agent-status")]
[Authorize]
public class SystemAgentStatusController(
    AgentMonitorService agentMonitor,
    IConfiguration configuration,
    ILogger<SystemAgentStatusController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAgentStatus()
    {
        var agentLocalUrl = (configuration["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(agentLocalUrl))
            return Ok(BuildDto(false, "NotConfigured", null));

        if (!agentMonitor.Reachable)
            return Ok(BuildDto(false, "Offline", null));

        // Parse the last cached JSON from the background probe
        var json = agentMonitor.LastStatusJson;
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var r = doc.RootElement;
                return Ok(new AgentStatusResponseDto(
                    Reachable: true,
                    Status: r.TryGetProperty("status", out var s) ? s.GetString() ?? "Online" : "Online",
                    InstallationName: r.TryGetProperty("installationName", out var n) ? n.GetString() : null,
                    AgentVersion: r.TryGetProperty("agentVersion", out var av) ? av.GetString() : null,
                    ServerVersion: r.TryGetProperty("serverVersion", out var sv) ? sv.GetString() : null,
                    ClientVersion: r.TryGetProperty("clientVersion", out var cv) ? cv.GetString() : null,
                    HubConnectionState: r.TryGetProperty("hubConnectionState", out var h) ? h.GetString() : null,
                    LastHeartbeatAt: r.TryGetProperty("lastHeartbeatAt", out var lh) && lh.ValueKind != JsonValueKind.Null
                        ? lh.GetDateTime() : null,
                    ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
                    UnreachableSinceUtc: null,
                    AutoRestartAfterMinutes: agentMonitor.AutoRestartAfterMinutes));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse cached agent status JSON");
            }
        }

        return Ok(BuildDto(true, "Online", null));
    }

    /// <summary>Manually triggers an Agent service restart. SuperAdmin only.</summary>
    [HttpPost("restart")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult RestartAgent()
    {
        logger.LogInformation("Manual Agent restart requested by {User}",
            User.Identity?.Name ?? "unknown");

        var result = agentMonitor.TryRestartService();

        return result.Success
            ? Ok(new AgentRestartResultDto(true, result.Message))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AgentRestartResultDto(false, result.Message));
    }

    // Builds a minimal (offline/not-configured/fallback online) response DTO.
    private AgentStatusResponseDto BuildDto(bool reachable, string status, string? json) =>
        new(reachable, status,
            InstallationName: null, AgentVersion: null, ServerVersion: null,
            ClientVersion: null, HubConnectionState: null, LastHeartbeatAt: null,
            ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
            UnreachableSinceUtc: reachable ? null : agentMonitor.UnreachableSince,
            AutoRestartAfterMinutes: agentMonitor.AutoRestartAfterMinutes);
}

public record AgentStatusResponseDto(
    bool Reachable,
    string Status,
    string? InstallationName,
    string? AgentVersion,
    string? ServerVersion,
    string? ClientVersion,
    string? HubConnectionState,
    DateTime? LastHeartbeatAt,
    DateTime ProbedAt,
    DateTime? UnreachableSinceUtc,
    int AutoRestartAfterMinutes);

public record AgentRestartResultDto(bool Success, string Message);
