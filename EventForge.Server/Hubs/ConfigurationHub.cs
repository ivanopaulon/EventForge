using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using EventForge.Server.Auth;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time configuration and system operation notifications.
/// Requires SuperAdmin authentication.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.RequireSuperAdmin)]
public class ConfigurationHub : Hub
{
    private readonly ILogger<ConfigurationHub> _logger;

    public ConfigurationHub(ILogger<ConfigurationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Notifies all connected clients about a configuration change.
    /// </summary>
    public async Task NotifyConfigurationChanged(string key, string newValue, string changedBy)
    {
        _logger.LogInformation("Configuration changed: {Key} by {User}", key, changedBy);
        
        await Clients.Others.SendAsync("ConfigurationChanged", new
        {
            Key = key,
            Value = newValue,
            ChangedBy = changedBy,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notifies all connected clients that a server restart is required.
    /// </summary>
    public async Task NotifyRestartRequired(List<string> reasons)
    {
        _logger.LogWarning("Server restart required: {Reasons}", string.Join(", ", reasons));
        
        await Clients.All.SendAsync("RestartRequired", new
        {
            Reasons = reasons,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notifies all connected clients about a system operation.
    /// </summary>
    public async Task NotifySystemOperation(string operationType, string action, string description, bool success)
    {
        _logger.LogInformation("System operation: {Type} - {Action} - {Success}", operationType, action, success);
        
        await Clients.All.SendAsync("SystemOperation", new
        {
            OperationType = operationType,
            Action = action,
            Description = description,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Configuration hub client connected: {User} [{ConnectionId}]", user, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Context.User?.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Configuration hub client disconnected: {User} [{ConnectionId}]", user, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
