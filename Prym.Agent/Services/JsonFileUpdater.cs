using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prym.Agent.Services;

/// <summary>
/// Atomic JSON file updater: reads a JSON file, applies key/value updates under a named
/// section, and writes back atomically (temp file + rename) to prevent corruption on crash.
/// <para>
/// Used by <c>InstallationCodeGenerator</c> and <c>AgentWorker</c> to persist small sets of
/// identity/enrollment keys to <c>agent-identity.json</c> without duplicating the
/// read-update-write-atomic pattern.
/// </para>
/// </summary>
internal static class JsonFileUpdater
{
    private static readonly JsonSerializerOptions _writeIndentOpts = new() { WriteIndented = true };

    /// <summary>
    /// Reads <paramref name="filePath"/>, updates the specified <paramref name="sectionName"/>
    /// sub-object with the supplied key/value <paramref name="updates"/>, and writes back
    /// atomically via a temporary file + rename.  Creates the file if it does not exist.
    /// </summary>
    /// <param name="filePath">Absolute path to the JSON file to update.</param>
    /// <param name="sectionName">Top-level JSON property name of the section to update.</param>
    /// <param name="updates">Key/value pairs to set inside the section.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task UpdateAsync(
        string filePath,
        string sectionName,
        IReadOnlyDictionary<string, JsonNode?> updates,
        CancellationToken ct = default)
    {
        JsonNode root;
        if (File.Exists(filePath))
        {
            var text = await File.ReadAllTextAsync(filePath, ct);
            root = JsonNode.Parse(text) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var section = root[sectionName] as JsonObject ?? new JsonObject();
        foreach (var (key, value) in updates)
            section[key] = value;
        root[sectionName] = section;

        var tmpPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, root.ToJsonString(_writeIndentOpts), ct);
        File.Move(tmpPath, filePath, overwrite: true);
    }

    /// <summary>Synchronous overload of <see cref="UpdateAsync"/>.</summary>
    public static void Update(
        string filePath,
        string sectionName,
        IReadOnlyDictionary<string, JsonNode?> updates)
    {
        JsonNode root;
        if (File.Exists(filePath))
        {
            var text = File.ReadAllText(filePath);
            root = JsonNode.Parse(text) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var section = root[sectionName] as JsonObject ?? new JsonObject();
        foreach (var (key, value) in updates)
            section[key] = value;
        root[sectionName] = section;

        var tmpPath = filePath + ".tmp";
        File.WriteAllText(tmpPath, root.ToJsonString(_writeIndentOpts));
        File.Move(tmpPath, filePath, overwrite: true);
    }
}
