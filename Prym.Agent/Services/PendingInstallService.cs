using System.Text.Json;

namespace Prym.Agent.Services;

/// <summary>
/// Represents a downloaded package that is waiting for an allowed maintenance window
/// or operator approval before being installed.
/// </summary>
public record PendingUpdate(
    Guid PackageId,
    StartUpdateCommand Command,
    string LocalZipPath,
    DateTime QueuedAt,
    /// <summary>Lower value = earlier position in the sequential install queue.</summary>
    int QueuePosition,
    /// <summary>When true, this update was sent in manual mode or has been downgraded after too many failures.</summary>
    bool IsManualInstall = false,
    /// <summary>How many times this package has failed to install. At <see cref="AgentOptions.Install.MaxAutoRetries"/> it is downgraded to manual.</summary>
    int FailCount = 0);

/// <summary>
/// Manages the ordered, sequential queue of pending updates.
///
/// Rules enforced here:
///   • Updates are processed strictly in QueuePosition order (FIFO per arrival).
///   • If an installation fails the queue is BLOCKED: no further installs run
///     until an operator calls Unblock() (with or without skipping the failed entry).
///   • This guarantees database migrations are never applied out of order.
/// </summary>
public class PendingInstallService(AgentOptions options, ILogger<PendingInstallService> logger)
{
    private static readonly JsonSerializerOptions SerializeOptions   = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly List<PendingUpdate> _queue = [];
    private readonly string _persistPath = Path.Combine(AppContext.BaseDirectory, "pending.json");
    private readonly Lock _lock = new();
    private int _nextPosition;

    // ── Blocked state ────────────────────────────────────────────────────────
    public bool IsBlocked { get; private set; }
    public string? BlockedReason { get; private set; }
    public Guid? BlockedByPackageId { get; private set; }

