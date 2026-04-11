using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace EventForge.Client.Shared.Helpers;

/// <summary>
/// Renders the restricted markdown subset produced by <c>EnhancedMessageComposer</c>:
/// <c>**bold**</c>, <c>*italic*</c>, <c>`code`</c>, and <c>[label](url)</c> links.
/// All other HTML is escaped. The result is safe to bind to <see cref="MarkupString"/>.
/// </summary>
public static partial class ChatMarkdownRenderer
{
    // Source-generated regexes (C# 12 / .NET 8+) — zero startup overhead, no JIT compilation.
    [GeneratedRegex(@"\*\*(.+?)\*\*",                              RegexOptions.Singleline)] private static partial Regex BoldRegex();
    [GeneratedRegex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)",        RegexOptions.Singleline)] private static partial Regex ItalicRegex();
    [GeneratedRegex(@"`([^`]+)`")]                                  private static partial Regex CodeRegex();
    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^\s)]+)\)")]        private static partial Regex LinkRegex();

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
        encoded = LinkRegex().Replace(encoded, m =>
            $"<a href=\"{System.Net.WebUtility.HtmlEncode(m.Groups[2].Value)}\" target=\"_blank\" rel=\"noopener noreferrer\">" +
            $"{m.Groups[1].Value}</a>");

        encoded = CodeRegex().Replace(encoded, m =>
            $"<code style=\"background:rgba(0,0,0,.08);border-radius:3px;padding:1px 4px;font-size:.85em;\">{m.Groups[1].Value}</code>");

        encoded = BoldRegex().Replace(encoded, m => $"<strong>{m.Groups[1].Value}</strong>");
        encoded = ItalicRegex().Replace(encoded, m => $"<em>{m.Groups[1].Value}</em>");

        // 3. Convert newlines to <br> so multi-line messages render correctly.
        encoded = encoded.Replace("\r\n", "<br>").Replace("\n", "<br>");

        return new MarkupString(encoded);
    }
}
