using System.Timers;

namespace EventForge.Client.Services.Common;

/// <summary>
/// Utility class for debouncing actions (prevents excessive calls)
/// </summary>
public class DebouncedAction : IDisposable
{
    private readonly int _delayMs;
    private System.Timers.Timer? _timer;
    private readonly object _lock = new();
    private bool _disposed;

    public DebouncedAction(int delayMs = 500)
    {
        _delayMs = delayMs;
    }

    /// <summary>
    /// Debounces the provided action - executes only after delay with no new calls
    /// </summary>
    public void Debounce(Action action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delayMs);
            _timer.Elapsed += (s, e) =>
            {
                try
                {
                    action.Invoke();
                }
                finally
                {
                    // Clean up timer after execution without calling Dispose() to avoid race condition
                    lock (_lock)
                    {
                        _timer?.Stop();
                        _timer?.Dispose();
                        _timer = null;
                    }
                }
            };
            _timer.AutoReset = false;
            _timer.Start();
        }
    }

    /// <summary>
    /// Debounces the provided async action
    /// </summary>
    public void Debounce(Func<Task> asyncAction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delayMs);
            _timer.Elapsed += (s, e) =>
            {
                // Use Task.Run to properly handle async execution in event handler
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await asyncAction.Invoke();
                    }
                    finally
                    {
                        // Clean up timer after execution
                        lock (_lock)
                        {
                            _timer?.Stop();
                            _timer?.Dispose();
                            _timer = null;
                        }
                    }
                });
            };
            _timer.AutoReset = false;
            _timer.Start();
        }
    }

    /// <summary>
    /// Disposes the debouncer and any active timer
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    _timer?.Stop();
                    _timer?.Dispose();
                    _timer = null;
                }
            }
            _disposed = true;
        }
    }
}