    // ── Install trigger (HTTP-initiated bypass of maintenance window) ────────
    private readonly System.Threading.Channels.Channel<Guid> _triggerChannel =
        System.Threading.Channels.Channel.CreateBounded<Guid>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            { FullMode = System.Threading.Channels.BoundedChannelFullMode.DropNewest });

    /// <summary>
    /// Signals the <see cref="ScheduledInstallWorker"/> to immediately install the specified
    /// queued package, bypassing the maintenance-window check.
    /// Returns <see langword="false"/> if a trigger is already pending (silently dropped).
    /// </summary>
    public bool TriggerImmediateInstall(Guid packageId) =>
        _triggerChannel.Writer.TryWrite(packageId);

    /// <summary>Waits until an immediate-install trigger arrives or <paramref name="ct"/> fires.</summary>
    public ValueTask<Guid> WaitForInstallTriggerAsync(CancellationToken ct) =>
        _triggerChannel.Reader.ReadAsync(ct);

    // ── Initialisation ───────────────────────────────────────────────────────

    /// <summary>Call once at startup to restore the queue from disk.</summary>
    public void LoadFromDisk()
    {
        if (!File.Exists(_persistPath)) return;
        try
        {
            var json = File.ReadAllText(_persistPath);
            var state = JsonSerializer.Deserialize<PersistentState>(json, DeserializeOptions);

            if (state is null) return;

            PersistentState? snapshotToSave = null;
            lock (_lock)
            {
                _queue.Clear();
                _queue.AddRange(state.Queue);
                _queue.Sort((a, b) => a.QueuePosition.CompareTo(b.QueuePosition));
                _nextPosition = _queue.Count > 0 ? _queue.Max(p => p.QueuePosition) + 1 : 0;
                IsBlocked = state.IsBlocked;
                BlockedReason = state.BlockedReason;
                BlockedByPackageId = state.BlockedByPackageId;

                // Remove entries whose zip files are no longer present on disk
                var missing = _queue.Where(p => !File.Exists(p.LocalZipPath)).ToList();
                foreach (var m in missing)
                {
                    logger.LogWarning("Removing pending update {PackageId} — zip not found at {Path}", m.PackageId, m.LocalZipPath);
                    _queue.Remove(m);
                }
                if (missing.Count > 0)
                    snapshotToSave = SnapshotState();
            }

            if (snapshotToSave is not null)
                SaveToDisk(snapshotToSave);

            logger.LogInformation("Restored {Count} pending update(s) from disk. Queue blocked={Blocked}", _queue.Count, IsBlocked);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load pending queue from {Path}", _persistPath);
        }
    }

    // ── Queue operations ─────────────────────────────────────────────────────

    /// <summary>Enqueue a downloaded + verified package for deferred installation.</summary>
    public void Enqueue(StartUpdateCommand command, string zipPath)
    {
        PersistentState snapshot;
        lock (_lock)
        {
            // Replace if same PackageId already present (re-queued after a transient error)
            _queue.RemoveAll(p => p.PackageId == command.PackageId);
            _queue.Add(new PendingUpdate(command.PackageId, command, zipPath, DateTime.UtcNow, _nextPosition++, command.IsManualInstall));
            _queue.Sort((a, b) => a.QueuePosition.CompareTo(b.QueuePosition));
            snapshot = SnapshotState();
        }

        SaveToDisk(snapshot);

        logger.LogInformation("Queued update {Component} {Version} (PackageId={PackageId}) at position {Pos}",
            command.Component, command.Version, command.PackageId, _nextPosition - 1);
    }

    /// <summary>Returns all queued entries in install order.</summary>
    public IReadOnlyList<PendingUpdate> GetAll()
    {
        lock (_lock) return _queue.ToList();
    }

    /// <summary>Returns the next entry to install (lowest QueuePosition), or null if queue is empty/blocked.</summary>
    public PendingUpdate? GetNext()
    {
        PendingUpdate? head;
        PendingUpdate? agentFirst  = null;
        PendingUpdate? serverFirst = null;

        lock (_lock)
        {
            if (IsBlocked || _queue.Count == 0) return null;
            head = _queue[0];

            // ── Agent-first: Agent self-update always takes absolute priority ──────────
            // This ensures the Agent is never blocked by a queued Server or Client update.
            // Rationale: the Agent must be up to date before it installs anything else.
            // File.Exists is intentionally done OUTSIDE the lock below.
            agentFirst = _queue.FirstOrDefault(p =>
                p.Command.Component.Equals("Agent", StringComparison.OrdinalIgnoreCase));

            // ── Server-before-Client: only evaluated when NO Agent update is pending ──
            // If the head is a Client package and a same-version Server package is also
            // queued (e.g. downloaded faster), return the Server entry first.
            if (agentFirst is null && head.Command.Component.Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                var version = head.Command.Version;
                serverFirst = _queue.FirstOrDefault(p =>
                    p.Command.Component.Equals("Server", StringComparison.OrdinalIgnoreCase) &&
                    p.Command.Version == version);
            }
        }

        // Perform file I/O and logging outside the lock to avoid blocking queue mutations.
        // TOCTOU note: agentFirst/serverFirst could be removed from the queue in the gap between
        // here and the caller's install attempt. That is acceptable: ScheduledInstallWorker
        // guards against missing zip files by calling File.Exists again before installing.

        // Agent wins over everything else in the queue.
        if (agentFirst is not null && File.Exists(agentFirst.LocalZipPath))
        {
            if (agentFirst.PackageId != head.PackageId)
                logger.LogInformation(
                    "Agent update v{AgentVersion} takes priority — deferring queued {Component} v{DeferredVersion}.",
                    agentFirst.Command.Version, head.Command.Component, head.Command.Version);
            return agentFirst;
        }

        if (serverFirst is not null && File.Exists(serverFirst.LocalZipPath))
        {
            logger.LogInformation(
                "Deferring Client {Version} install — Server {Version} must be installed first.",
                head.Command.Version, serverFirst.Command.Version);
            return serverFirst;
        }

        return head;
    }

    public PendingUpdate? GetByPackageId(Guid packageId)
    {
        lock (_lock) return _queue.FirstOrDefault(p => p.PackageId == packageId);
    }

    /// <summary>Remove an entry after successful installation (or explicit operator skip).</summary>
    public void Remove(Guid packageId)
    {
        PersistentState snapshot;
        lock (_lock)
        {
            _queue.RemoveAll(p => p.PackageId == packageId);
            snapshot = SnapshotState();
        }

        SaveToDisk(snapshot);
    }

    // ── Blocked state ────────────────────────────────────────────────────────

    /// <summary>
    /// Block the queue because an installation failed.
    /// Increments FailCount and downgrades to manual after MaxAutoRetries.
    /// Returns true if the package was downgraded from automatic to manual during this call.
    /// </summary>
    public bool Block(Guid packageId, string reason)
    {
        bool downgradedToManual = false;
        PersistentState snapshot;
        lock (_lock)
        {
            IsBlocked = true;
            BlockedReason = reason;
            BlockedByPackageId = packageId;

            // Increment FailCount; downgrade to manual at threshold.
            var idx = _queue.FindIndex(p => p.PackageId == packageId);
            if (idx >= 0)
            {
                var entry = _queue[idx];
                var newFail = entry.FailCount + 1;
                var maxRetries = options.Install.MaxAutoRetries;
                var becomeManual = !entry.IsManualInstall && maxRetries > 0 && newFail >= maxRetries;
                _queue[idx] = entry with { FailCount = newFail, IsManualInstall = entry.IsManualInstall || becomeManual };
                downgradedToManual = becomeManual;
            }

            snapshot = SnapshotState();
        }

        SaveToDisk(snapshot);

        logger.LogError("Install queue BLOCKED by failed update {PackageId}: {Reason}", packageId, reason);
        if (downgradedToManual)
            logger.LogWarning(
                "Package {PackageId} downgraded to MANUAL after {Max} consecutive failures — operator approval required to retry.",
                packageId, options.Install.MaxAutoRetries);

        return downgradedToManual;
    }

    /// <summary>
    /// Unblock the queue.
    /// If <paramref name="skipAndRemove"/> is true the blocking entry is also removed from the queue
    /// (operator explicitly skips the failed update, accepting migration risk).
    /// </summary>
    public void Unblock(bool skipAndRemove = false)
    {
        PersistentState snapshot;
        lock (_lock)
        {
            if (skipAndRemove && BlockedByPackageId.HasValue)
            {
                var skipped = _queue.FirstOrDefault(p => p.PackageId == BlockedByPackageId.Value);
                if (skipped is not null)
                {
                    logger.LogWarning("Skipping failed update {PackageId} ({Component} {Version}) by operator request",
                        skipped.PackageId, skipped.Command.Component, skipped.Command.Version);
                    _queue.Remove(skipped);
                    // Cleanup zip
                    try { if (File.Exists(skipped.LocalZipPath)) File.Delete(skipped.LocalZipPath); }
                    catch (Exception ex) { logger.LogWarning(ex, "Could not delete zip file {ZipPath} during queue skip", skipped.LocalZipPath); }
                }
            }

            IsBlocked = false;
            BlockedReason = null;
            BlockedByPackageId = null;
            snapshot = SnapshotState();
        }

        SaveToDisk(snapshot);

        logger.LogInformation("Install queue unblocked (skipAndRemove={Skip})", skipAndRemove);
    }

    // ── Maintenance window ───────────────────────────────────────────────────

    /// <summary>Returns true if the current local time falls inside any configured maintenance window.</summary>
    public bool IsInMaintenanceWindow()
    {
        if (options.MaintenanceWindows.Count == 0) return true; // no restrictions → always allowed

        var now = DateTime.Now;
        foreach (var window in options.MaintenanceWindows)
        {
            if (window.DaysOfWeek.Count > 0 && !window.DaysOfWeek.Contains(now.DayOfWeek))
                continue;

            if (!TimeOnly.TryParse(window.StartTime, out var start) ||
                !TimeOnly.TryParse(window.EndTime, out var end))
                continue;

            var current = TimeOnly.FromDateTime(now);

            // Overnight window (e.g. 23:00 → 01:00)
            if (start > end)
            {
                if (current >= start || current <= end) return true;
            }
            else
            {
                if (current >= start && current <= end) return true;
            }
        }

        return false;
    }

    /// <summary>Returns the next UTC instant when a maintenance window opens, or null if windows are empty.</summary>
    public DateTime? GetNextWindowStart()
    {
        if (options.MaintenanceWindows.Count == 0) return null;

        var now = DateTime.Now;
        var best = DateTime.MaxValue;

        for (var d = 0; d <= 7; d++)
        {
            var candidate = now.Date.AddDays(d);
            foreach (var window in options.MaintenanceWindows)
            {
                if (window.DaysOfWeek.Count > 0 && !window.DaysOfWeek.Contains(candidate.DayOfWeek))
                    continue;

                if (!TimeOnly.TryParse(window.StartTime, out var start)) continue;

                var windowStart = candidate.Add(start.ToTimeSpan());
                if (windowStart > now && windowStart < best)
                    best = windowStart;
            }
        }

        return best == DateTime.MaxValue ? null : best.ToUniversalTime();
    }

    // ── Persistence ──────────────────────────────────────────────────────────

    /// <summary>
    /// Captures a snapshot of the current queue state.
    /// <b>Must be called while holding <c>_lock</c>.</b>
    /// </summary>
    private PersistentState SnapshotState() =>
        new(_queue.ToList(), IsBlocked, BlockedReason, BlockedByPackageId);

    /// <summary>
    /// Serializes <paramref name="state"/> and atomically writes it to disk.
    /// <b>Must be called OUTSIDE <c>_lock</c></b> — file I/O should never block queue mutations.
    /// </summary>
    private void SaveToDisk(PersistentState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, SerializeOptions);
            // Write to a temp file first, then atomically rename to avoid corruption on crash.
            var tmpPath = _persistPath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _persistPath, overwrite: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist pending queue to {Path}", _persistPath);
        }
    }

    private record PersistentState(
        List<PendingUpdate> Queue,
        bool IsBlocked,
        string? BlockedReason,
        Guid? BlockedByPackageId);
}
