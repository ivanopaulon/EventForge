using Syncfusion.Blazor.RichTextEditor;

namespace Prym.Web.Shared.Helpers;

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
    /// The first two entries are custom items (AttachFile, AddEmoji) handled via
    /// <see cref="Syncfusion.Blazor.RichTextEditor.RichTextEditorCustomToolbarItem"/>.
    /// </summary>
    public static readonly List<ToolbarItemModel> ToolbarItems =
    [
        new() { Name = "AttachFile", TooltipText = "Allega file" },
        new() { Name = "AddEmoji",   TooltipText = "Emoji" },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.Bold },
        new() { Command = ToolbarCommand.Italic },
        new() { Command = ToolbarCommand.Underline },
        new() { Command = ToolbarCommand.StrikeThrough },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.OrderedList },
        new() { Command = ToolbarCommand.UnorderedList },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.CreateLink },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.InlineCode },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.Undo },
        new() { Command = ToolbarCommand.Redo },
        new() { Command = ToolbarCommand.ClearFormat },
    ];
}
