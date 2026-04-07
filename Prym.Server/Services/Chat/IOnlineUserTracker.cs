namespace Prym.Server.Services.Chat;

/// <summary>
/// Tracks which users are currently connected to the chat hub.
/// Registered as a singleton to maintain state across all hub connections.
/// </summary>
public interface IOnlineUserTracker
{
    /// <summary>
    /// Records a new connection for the specified user.
    /// </summary>
    void UserConnected(Guid userId);

    /// <summary>
    /// Removes a connection for the specified user.
    /// When the connection count reaches zero the user is considered offline.
    /// </summary>
    void UserDisconnected(Guid userId);

    /// <summary>
    /// Returns true when the user has at least one active connection.
    /// </summary>
    bool IsOnline(Guid userId);

    /// <summary>
    /// Returns the set of currently online user IDs.
    /// </summary>
    IReadOnlySet<Guid> GetOnlineUserIds();
}
