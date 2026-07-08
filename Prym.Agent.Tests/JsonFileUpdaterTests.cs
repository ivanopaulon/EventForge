using Prym.Agent.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="JsonFileUpdater"/>.
/// Covers atomic write, missing-file creation, section merging, and sync/async variants.
/// </summary>
public class JsonFileUpdaterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public JsonFileUpdaterTests() => Directory.CreateDirectory(_dir);
    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string FilePath(string name = "test.json") => Path.Combine(_dir, name);

    // ── Sync ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_CreatesFileWhenMissing()
    {
        var path = FilePath();
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["Foo"] = "bar" });

        Assert.True(File.Exists(path));
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("bar", root["PrymAgent"]!["Foo"]!.GetValue<string>());
    }

    [Fact]
    public void Update_OverwritesExistingKey()
    {
        var path = FilePath();
        // Write initial value
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["ApiKey"] = "old" });
        // Overwrite
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["ApiKey"] = "new" });

        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("new", root["PrymAgent"]!["ApiKey"]!.GetValue<string>());
    }

    [Fact]
    public void Update_PreservesOtherKeysInSection()
    {
        var path = FilePath();
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["A"] = "1", ["B"] = "2" });
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["A"] = "99" });

        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("99", root["PrymAgent"]!["A"]!.GetValue<string>());
        Assert.Equal("2", root["PrymAgent"]!["B"]!.GetValue<string>());
    }

    [Fact]
    public void Update_PreservesOtherTopLevelSections()
    {
        var path = FilePath();
        // Pre-populate a different section
        File.WriteAllText(path, """{"OtherSection":{"X":"keep"}}""");

        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?> { ["Key"] = "value" });

        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("keep", root["OtherSection"]!["X"]!.GetValue<string>());
        Assert.Equal("value", root["PrymAgent"]!["Key"]!.GetValue<string>());
    }

    [Fact]
    public void Update_WritesValidJson()
    {
        var path = FilePath();
        JsonFileUpdater.Update(path, "PrymAgent", new Dictionary<string, JsonNode?>
        {
            ["ApiKey"] = "abc",
            ["InstallationId"] = Guid.NewGuid().ToString()
        });

        // Must be parseable without exception
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    // ── Async ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CreatesFileWhenMissing()
    {
        var path = FilePath("async.json");
        await JsonFileUpdater.UpdateAsync(path, "PrymAgent",
            new Dictionary<string, JsonNode?> { ["InstallationCode"] = "EF-TEST" });

        Assert.True(File.Exists(path));
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("EF-TEST", root["PrymAgent"]!["InstallationCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateAsync_OverwritesExistingKey()
    {
        var path = FilePath("async2.json");
        await JsonFileUpdater.UpdateAsync(path, "PrymAgent",
            new Dictionary<string, JsonNode?> { ["ApiKey"] = "v1" });
        await JsonFileUpdater.UpdateAsync(path, "PrymAgent",
            new Dictionary<string, JsonNode?> { ["ApiKey"] = "v2" });

        var root = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal("v2", root["PrymAgent"]!["ApiKey"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateAsync_NoTmpFileLeftAfterSuccess()
    {
        var path = FilePath("notmp.json");
        await JsonFileUpdater.UpdateAsync(path, "S", new Dictionary<string, JsonNode?> { ["K"] = "v" });

        Assert.False(File.Exists(path + ".tmp"), "Temp file should have been renamed/deleted.");
    }
}
