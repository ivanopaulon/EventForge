namespace Prym.Agent.Services;

/// <summary>
/// Singleton that exposes the current Hub connection state, last heartbeat time,
/// and signals for the local web UI to trigger agent actions.
/// </summary>
public class AgentStatusService
{
    private volatile bool _reRegisterRequested;

    public string HubConnectionState { get; set; } = "Disconnected";
    public DateTime? LastHeartbeatAt { get; set; }
    public string? LastHeartbeatError { get; set; }

    /// <summary>Enrollment status: null = not attempted, "Enrolled" = success, "Failed" = error.</summary>
    public string? EnrollmentStatus { get; set; }

    /// <summary>
    /// Request that the AgentWorker re-sends a RegisterInstallation message to the Hub
    /// on the next heartbeat cycle. The flag is automatically cleared after the worker acts on it.
    /// </summary>
    public void RequestReRegister() => _reRegisterRequested = true;

    /// <summary>Consumes the re-register request (returns true the first time, false thereafter).</summary>
    public bool ConsumeReRegisterRequest()
    {
        if (!_reRegisterRequested) return false;
        _reRegisterRequested = false;
        return true;
    }
}
