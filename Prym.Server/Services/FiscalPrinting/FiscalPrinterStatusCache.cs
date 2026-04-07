using System.Collections.Concurrent;
using Prym.DTOs.FiscalPrinting;

namespace Prym.Server.Services.FiscalPrinting;

/// <summary>
/// Thread-safe in-memory cache for <see cref="FiscalPrinterStatus"/> snapshots.
/// Entries expire after <see cref="EntryLifetime"/> seconds (default: 15 s).
/// The cache is populated by <see cref="Prym.Server.HostedServices.FiscalPrinterMonitorService"/>
/// and read by the REST API (GET /api/v1/fiscal-printing/status/{printerId}).
/// </summary>
/// <remarks>
/// Registered as <b>Singleton</b> so the background service and the API controller
/// share the same in-memory state.
/// </remarks>
public sealed class FiscalPrinterStatusCache
{
    // -------------------------------------------------------------------------
    //  Configuration
    // -------------------------------------------------------------------------

    /// <summary>Duration after which a cached entry is considered stale (15 seconds).</summary>
    public static readonly TimeSpan EntryLifetime = TimeSpan.FromSeconds(15);

    // -------------------------------------------------------------------------
    //  Internal record
    // -------------------------------------------------------------------------

    private sealed record CacheEntry(FiscalPrinterStatus Status, DateTimeOffset Timestamp);

    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the cached <see cref="FiscalPrinterStatus"/> for the given printer,
    /// or <see langword="null"/> if the entry is absent or has expired.
    /// </summary>
    /// <param name="printerId">Printer unique identifier.</param>
    public FiscalPrinterStatus? GetCachedStatus(Guid printerId)
    {
        if (!_cache.TryGetValue(printerId, out var entry)) return null;
        if (DateTimeOffset.UtcNow - entry.Timestamp > EntryLifetime) return null;
        return entry.Status;
    }

    /// <summary>
    /// Stores or replaces the status for the given printer.
    /// Timestamps the entry with the current UTC time.
    /// </summary>
    /// <param name="printerId">Printer unique identifier.</param>
    /// <param name="status">Latest status snapshot from the printer.</param>
    public void UpdateStatus(Guid printerId, FiscalPrinterStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _cache[printerId] = new CacheEntry(status, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Returns all cached entries that have not yet expired, keyed by printer ID.
    /// </summary>
    public IReadOnlyDictionary<Guid, FiscalPrinterStatus> GetAllValidEntries()
    {
        var cutoff = DateTimeOffset.UtcNow - EntryLifetime;
        var result = new Dictionary<Guid, FiscalPrinterStatus>();

        foreach (var (key, entry) in _cache)
        {
            if (entry.Timestamp > cutoff)
                result[key] = entry.Status;
        }

        return result;
    }

    /// <summary>Removes all cached entries (primarily useful for testing).</summary>
    public void Clear() => _cache.Clear();
}
