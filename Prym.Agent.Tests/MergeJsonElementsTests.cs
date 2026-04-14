using System.Text.Json;
using Prym.Agent.Services;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="UpdateExecutorService.MergeJsonElements"/>.
/// The merge rule is: target values are preserved, template adds NEW keys only.
/// Nested objects are merged recursively; arrays and scalars in target are kept as-is.
/// </summary>
public class MergeJsonElementsTests
{
    // ── Target values take precedence ────────────────────────────────────────

    [Fact]
    public void ExistingKey_TargetValuePreserved()
    {
        var target   = Parse("""{ "key": "target-value" }""");
        var template = Parse("""{ "key": "template-value" }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Equal("target-value", result["key"]);
    }

    [Fact]
    public void ExistingNumericKey_TargetValuePreserved()
    {
        var target   = Parse("""{ "timeout": 30 }""");
        var template = Parse("""{ "timeout": 60 }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Equal(30, Convert.ToInt32(result["timeout"]));
    }

    // ── New keys from template are added ─────────────────────────────────────

    [Fact]
    public void NewKeyInTemplate_IsAdded()
    {
        var target   = Parse("""{ "existing": 1 }""");
        var template = Parse("""{ "existing": 0, "newKey": "from-template" }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Equal("from-template", result["newKey"]);
        Assert.Equal(1, Convert.ToInt32(result["existing"]));
    }

    [Fact]
    public void KeyOnlyInTarget_IsPreserved()
    {
        var target   = Parse("""{ "onlyInTarget": true }""");
        var template = Parse("""{ "otherKey": false }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.True(Convert.ToBoolean(result["onlyInTarget"]));
        // Template's otherKey is also added since it's new
        Assert.False(Convert.ToBoolean(result["otherKey"]));
    }

    // ── Nested object merging ────────────────────────────────────────────────

    [Fact]
    public void NestedObject_RecursivelyMerged()
    {
        var target   = Parse("""{ "section": { "keep": "mine", "num": 10 } }""");
        var template = Parse("""{ "section": { "keep": "default", "newProp": "added" } }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        var section = Assert.IsType<Dictionary<string, object?>>(result["section"]);
        Assert.Equal("mine", section["keep"]);
        Assert.Equal("added", section["newProp"]);
        Assert.Equal(10, Convert.ToInt32(section["num"]));
    }

    [Fact]
    public void DeepNestedObject_RecursivelyMerged()
    {
        var target   = Parse("""{ "a": { "b": { "keep": 1 } } }""");
        var template = Parse("""{ "a": { "b": { "keep": 99, "new": 2 } } }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        var a = Assert.IsType<Dictionary<string, object?>>(result["a"]);
        var b = Assert.IsType<Dictionary<string, object?>>(a["b"]);
        Assert.Equal(1, Convert.ToInt32(b["keep"]));
        Assert.Equal(2, Convert.ToInt32(b["new"]));
    }

    // ── Non-object template properties are not recursed into ─────────────────

    [Fact]
    public void ArrayValue_InTarget_IsPreservedAsIs()
    {
        var target   = Parse("""{ "items": [1, 2, 3] }""");
        var template = Parse("""{ "items": [4, 5] }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        var items = Assert.IsType<List<object?>>(result["items"]);
        Assert.Equal(3, items.Count); // original array kept
    }

    // ── Empty documents ───────────────────────────────────────────────────────

    [Fact]
    public void EmptyTarget_AllTemplateKeysAdded()
    {
        var target   = Parse("{}");
        var template = Parse("""{ "a": 1, "b": "two" }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, Convert.ToInt32(result["a"]));
        Assert.Equal("two", result["b"]);
    }

    [Fact]
    public void EmptyTemplate_OnlyTargetKeysPresent()
    {
        var target   = Parse("""{ "x": 42 }""");
        var template = Parse("{}");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Single(result);
        Assert.Equal(42, Convert.ToInt32(result["x"]));
    }

    // ── Null value handling ───────────────────────────────────────────────────

    [Fact]
    public void NullValueInTarget_IsPreserved()
    {
        var target   = Parse("""{ "key": null }""");
        var template = Parse("""{ "key": "default" }""");

        var result = UpdateExecutorService.MergeJsonElements(target, template);

        Assert.Null(result["key"]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;
}
