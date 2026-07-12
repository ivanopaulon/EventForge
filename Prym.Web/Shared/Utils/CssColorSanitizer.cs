using System.Text.RegularExpressions;

namespace Prym.Web.Shared.Utils;

/// <summary>
/// Validates admin-provided color strings (e.g. <c>FidelityTier.Color</c>) before they are
/// interpolated into inline CSS "style" attributes, to prevent CSS/HTML injection via crafted
/// color values (e.g. "red; } &lt;script&gt;...").
/// </summary>
public static partial class CssColorSanitizer
{
    [GeneratedRegex(@"^(#[0-9A-Fa-f]{3,8}|[A-Za-z]{1,30}|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(,\s*(0|1|0?\.\d+)\s*)?\))$")]
    private static partial Regex SafeColorPattern();

    /// <summary>
    /// Returns <paramref name="color"/> if it is a safe CSS color (hex, named color, or rgb/rgba),
    /// otherwise returns null.
    /// </summary>
    public static string? SanitizeOrNull(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        return SafeColorPattern().IsMatch(trimmed) ? trimmed : null;
    }
}
