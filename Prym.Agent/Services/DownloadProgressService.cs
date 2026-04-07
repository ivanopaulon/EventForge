namespace Prym.Agent.Services;

/// <summary>
/// Singleton that tracks the current download operation for real-time display in the local UI.
/// Thread-safe via <see cref="Lock"/>.
/// </summary>
public class DownloadProgressService
{
    /// <summary>Snapshot of an active download, updated approximately every second.</summary>
    public sealed record DownloadSnapshot(
        Guid PackageId,
        string Component,
        string Version,
        long BytesDownloaded,
        long? TotalBytes,
        double SpeedBytesPerSecond,
        DateTime StartedAt)
    {
        public int? PercentComplete => TotalBytes > 0
            ? (int)Math.Round(BytesDownloaded * 100.0 / TotalBytes.Value)
            : null;

        public string FormattedDownloaded => FormatBytes(BytesDownloaded);
        public string FormattedTotal       => TotalBytes.HasValue ? FormatBytes(TotalBytes.Value) : "?";
        public string FormattedSpeed       => $"{FormatBytes((long)SpeedBytesPerSecond)}/s";

        public TimeSpan? Eta => TotalBytes.HasValue && SpeedBytesPerSecond > 0
            ? TimeSpan.FromSeconds((TotalBytes.Value - BytesDownloaded) / SpeedBytesPerSecond)
            : null;

        private static string FormatBytes(long bytes) => bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024         => $"{bytes / 1_024.0:F0} KB",
            _                => $"{bytes} B"
        };
    }

    private readonly Lock _lock = new();
    private DownloadSnapshot? _current;
    private long _lastReportedBytes;
    private DateTime _lastReportedAt;

    /// <summary>Returns the current active download snapshot, or null if no download is in progress.</summary>
    public DownloadSnapshot? Current { get { lock (_lock) return _current; } }

    /// <summary>Call when a download begins.</summary>
    public void Start(Guid packageId, string component, string version)
    {
        lock (_lock)
        {
            _lastReportedBytes = 0;
            _lastReportedAt    = DateTime.UtcNow;
            _current = new DownloadSnapshot(packageId, component, version,
                BytesDownloaded: 0, TotalBytes: null,
                SpeedBytesPerSecond: 0, StartedAt: DateTime.UtcNow);
        }
    }

    /// <summary>Call periodically during download with current byte counts.</summary>
    public void Update(Guid packageId, long bytesDownloaded, long? totalBytes)
    {
        lock (_lock)
        {
            if (_current is null || _current.PackageId != packageId) return;

            var now     = DateTime.UtcNow;
            var elapsed = (now - _lastReportedAt).TotalSeconds;
            var speed   = elapsed > 0
                ? (bytesDownloaded - _lastReportedBytes) / elapsed
                : _current.SpeedBytesPerSecond;

            _lastReportedBytes = bytesDownloaded;
            _lastReportedAt    = now;

            _current = _current with
            {
                BytesDownloaded      = bytesDownloaded,
                TotalBytes           = totalBytes,
                SpeedBytesPerSecond  = Math.Max(0, speed)
            };
        }
    }

    /// <summary>Call when the download completes successfully or fails.</summary>
    public void Complete(Guid packageId)
    {
        lock (_lock)
        {
            if (_current?.PackageId == packageId)
                _current = null;
        }
    }
}
