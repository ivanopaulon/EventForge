using Syncfusion.Blazor.RichTextEditor;

namespace EventForge.Client.Shared.Helpers;

/// <summary>
/// Centralised toolbar configuration shared by <c>SfRichTextEditor</c> (new message composition)
/// and <c>SfInPlaceEditor</c> (inline message editing) so that both surfaces always present
/// an identical, consistent set of formatting options.
///
/// Intentionally excludes FontName, FontSize, FontColor, BackgroundColor, Print and FullScreen
/// — these are inappropriate for a chat context and add visual noise.
/// </summary>
public static class ChatEditorConfig
{
    /// <summary>
    /// Ordered list of toolbar items for the chat RTE / in-place editor.
    /// </summary>
    public static readonly List<ToolbarItemModel> ToolbarItems =
    [
        new() { Command = Command.Bold },
        new() { Command = Command.Italic },
        new() { Command = Command.Underline },
        new() { Command = Command.StrikeThrough },
        new() { Command = Command.Separator },
        new() { Command = Command.OrderedList },
        new() { Command = Command.UnorderedList },
        new() { Command = Command.Separator },
        new() { Command = Command.CreateLink },
        new() { Command = Command.Separator },
        new() { Command = Command.InlineCode },
        new() { Command = Command.Separator },
        new() { Command = Command.Undo },
        new() { Command = Command.Redo },
        new() { Command = Command.ClearFormat },
    ];
}
