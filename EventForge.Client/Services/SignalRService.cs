using Microsoft.AspNetCore.SignalR.Client;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing SignalR connection and real-time communication.
/// </summary>
public class SignalRService : IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _hubConnection;

    public event Action<object>? AuditLogUpdated;
    public event Action<object>? UserStatusChanged;
    public event Action<object>? UserRolesChanged;
    public event Action<object>? PasswordChangeForced;
    public event Action<object>? BackupStatusChanged;

    public SignalRService(IHttpClientFactory httpClientFactory, IAuthService authService, ILogger<SignalRService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Starts the SignalR connection to the audit log hub.
    /// </summary>
    public async Task StartConnectionAsync()
    {
        if (_hubConnection != null)
        {
            return; // Already connected
        }

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot start SignalR connection: no access token available");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            
            var hubUrl = new Uri(httpClient.BaseAddress!, "/hubs/audit-log").ToString();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            // Register event handlers
            _hubConnection.On<object>("AuditLogUpdated", (data) =>
            {
                _logger.LogInformation("Audit log updated: {Data}", data);
                AuditLogUpdated?.Invoke(data);
            });

            _hubConnection.On<object>("UserStatusChanged", (data) =>
            {
                _logger.LogInformation("User status changed: {Data}", data);
                UserStatusChanged?.Invoke(data);
            });

            _hubConnection.On<object>("UserRolesChanged", (data) =>
            {
                _logger.LogInformation("User roles changed: {Data}", data);
                UserRolesChanged?.Invoke(data);
            });

            _hubConnection.On<object>("PasswordChangeForced", (data) =>
            {
                _logger.LogInformation("Password change forced: {Data}", data);
                PasswordChangeForced?.Invoke(data);
            });

            _hubConnection.On<object>("BackupStatusChanged", (data) =>
            {
                _logger.LogInformation("Backup status changed: {Data}", data);
                BackupStatusChanged?.Invoke(data);
            });

            // Handle connection events
            _hubConnection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                await JoinAuditLogGroup();
            };

            _hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "SignalR connection closed");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await StartConnectionAsync(); // Attempt to reconnect
            };

            await _hubConnection.StartAsync();
            await JoinAuditLogGroup();

            _logger.LogInformation("SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
        }
    }

    /// <summary>
    /// Stops the SignalR connection.
    /// </summary>
    public async Task StopConnectionAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            _logger.LogInformation("SignalR connection stopped");
        }
    }

    /// <summary>
    /// Joins the audit log group for receiving updates.
    /// </summary>
    private async Task JoinAuditLogGroup()
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("JoinAuditLogGroup");
                _logger.LogInformation("Joined audit log group");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join audit log group");
            }
        }
    }

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Checks if the connection is active.
    /// </summary>
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}