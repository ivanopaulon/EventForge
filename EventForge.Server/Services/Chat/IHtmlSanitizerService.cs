namespace EventForge.Server.Services.Chat;

/// <summary>
/// Sanitizes HTML produced by the Syncfusion RichTextEditor before persistence.
/// Always invoked server-side; the client is never trusted.
/// </summary>
public interface IHtmlSanitizerService
{
    /// <summary>
    /// Returns a sanitized copy of <paramref name="html"/> that contains only
    /// the tags and attributes produced by the chat toolbar whitelist.
    /// </summary>
    string Sanitize(string html);
}
