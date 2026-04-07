using System.Collections.Concurrent;

namespace Prym.Server.Services.Chat;

/// <summary>
/// In-memory implementation of <see cref="IOnlineUserTracker"/>.
/// Uses a connection-count per user so that a user with multiple browser tabs
/// is still considered online until all tabs are closed.
/// </summary>
public sealed class OnlineUserTracker : IOnlineUserTracker
{
    // userId → number of active connections
    private readonly ConcurrentDictionary<Guid, int> _connections = new();

    /// <inheritdoc/>
    public void UserConnected(Guid userId)
        => _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);

    /// <inheritdoc/>
    public void UserDisconnected(Guid userId)
    {
        _connections.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));

        // Remove the entry once the user has no more active connections
        if (_connections.TryGetValue(userId, out var remaining) && remaining == 0)
            _connections.TryRemove(userId, out _);
    }

    /// <inheritdoc/>
    public bool IsOnline(Guid userId)
        => _connections.TryGetValue(userId, out var count) && count > 0;

    /// <inheritdoc/>
    public IReadOnlySet<Guid> GetOnlineUserIds()
        => _connections
            .Where(kv => kv.Value > 0)
            .Select(kv => kv.Key)
            .ToHashSet();
}
