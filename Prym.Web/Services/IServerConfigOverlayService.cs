namespace Prym.Web.Services;

/// <summary>
/// Service for controlling the visibility of the server configuration overlay.
/// </summary>
public interface IServerConfigOverlayService
{
    /// <summary>
    /// Whether the server configuration overlay is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Fired whenever IsVisible changes so components can call StateHasChanged.
    /// </summary>
    event Action? OnVisibilityChanged;

    /// <summary>
    /// Shows the server configuration overlay.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the server configuration overlay.
    /// </summary>
    void Hide();
}
