namespace EventForge.UpdateHub.Services;

/// <summary>Tracks which agent SignalR connections belong to which installation.</summary>
public interface IConnectionTracker
{
    void Register(string connectionId, Guid installationId);
    void Unregister(string connectionId);
    Guid? GetInstallationId(string connectionId);
    string? GetConnectionId(Guid installationId);
    IReadOnlyCollection<Guid> GetOnlineInstallationIds();
}
