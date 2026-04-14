using System.Text.RegularExpressions;

namespace Prym.ManagementHub.Utilities;

/// <summary>
/// File-system utility helpers shared across controllers, services, and Razor Pages.
/// </summary>
public static partial class FileNameHelper
{
    /// <summary>
    /// Removes characters from a user-supplied version string that are not safe for use in
    /// file names. Allows alphanumeric characters, dots, hyphens, and underscores only.
    /// </summary>
    public static string SanitizeForFileName(string input) =>
        UnsafeCharsRegex().Replace(input, "_");

    [GeneratedRegex(@"[^a-zA-Z0-9.\-_]")]
    private static partial Regex UnsafeCharsRegex();
}
