using Prym.Agent.Controllers;

namespace Prym.Agent.Tests;

/// <summary>Tests for <see cref="PrinterProxyHostValidator.IsHostAllowed"/>.</summary>
public class PrinterProxyHostValidatorTests
{
    // ── Empty patterns — IsHostAllowed returns false; controller skips check ─

    [Fact]
    public void EmptyPatterns_IsHostAllowedReturnsFalse()
    {
        // IsHostAllowed itself returns false for empty patterns (no pattern matched).
        // The "allow all when no patterns configured" logic lives in the controller:
        //   if (allowedPatterns.Count > 0 && !IsHostAllowed(...)) → block
        // So empty patterns = controller skips the check = all hosts allowed at call site.
        Assert.False(PrinterProxyHostValidator.IsHostAllowed("192.168.1.1", []));
        Assert.False(PrinterProxyHostValidator.IsHostAllowed("evil.com", []));
    }

    // ── Exact match ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("192.168.1.100", "192.168.1.100")]
    [InlineData("printer.local", "printer.local")]
    [InlineData("PRINTER.LOCAL", "printer.local")]   // case-insensitive
    [InlineData("printer.local", "PRINTER.LOCAL")]
    public void ExactMatch_Allowed(string host, string pattern)
    {
        Assert.True(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    [Theory]
    [InlineData("192.168.1.100", "192.168.1.101")]
    [InlineData("printer.local", "otherprinter.local")]
    public void ExactMatch_Rejected(string host, string pattern)
    {
        Assert.False(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    // ── Wildcard suffix  *.domain ─────────────────────────────────────────────

    [Theory]
    [InlineData("sub.example.com", "*.example.com")]
    [InlineData("deep.sub.example.com", "*.example.com")]
    [InlineData("example.com", "*.example.com")]         // bare domain itself is accepted
    [InlineData("SUB.EXAMPLE.COM", "*.example.com")]     // case-insensitive
    public void WildcardSuffix_Allowed(string host, string pattern)
    {
        Assert.True(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    [Theory]
    [InlineData("notexample.com", "*.example.com")]   // no dot boundary
    [InlineData("evil-example.com", "*.example.com")]
    [InlineData("xexample.com", "*.example.com")]
    public void WildcardSuffix_Rejected(string host, string pattern)
    {
        Assert.False(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    // ── Wildcard prefix  prefix.* ──────────────────────────────────────────────

    [Theory]
    [InlineData("192.168.1.1", "192.168.1.*")]
    [InlineData("192.168.1.254", "192.168.1.*")]
    [InlineData("10.0.0.1", "10.0.0.*")]
    public void WildcardPrefix_Allowed(string host, string pattern)
    {
        Assert.True(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    [Theory]
    [InlineData("192.168.10.5", "192.168.1.*")]   // different third octet
    [InlineData("192.168.1.5.extra", "192.168.1.*")]   // extra octet after
    public void WildcardPrefix_Rejected(string host, string pattern)
    {
        Assert.False(PrinterProxyHostValidator.IsHostAllowed(host, [pattern]));
    }

    // ── Multiple patterns — OR semantics ─────────────────────────────────────

    [Fact]
    public void MultiplePatterns_MatchesFirstAllowedPattern()
    {
        var patterns = new List<string> { "10.0.0.*", "192.168.1.*", "printer.local" };
        Assert.True(PrinterProxyHostValidator.IsHostAllowed("192.168.1.50", patterns));
        Assert.True(PrinterProxyHostValidator.IsHostAllowed("printer.local", patterns));
        Assert.True(PrinterProxyHostValidator.IsHostAllowed("10.0.0.1", patterns));
    }

    [Fact]
    public void MultiplePatterns_RejectsHostNotMatchingAny()
    {
        var patterns = new List<string> { "10.0.0.*", "192.168.1.*" };
        Assert.False(PrinterProxyHostValidator.IsHostAllowed("172.16.0.1", patterns));
    }
}
