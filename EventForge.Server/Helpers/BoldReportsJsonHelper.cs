using System.Text.Json;

namespace EventForge.Server.Helpers;

/// <summary>
/// Utility for normalising the <c>Dictionary&lt;string, object&gt;</c> payload that arrives
/// from the Bold Reports JavaScript SDK.
///
/// <para>
/// When ASP.NET Core deserialises <c>[FromBody] Dictionary&lt;string, object&gt;</c> with the
/// default System.Text.Json binder, every value is left as a <see cref="JsonElement"/> instead
/// of a primitive type.  The <c>ReportDesignerHelper</c> and <c>ReportHelper</c> classes in
/// BoldReports.Net.Core expect plain .NET primitives (string, bool, int, long, double) and fail
/// silently — or throw internally — when they encounter <see cref="JsonElement"/> values.
/// </para>
/// <para>
/// Calling <see cref="NormaliseJsonElements"/> before passing the dictionary to any Bold Reports
/// helper converts every value recursively to its equivalent primitive.
/// </para>
/// </summary>
internal static class BoldReportsJsonHelper
{
    /// <summary>
    /// Returns a new dictionary where all <see cref="JsonElement"/> values (including those
    /// nested inside arrays or nested objects) have been replaced by equivalent CLR primitives.
    /// Non-<see cref="JsonElement"/> values are left as-is.
    /// </summary>
    internal static Dictionary<string, object> NormaliseJsonElements(Dictionary<string, object> source)
    {
        var result = new Dictionary<string, object>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in source)
            result[kvp.Key] = ConvertValue(kvp.Value);
        return result;
    }

    private static object ConvertValue(object value)
    {
        if (value is not JsonElement element)
            return value;

        return element.ValueKind switch
        {
            JsonValueKind.String  => element.GetString() ?? string.Empty,
            JsonValueKind.True    => true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => string.Empty,
            JsonValueKind.Number  => element.TryGetInt64(out var l)  ? (object)l
                                   : element.TryGetDouble(out var d) ? (object)d
                                   : element.GetRawText(),
            JsonValueKind.Array   => ConvertArray(element),
            JsonValueKind.Object  => ConvertObject(element),
            _                     => element.GetRawText(),
        };
    }

    private static object ConvertArray(JsonElement element)
    {
        var list = new List<object>();
        foreach (var item in element.EnumerateArray())
            list.Add(ConvertValue(item));
        return list;
    }

    private static Dictionary<string, object> ConvertObject(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
            dict[prop.Name] = ConvertValue(prop.Value);
        return dict;
    }
}
