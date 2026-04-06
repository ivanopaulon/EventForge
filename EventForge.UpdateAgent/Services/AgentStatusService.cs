namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Singleton that exposes the current Hub connection state and last heartbeat time
/// for consumption by the local web UI.
/// </summary>
public class AgentStatusService
{
    public string HubConnectionState { get; set; } = "Disconnected";
    public DateTime? LastHeartbeatAt { get; set; }
    public string? LastHeartbeatError { get; set; }
}
