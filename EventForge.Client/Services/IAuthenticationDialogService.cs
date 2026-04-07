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

    // Event used by LoginOverlay to receive login requests; subscribe in overlay components only
    event Func<TaskCompletionSource<bool>, Task>? LoginRequested;
}
