using MudBlazor;

namespace EventForge.Client.Shared.Components.Dialogs;

/// <summary>
/// Standard dimensions and options for all EFDialog-based detail dialogs.
/// Use these constants to ensure visual consistency across all dialogs.
/// </summary>
public static class EFDialogDefaults
{
    /// <summary>
    /// MinWidth for simple form dialogs (no tabs).
    /// </summary>
    public const string MinWidthSimple = "min(860px, 95vw)";

    /// <summary>
    /// MinWidth for complex dialogs with vertical tabs.
    /// </summary>
    public const string MinWidthTabbed = "min(1120px, 95vw)";

    /// <summary>
    /// MinHeight for the scrollable body of tabbed dialogs.
    /// Keeps the dialog height stable when switching between tabs.
    /// </summary>
    public const string MinHeightTabbed = "520px";

    /// <summary>
    /// MinHeight for the scrollable body of simple (no-tab) dialogs.
    /// </summary>
    public const string MinHeightSimple = "260px";

    /// <summary>
    /// Standard DialogOptions for all detail dialogs opened from management pages.
    /// Prevents accidental close via backdrop click or MudBlazor's built-in close button
    /// (EFDialog handles its own close button with unsaved-changes guard).
    /// </summary>
    public static DialogOptions Options => new()
    {
        BackdropClick = false,
        CloseButton = false,
        Position = DialogPosition.Center,
    };
}
