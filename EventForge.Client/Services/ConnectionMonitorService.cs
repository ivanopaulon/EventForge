using Microsoft.Extensions.Logging;

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
    Task CheckNowAsync();
}

public class ConnectionMonitorService : IConnectionMonitorService
{
    private readonly IHealthService _healthService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<ConnectionMonitorService> _logger;
    private Timer? _timer;
    private bool _isRunning;
    private bool _hasCompletedFirstCheck = false;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;
    public bool IsApiReachable { get; private set; } = true;
    public bool IsSignalRConnected { get; private set; } = true;
    public DateTimeOffset? DisconnectedSince { get; private set; }
    public string? StatusMessage { get; private set; }

    public event Action<ConnectionStatus>? StatusChanged;

    public ConnectionMonitorService(
        IHealthService healthService,
        IRealtimeService realtimeService,
        ILogger<ConnectionMonitorService> logger)
    {
        _healthService = healthService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _timer = new Timer(async _ => await CheckNowAsync(), null,
            TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
    }

    public async Task CheckNowAsync()
    {
        var previousStatus = Status;

        try
        {
            var health = await _healthService.GetHealthAsync();
            IsApiReachable = health != null;
        }
        catch
        {
            IsApiReachable = false;
        }

        IsSignalRConnected = _realtimeService.IsChatConnected || _realtimeService.IsNotificationConnected;

        ConnectionStatus newStatus;
        if (IsApiReachable && IsSignalRConnected)
        {
            newStatus = ConnectionStatus.Connected;
            StatusMessage = null;
            DisconnectedSince = null;
        }
        else if (!_hasCompletedFirstCheck)
        {
            // Still in the very first check cycle: stay Unknown to avoid flash
            newStatus = ConnectionStatus.Unknown;
        }
        else if (!IsApiReachable && !IsSignalRConnected)
        {
            newStatus = ConnectionStatus.Disconnected;
            StatusMessage = "Server non raggiungibile";
            if (DisconnectedSince == null) DisconnectedSince = DateTimeOffset.UtcNow;
        }
        else
        {
            newStatus = ConnectionStatus.Degraded;
            StatusMessage = !IsApiReachable ? "API non raggiungibile" : "Connessione real-time interrotta";
            if (DisconnectedSince == null) DisconnectedSince = DateTimeOffset.UtcNow;
        }

        _hasCompletedFirstCheck = true;
        Status = newStatus;

        if (newStatus != previousStatus)
        {
            _logger.LogWarning("Connection status changed: {Previous} -> {New}", previousStatus, newStatus);
            StatusChanged?.Invoke(newStatus);
        }
    }

    public void Dispose() => Stop();
}
