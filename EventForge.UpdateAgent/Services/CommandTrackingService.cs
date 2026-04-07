namespace EventForge.UpdateAgent.Services;

public enum CommandState
{
    Received,
    Downloading,
    DownloadFailed,
    Downloaded,
    Installing,
    Installed,
    Failed
}

public record TrackedCommand(
    Guid PackageId,
    string Component,
    string Version,
    CommandState State,
    string? ErrorMessage,
    DateTime ReceivedAt,
    DateTime? StateChangedAt);

/// <summary>
/// Represents a package announced by the Hub via the UpdateAvailable signal.
/// </summary>
public record NotifiedPackage(
    Guid PackageId,
    string Component,
    string Version,
    string? ReleaseNotes,
    DateTime ReceivedAt,
    /// <summary>true = version was newer and agent called RequestStartUpdate; false = already up-to-date or component not managed.</summary>
    bool WasNewer);

/// <summary>
/// Thread-safe singleton that tracks every <see cref="StartUpdateCommand"/> received
/// with its full lifecycle state, and every UpdateAvailable notification.
/// </summary>
public class CommandTrackingService
{
    private readonly List<TrackedCommand> _commands = [];
    private readonly List<NotifiedPackage> _notified = [];
    private readonly Lock _lock = new();
    private const int MaxEntries = 50;

    public void Track(StartUpdateCommand command)
    {
        lock (_lock)
        {
            _commands.RemoveAll(c => c.PackageId == command.PackageId);
            _commands.Add(new TrackedCommand(
                command.PackageId,
                command.Component,
                command.Version,
                CommandState.Received,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow));
            TrimOldest();
        }
    }

    public void SetState(Guid packageId, CommandState state, string? error = null)
    {
        lock (_lock)
        {
            var idx = _commands.FindIndex(c => c.PackageId == packageId);
            if (idx < 0) return;
            var existing = _commands[idx];
            _commands[idx] = existing with { State = state, ErrorMessage = error, StateChangedAt = DateTime.UtcNow };
        }
    }

    public IReadOnlyList<TrackedCommand> GetAll()
    {
        lock (_lock)
            return [.. _commands.OrderByDescending(c => c.ReceivedAt)];
    }

    public void Remove(Guid packageId)
    {
        lock (_lock)
            _commands.RemoveAll(c => c.PackageId == packageId);
    }

    public void TrackNotified(Guid packageId, string component, string version, string? releaseNotes, bool wasNewer)
    {
        lock (_lock)
        {
            _notified.RemoveAll(n => n.PackageId == packageId);
            _notified.Add(new NotifiedPackage(packageId, component, version, releaseNotes, DateTime.UtcNow, wasNewer));
            TrimOldestNotified();
        }
    }

    public IReadOnlyList<NotifiedPackage> GetNotified()
    {
        lock (_lock)
            return [.. _notified.OrderByDescending(n => n.ReceivedAt)];
    }

    private void TrimOldestNotified()
    {
        while (_notified.Count > MaxEntries)
            _notified.RemoveAt(0);
    }

    private void TrimOldest()
    {
        if (_commands.Count <= MaxEntries) return;
        // Remove oldest successfully-completed entries first
        var toRemove = _commands
            .Where(c => c.State == CommandState.Installed)
            .OrderBy(c => c.ReceivedAt)
            .Take(_commands.Count - MaxEntries)
            .ToList();

        foreach (var item in toRemove)
            _commands.Remove(item);

        // If still over limit, remove oldest overall
        while (_commands.Count > MaxEntries)
            _commands.RemoveAt(0);
    }
}
