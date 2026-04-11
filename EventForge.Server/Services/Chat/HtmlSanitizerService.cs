using Ganss.Xss;

namespace EventForge.Server.Services.Chat;

/// <summary>
/// Singleton HTML sanitizer configured to allow only the elements producible by
/// the <c>ChatEditorConfig</c> toolbar: bold, italic, underline, strikethrough,
/// ordered/unordered lists, hyperlinks, inline code/pre, and tables.
///
/// Anything outside this whitelist — including all event attributes, style
/// attributes, and script elements — is stripped before the content is persisted
/// or broadcast to other clients.
/// </summary>
public sealed class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        // Replace default allowed tags with the exact set the toolbar can produce.
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "b", "em", "i", "u", "s", "strike",
            "ul", "ol", "li",
            "a",
            "code", "pre",
            "table", "thead", "tbody", "tr", "td", "th",
            "span", "div"
        })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        // Allow only the href and target attributes (for links).
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");

        // Strip all inline styles — toolbar does not produce them.
        _sanitizer.AllowedCssProperties.Clear();

        // Allow only safe URI schemes in href.
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }

    /// <inheritdoc/>
    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
