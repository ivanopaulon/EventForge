namespace Prym.Web.Services;

/// <summary>
/// Service for managing the global loading dialog component
/// </summary>
public interface ILoadingDialogService
{
    /// <summary>
    /// Event triggered when dialog state changes
    /// </summary>
    event Action<LoadingDialogState>? StateChanged;

    /// <summary>
    /// Shows the loading dialog with specified options
    /// </summary>
    Task ShowAsync(string title = "Caricamento...", string? operation = null, bool showProgress = false,
        IReadOnlyList<string>? progressMessages = null, bool showElapsedTime = false, CancellationToken ct = default);

    /// <summary>
    /// Updates the current operation text
    /// </summary>
    Task UpdateOperationAsync(string operation, CancellationToken ct = default);

    /// <summary>
    /// Updates the progress percentage (0-100)
    /// </summary>
    Task UpdateProgressAsync(double progress, CancellationToken ct = default);

    /// <summary>
    /// Hides the loading dialog
    /// </summary>
    Task HideAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current state of the loading dialog
    /// </summary>
    LoadingDialogState GetCurrentState();

    /// <summary>
    /// Begins a multi-step loading operation whose overlay only becomes visible if the
    /// operation is still running after <paramref name="delayMsBeforeShow"/> milliseconds,
    /// avoiding flicker on fast (sub-threshold) loads. Update <see cref="CurrentOperation"/>
    /// text via <see cref="UpdateOperationAsync"/> for each step, then dispose the returned
    /// scope (e.g. via <c>await using</c>) to hide the overlay when the operation completes.
    /// </summary>
    Task<IAsyncDisposable> BeginOperationAsync(string title, int delayMsBeforeShow = 200, CancellationToken ct = default);
}

/// <summary>
/// Implementation of the loading dialog service
/// </summary>
public class LoadingDialogService : ILoadingDialogService
{
    private LoadingDialogState _currentState = new();

    public event Action<LoadingDialogState>? StateChanged;

    public async Task ShowAsync(string title = "Caricamento...", string? operation = null, bool showProgress = false,
        IReadOnlyList<string>? progressMessages = null, bool showElapsedTime = false, CancellationToken ct = default)
    {
        _currentState = new LoadingDialogState
        {
            IsVisible = true,
            Title = title,
            CurrentOperation = operation ?? string.Empty,
            ShowProgress = showProgress,
            Progress = showProgress ? 0 : null,
            ProgressMessages = progressMessages,
            ShowElapsedTime = showElapsedTime
        };

        StateChanged?.Invoke(_currentState);
        await Task.CompletedTask;
    }

    public async Task UpdateOperationAsync(string operation, CancellationToken ct = default)
    {
        if (_currentState.IsVisible)
        {
            _currentState.CurrentOperation = operation;
            StateChanged?.Invoke(_currentState);
        }
        await Task.CompletedTask;
    }

    public async Task UpdateProgressAsync(double progress, CancellationToken ct = default)
    {
        if (_currentState.IsVisible && _currentState.ShowProgress)
        {
            _currentState.Progress = Math.Max(0, Math.Min(100, progress));
            StateChanged?.Invoke(_currentState);
        }
        await Task.CompletedTask;
    }

    public async Task HideAsync(CancellationToken ct = default)
    {
        if (_currentState.IsVisible)
        {
            _currentState.IsVisible = false;
            StateChanged?.Invoke(_currentState);
        }
        await Task.CompletedTask;
    }

    public LoadingDialogState GetCurrentState()
    {
        return _currentState with { }; // Return a copy
    }

    public Task<IAsyncDisposable> BeginOperationAsync(string title, int delayMsBeforeShow = 200, CancellationToken ct = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _currentState = new LoadingDialogState
        {
            IsVisible = false,
            Title = title,
            CurrentOperation = string.Empty
        };

        var delayTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Math.Max(0, delayMsBeforeShow), cts.Token);
                _currentState.IsVisible = true;
                StateChanged?.Invoke(_currentState);
            }
            catch (OperationCanceledException)
            {
                // Operation completed before the delay elapsed — overlay never shown, no flicker.
            }
        }, CancellationToken.None);

        return Task.FromResult<IAsyncDisposable>(new LoadingOperationScope(this, cts, delayTask));
    }

    /// <summary>
    /// Scope returned by <see cref="BeginOperationAsync"/>. Disposing it cancels the pending
    /// delayed show (if not yet elapsed) and hides the overlay (if it was shown).
    /// </summary>
    private sealed class LoadingOperationScope(LoadingDialogService owner, CancellationTokenSource cts, Task delayTask) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            cts.Cancel();
            try
            {
                await delayTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the delay was cancelled before elapsing (fast operation, overlay never shown).
            }
            finally
            {
                cts.Dispose();
            }

            await owner.HideAsync();
        }
    }
}

/// <summary>
/// State model for the loading dialog
/// </summary>
public record LoadingDialogState
{
    public bool IsVisible { get; set; } = false;
    public string Title { get; set; } = "Caricamento...";
    public string CurrentOperation { get; set; } = string.Empty;
    public bool ShowProgress { get; set; } = false;
    public double? Progress { get; set; }
    public IReadOnlyList<string>? ProgressMessages { get; set; }
    public bool ShowElapsedTime { get; set; } = false;
}