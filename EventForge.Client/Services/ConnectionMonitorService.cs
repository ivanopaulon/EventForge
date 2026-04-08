namespace EventForge.Client.Services;

public enum ConnectionStatus
{
    Unknown,
    Connected,
    Degraded,
    Disconnected
}

public interface IConnectionMonitorService : IDisposable
{
    ConnectionStatus Status { get; }
    bool IsApiReachable { get; }
    bool IsSignalRConnected { get; }
    DateTimeOffset? DisconnectedSince { get; }
    string? StatusMessage { get; }
    event Action<ConnectionStatus>? StatusChanged;
    void Start();
    void Stop();
    Task CheckNowAsync(CancellationToken ct = default);
}

public class ConnectionMonitorService(
    IHealthService healthService,
    IRealtimeService realtimeService,
    ILogger<ConnectionMonitorService> logger) : IConnectionMonitorService
{
    private Timer? _timer;
    private bool _isRunning;
    private bool _signalRWasEverConnected = false;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;
    public bool IsApiReachable { get; private set; } = true;
    public bool IsSignalRConnected { get; private set; } = true;
    public DateTimeOffset? DisconnectedSince { get; private set; }
    public string? StatusMessage { get; private set; }

    public event Action<ConnectionStatus>? StatusChanged;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        // Use a 10-second initial delay so the app has time to fully initialise
        // (load translations, branding, auth state) before the first connectivity probe.
        // Subsequent checks fire every 8 seconds.
        _timer = new Timer(async _ => await CheckNowAsync(), null,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(8));
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
    }

    public async Task CheckNowAsync(CancellationToken ct = default)
    {
        var previousStatus = Status;

        try
        {
            var health = await healthService.GetHealthAsync();
            IsApiReachable = health != null;
        }
        catch
        {
            IsApiReachable = false;
        }

        IsSignalRConnected = realtimeService.IsChatConnected || realtimeService.IsNotificationConnected;
        if (IsSignalRConnected) _signalRWasEverConnected = true;

        // Only treat SignalR as a problem if it was previously established.
        // Before login, connections are never started so we must not flag them as broken.
        var signalRProblem = !IsSignalRConnected && _signalRWasEverConnected;

        ConnectionStatus newStatus;
        if (IsApiReachable && !signalRProblem)
        {
            newStatus = ConnectionStatus.Connected;
            StatusMessage = null;
            DisconnectedSince = null;
        }
        else if (!IsApiReachable && signalRProblem)
        {
            newStatus = ConnectionStatus.Disconnected;
            StatusMessage = "Server non raggiungibile";
            if (DisconnectedSince == null) DisconnectedSince = DateTimeOffset.UtcNow;
        }
        else if (!IsApiReachable)
        {
            newStatus = ConnectionStatus.Disconnected;
            StatusMessage = "Server non raggiungibile";
            if (DisconnectedSince == null) DisconnectedSince = DateTimeOffset.UtcNow;
        }
        else
        {
            // API OK but SignalR dropped after having been connected
            newStatus = ConnectionStatus.Degraded;
            StatusMessage = "Connessione real-time interrotta";
            if (DisconnectedSince == null) DisconnectedSince = DateTimeOffset.UtcNow;
        }

        Status = newStatus;

        if (newStatus != previousStatus)
        {
            logger.LogWarning("Connection status changed: {Previous} -> {New}", previousStatus, newStatus);
            StatusChanged?.Invoke(newStatus);
        }
    }

    public void Dispose() => Stop();
}
