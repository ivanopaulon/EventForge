using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace EventForge.Client.Shared.Helpers;

/// <summary>
/// Renders the restricted markdown subset produced by <c>EnhancedMessageComposer</c>:
/// <c>**bold**</c>, <c>*italic*</c>, <c>`code`</c>, and <c>[label](url)</c> links.
/// All other HTML is escaped. The result is safe to bind to <see cref="MarkupString"/>.
/// </summary>
public static class ChatMarkdownRenderer
{
    // Order matters: bold before italic so ** isn't confused with two separate *.
    private static readonly Regex Bold   = new(@"\*\*(.+?)\*\*",                              RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Italic = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)",        RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Code   = new(@"`([^`]+)`",                                  RegexOptions.Compiled);
    private static readonly Regex Link   = new(@"\[([^\]]+)\]\((https?://[^\s)]+)\)",         RegexOptions.Compiled);

    /// <summary>
    /// Converts a chat message string to a safe <see cref="MarkupString"/>.
    /// </summary>
    public static MarkupString Render(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new MarkupString(string.Empty);

        // 1. HTML-encode the raw text first to prevent injection.
        var encoded = System.Net.WebUtility.HtmlEncode(text);

        // 2. Apply markdown transforms on the encoded string.
        //    The delimiter characters (*, `, [, ]) are plain ASCII and are not affected
        //    by HtmlEncode, so the regex patterns work correctly on the encoded string.
        encoded = Link.Replace(encoded, m =>
            $"<a href=\"{System.Net.WebUtility.HtmlEncode(m.Groups[2].Value)}\" target=\"_blank\" rel=\"noopener noreferrer\">" +
            $"{m.Groups[1].Value}</a>");

        encoded = Code.Replace(encoded, m =>
            $"<code style=\"background:rgba(0,0,0,.08);border-radius:3px;padding:1px 4px;font-size:.85em;\">{m.Groups[1].Value}</code>");

        encoded = Bold.Replace(encoded, m => $"<strong>{m.Groups[1].Value}</strong>");
        encoded = Italic.Replace(encoded, m => $"<em>{m.Groups[1].Value}</em>");

        // 3. Convert newlines to <br> so multi-line messages render correctly.
        encoded = encoded.Replace("\r\n", "<br>").Replace("\n", "<br>");

        return new MarkupString(encoded);
    }
}
