using MudBlazor;

namespace Prym.Client.Shared.Components.Dialogs.SystemDialogs;

/// <summary>
/// Mutable state bag shared between <see cref="PageLoadingOverlay"/> (bridge)
/// and <see cref="PageLoadingSystemDialog"/> (content).
/// Call <see cref="Notify"/> after mutating to trigger a UI refresh.
/// </summary>
public sealed class PageLoadingState
{
    public enum AnimationType { Blink, Pulse, None }

    public string? Message { get; set; }
    public string IconPath { get; set; } = "trace.svg";
    public bool ShowProgressLog { get; set; }
    public IReadOnlyList<string>? ProgressMessages { get; set; }
    public bool ShowElapsedTime { get; set; }
    public int ProgressIntervalMs { get; set; } = 2500;
    public double? ProgressValue { get; set; }
    public Color ProgressColor { get; set; } = Color.Primary;
    public AnimationType Animation { get; set; } = AnimationType.Pulse;

    public event Action? Changed;

    internal void Notify() => Changed?.Invoke();
}
