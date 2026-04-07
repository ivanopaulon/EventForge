namespace EventForge.Client.Services;

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
    Task ShowAsync(string title = "Caricamento...", string? operation = null, bool showProgress = false);

    /// <summary>
    /// Updates the current operation text
    /// </summary>
    Task UpdateOperationAsync(string operation);

    /// <summary>
    /// Updates the progress percentage (0-100)
    /// </summary>
    Task UpdateProgressAsync(double progress);

    /// <summary>
    /// Hides the loading dialog
    /// </summary>
    Task HideAsync();

    /// <summary>
    /// Gets the current state of the loading dialog
    /// </summary>
    LoadingDialogState GetCurrentState();
}

/// <summary>
/// Implementation of the loading dialog service
/// </summary>
public class LoadingDialogService : ILoadingDialogService
{
    private LoadingDialogState _currentState = new();

    public event Action<LoadingDialogState>? StateChanged;

    public async Task ShowAsync(string title = "Caricamento...", string? operation = null, bool showProgress = false)
    {
        _currentState = new LoadingDialogState
        {
            IsVisible = true,
            Title = title,
            CurrentOperation = operation ?? string.Empty,
            ShowProgress = showProgress,
            Progress = showProgress ? 0 : null
        };

        StateChanged?.Invoke(_currentState);
        await Task.CompletedTask;
    }

    public async Task UpdateOperationAsync(string operation)
    {
        if (_currentState.IsVisible)
        {
            _currentState.CurrentOperation = operation;
            StateChanged?.Invoke(_currentState);
        }
        await Task.CompletedTask;
    }

    public async Task UpdateProgressAsync(double progress)
    {
        if (_currentState.IsVisible && _currentState.ShowProgress)
        {
            _currentState.Progress = Math.Max(0, Math.Min(100, progress));
            StateChanged?.Invoke(_currentState);
        }
        await Task.CompletedTask;
    }

    public async Task HideAsync()
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
}

/// <summary>
/// State model for the loading dialog
/// </summary>
public record LoadingDialogState
{
    public bool IsVisible { get; set; } = false;
    public string Title { get; set; } = "Caricamento...";
    public string CurrentOperation { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string LogoAltText { get; set; } = "Logo";
    public bool ShowTotalTime { get; set; } = true;
    public bool ShowTaskTime { get; set; } = true;
    public bool ShowProgress { get; set; } = false;
    public double? Progress { get; set; }
}