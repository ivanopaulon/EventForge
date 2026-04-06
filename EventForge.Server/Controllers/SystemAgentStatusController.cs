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
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    [HttpGet]
    public IActionResult GetAgentStatus()
    {
        var agentLocalUrl = (configuration["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(agentLocalUrl))
        {
            return Ok(new AgentStatusResponseDto(
                Reachable: false,
                Status: "NotConfigured",
                InstallationName: null,
                AgentVersion: null,
                ServerVersion: null,
                ClientVersion: null,
                HubConnectionState: null,
                LastHeartbeatAt: null,
                ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
                UnreachableSinceUtc: null,
                AutoRestartAfterMinutes: 0));
        }

        if (!agentMonitor.Reachable)
        {
            return Ok(new AgentStatusResponseDto(
                Reachable: false,
                Status: "Offline",
                InstallationName: null,
                AgentVersion: null,
                ServerVersion: null,
                ClientVersion: null,
                HubConnectionState: null,
                LastHeartbeatAt: null,
                ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
                UnreachableSinceUtc: agentMonitor.UnreachableSince,
                AutoRestartAfterMinutes: agentMonitor.AutoRestartAfterMinutes));
        }

        // Parse the last cached JSON from the background probe
        try
        {
            var json = agentMonitor.LastStatusJson;
            if (!string.IsNullOrEmpty(json))
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return Ok(new AgentStatusResponseDto(
                    Reachable: true,
                    Status: root.TryGetProperty("status", out var s) ? s.GetString() ?? "Online" : "Online",
                    InstallationName: root.TryGetProperty("installationName", out var n) ? n.GetString() : null,
                    AgentVersion: root.TryGetProperty("agentVersion", out var av) ? av.GetString() : null,
                    ServerVersion: root.TryGetProperty("serverVersion", out var sv) ? sv.GetString() : null,
                    ClientVersion: root.TryGetProperty("clientVersion", out var cv) ? cv.GetString() : null,
                    HubConnectionState: root.TryGetProperty("hubConnectionState", out var h) ? h.GetString() : null,
                    LastHeartbeatAt: root.TryGetProperty("lastHeartbeatAt", out var lh) && lh.ValueKind != JsonValueKind.Null
                        ? lh.GetDateTime() : null,
                    ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
                    UnreachableSinceUtc: null,
                    AutoRestartAfterMinutes: agentMonitor.AutoRestartAfterMinutes));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse cached agent status JSON");
        }

        return Ok(new AgentStatusResponseDto(
            Reachable: true,
            Status: "Online",
            InstallationName: null,
            AgentVersion: null,
            ServerVersion: null,
            ClientVersion: null,
            HubConnectionState: null,
            LastHeartbeatAt: null,
            ProbedAt: agentMonitor.LastSeenAt ?? DateTime.UtcNow,
            UnreachableSinceUtc: null,
            AutoRestartAfterMinutes: agentMonitor.AutoRestartAfterMinutes));
    }

    /// <summary>
    /// Manually triggers an Agent service restart. SuperAdmin only.
    /// </summary>
    [HttpPost("restart")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult RestartAgent()
    {
        logger.LogInformation("Manual Agent restart requested by {User}",
            User.Identity?.Name ?? "unknown");

        var message = agentMonitor.TryRestartService();
        var success = !message.StartsWith("Errore") &&
                      !message.StartsWith("Il riavvio") &&
                      !message.Contains("non trovato");

        return success
            ? Ok(new AgentRestartResultDto(true, message))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new AgentRestartResultDto(false, message));
    }
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
