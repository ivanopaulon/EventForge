namespace Prym.Agent.Services;

/// <summary>
/// Singleton that exposes the current Hub connection state, last heartbeat time,
/// and signals for the local web UI to trigger agent actions.
/// </summary>
public class AgentStatusService
{
    private volatile bool _reRegisterRequested;

    /// <summary>Current SignalR Hub connection state string (e.g. "Connected", "Disconnected").</summary>
    private volatile string _hubConnectionState = "Disconnected";

    public string HubConnectionState
    {
        get => _hubConnectionState;
        set => _hubConnectionState = value;
    }

    /// <summary>Error message from the last failed heartbeat, or null when the last heartbeat succeeded.</summary>
    private volatile string? _lastHeartbeatError;

    public string? LastHeartbeatError
    {
        get => _lastHeartbeatError;
        set => _lastHeartbeatError = value;
    }

    private readonly object _heartbeatLock = new();
    private DateTime? _lastHeartbeatAt;

    public DateTime? LastHeartbeatAt
    {
        get { lock (_heartbeatLock) return _lastHeartbeatAt; }
        set { lock (_heartbeatLock) _lastHeartbeatAt = value; }
    }

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
