using Microsoft.AspNetCore.Components;

namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Component for entering notes in document row dialogs.
/// Provides character limit validation and counter display.
/// </summary>
public partial class DocumentRowNotesPanel : ComponentBase
{
    #region Parameters

    /// <summary>
    /// Current notes value
    /// </summary>
    [Parameter]
    public string? Notes { get; set; }

    /// <summary>
    /// Event triggered when notes value changes
    /// </summary>
    [Parameter]
    public EventCallback<string?> NotesChanged { get; set; }

    /// <summary>
    /// Maximum character length for notes (default: 200)
    /// </summary>
    [Parameter]
    public int MaxLength { get; set; } = 200;

    /// <summary>
    /// Disable notes input
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; } = false;

    #endregion

    #region Methods

    /// <summary>
    /// Handles notes value change with validation
    /// </summary>
    private async Task OnNotesChanged(string? value)
    {
        // Enforce max length
        if (!string.IsNullOrEmpty(value) && value.Length > MaxLength)
        {
            value = value.Substring(0, MaxLength);
        }

        // Update parent
        if (NotesChanged.HasDelegate)
        {
            await NotesChanged.InvokeAsync(value);
        }
    }

    #endregion
}
