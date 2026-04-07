namespace EventForge.UpdateHub.Services;

/// <summary>Tracks which agent SignalR connections belong to which installation.</summary>
public interface IConnectionTracker
{
    /// <summary>Associates a SignalR <paramref name="connectionId"/> with an <paramref name="installationId"/>.</summary>
    void Register(string connectionId, Guid installationId);

    /// <summary>Removes the mapping for the given <paramref name="connectionId"/> when the agent disconnects.</summary>
    void Unregister(string connectionId);

    /// <summary>Returns the installation ID associated with <paramref name="connectionId"/>, or <see langword="null"/> if unknown.</summary>
    Guid? GetInstallationId(string connectionId);

    /// <summary>Returns the current SignalR connection ID for <paramref name="installationId"/>, or <see langword="null"/> if the installation is offline.</summary>
    string? GetConnectionId(Guid installationId);

    /// <summary>Returns a snapshot of all installation IDs that currently have an active connection.</summary>
    IReadOnlyCollection<Guid> GetOnlineInstallationIds();
}
