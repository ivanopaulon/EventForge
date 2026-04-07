namespace Prym.Agent.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Truncates <paramref name="value"/> to at most <paramref name="maxLength"/> characters,
    /// appending "…" when the string is cut. Safe to call on null (returns empty string).
    /// </summary>
    public static string TruncateForDisplay(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }
}
