using System.Collections.Concurrent;

namespace Prym.Hub.Services;

/// <summary>
/// Thread-safe in-memory tracker for agent SignalR connections.
/// Registered as Singleton.
/// </summary>
public class ConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, Guid> _connectionToInstallation = new();
    private readonly ConcurrentDictionary<Guid, string> _installationToConnection = new();

    public void Register(string connectionId, Guid installationId)
    {
        // Remove old connection for this installation if any
        if (_installationToConnection.TryGetValue(installationId, out var oldConn))
            _connectionToInstallation.TryRemove(oldConn, out _);

        _connectionToInstallation[connectionId] = installationId;
        _installationToConnection[installationId] = connectionId;
    }

    public void Unregister(string connectionId)
    {
        if (_connectionToInstallation.TryRemove(connectionId, out var installationId))
            _installationToConnection.TryRemove(installationId, out _);
    }

    public Guid? GetInstallationId(string connectionId)
        => _connectionToInstallation.TryGetValue(connectionId, out var id) ? id : null;

    public string? GetConnectionId(Guid installationId)
        => _installationToConnection.TryGetValue(installationId, out var conn) ? conn : null;

    public IReadOnlyCollection<Guid> GetOnlineInstallationIds()
        => _installationToConnection.Keys.ToList().AsReadOnly();
}
