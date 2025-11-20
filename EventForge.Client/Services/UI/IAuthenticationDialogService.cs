namespace EventForge.Client.Services.UI;

/// <summary>
/// Service for managing authentication dialogs across the application
/// </summary>
public interface IAuthenticationDialogService
{
    /// <summary>
    /// Shows the login dialog and returns true if authentication was successful
    /// </summary>
    Task<bool> ShowLoginDialogAsync();
}
