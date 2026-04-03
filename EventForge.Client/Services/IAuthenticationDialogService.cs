namespace EventForge.Client.Services;

/// <summary>
/// Service for managing authentication dialogs across the application
/// </summary>
public interface IAuthenticationDialogService
{
    /// <summary>
    /// Shows the login overlay and returns true if authentication was successful.
    /// Keeps the same signature as before for full backward compatibility.
    /// </summary>
    Task<bool> ShowLoginDialogAsync();

    // Internal event used by LoginOverlay to know when to show itself
    event Func<TaskCompletionSource<bool>, Task>? LoginRequested;
}
