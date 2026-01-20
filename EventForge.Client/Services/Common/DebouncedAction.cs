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

    public DebouncedAction(int delayMs = 500)
    {
        _delayMs = delayMs;
    }

    /// <summary>
    /// Debounces the provided action - executes only after delay with no new calls
    /// </summary>
    public void Debounce(Action action)
    {
        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delayMs);
            _timer.Elapsed += (s, e) =>
            {
                action.Invoke();
                Dispose();
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
        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delayMs);
            _timer.Elapsed += async (s, e) =>
            {
                await asyncAction.Invoke();
                Dispose();
            };
            _timer.AutoReset = false;
            _timer.Start();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
