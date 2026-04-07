namespace EventForge.Client.Services;

/// <summary>
/// Implementation of IServerConfigOverlayService.
/// Registered as a singleton so overlay state is shared across all components.
/// </summary>
public class ServerConfigOverlayService : IServerConfigOverlayService
{
    public bool IsVisible { get; private set; }

    public event Action? OnVisibilityChanged;

    public void Show()
    {
        if (!IsVisible)
        {
            IsVisible = true;
            OnVisibilityChanged?.Invoke();
        }
    }

    public void Hide()
    {
        if (IsVisible)
        {
            IsVisible = false;
            OnVisibilityChanged?.Invoke();
        }
    }
}
