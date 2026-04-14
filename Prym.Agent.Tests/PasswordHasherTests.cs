using Prym.Agent.Security;

namespace Prym.Agent.Tests;

/// <summary>Tests for <see cref="PasswordHasher"/>.</summary>
public class PasswordHasherTests
{
    // ── IsHashed ────────────────────────────────────────────────────────────

    [Fact]
    public void IsHashed_NullOrEmpty_ReturnsFalse()
    {
        Assert.False(PasswordHasher.IsHashed(null!));
        Assert.False(PasswordHasher.IsHashed(string.Empty));
    }

    [Theory]
    [InlineData("plaintext")]
    [InlineData("Admin#123!")]
    [InlineData("v2:notourprefix")]
    [InlineData("V1:uppercase")]
    public void IsHashed_PlaintextOrWrongPrefix_ReturnsFalse(string value)
    {
        Assert.False(PasswordHasher.IsHashed(value));
    }

    [Fact]
    public void IsHashed_HashedValue_ReturnsTrue()
    {
        var hashed = PasswordHasher.Hash("anypassword");
        Assert.True(PasswordHasher.IsHashed(hashed));
        Assert.StartsWith("v1:", hashed);
    }

    // ── Hash ────────────────────────────────────────────────────────────────

    [Fact]
    public void Hash_ProducesV1PrefixedString()
    {
        var hash = PasswordHasher.Hash("secret");
        Assert.StartsWith("v1:", hash);
    }

    [Fact]
    public void Hash_DifferentCallsProduceDifferentHashes()
    {
        // Different salts each time → different encoded output
        var h1 = PasswordHasher.Hash("secret");
        var h2 = PasswordHasher.Hash("secret");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void Hash_ContainsTwoColonSeparatedBase64Parts()
    {
        // Format: "v1:{base64salt}:{base64hash}"
        var hash = PasswordHasher.Hash("secret");
        var rest = hash["v1:".Length..];
        var colon = rest.IndexOf(':');
        Assert.True(colon > 0, "Missing colon separator in hashed value");
        // Both parts must be valid base64
        Assert.True(TryParseBase64(rest[..colon]));
        Assert.True(TryParseBase64(rest[(colon + 1)..]));
    }

    // ── Verify ──────────────────────────────────────────────────────────────

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        const string pwd = "MyP@ssw0rd!";
        var hash = PasswordHasher.Hash(pwd);
        Assert.True(PasswordHasher.Verify(pwd, hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("correct");
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_EmptyStoredValue_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("anything", string.Empty));
        Assert.False(PasswordHasher.Verify("anything", null!));
    }

    [Fact]
    public void Verify_LegacyPlaintext_AcceptsExactMatch()
    {
        // Backward-compat: stored value is not hashed (legacy)
        Assert.True(PasswordHasher.Verify("Admin#123!", "Admin#123!"));
    }

    [Fact]
    public void Verify_LegacyPlaintext_RejectsMismatch()
    {
        Assert.False(PasswordHasher.Verify("wrong", "Admin#123!"));
    }

    [Fact]
    public void Verify_MalformedHashedValue_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("pwd", "v1:notbase64!@#$:alsonotbase64!@#$"));
    }

    [Fact]
    public void Verify_HashMissingColon_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("pwd", "v1:nocolonatall"));
    }

    [Fact]
    public void Verify_HashRoundTrip_ConsistentWithMultipleVerifications()
    {
        const string pwd = "round-trip-test";
        var hash = PasswordHasher.Hash(pwd);
        for (var i = 0; i < 3; i++)
            Assert.True(PasswordHasher.Verify(pwd, hash));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool TryParseBase64(string s)
    {
        try { Convert.FromBase64String(s); return true; }
        catch (FormatException) { return false; }
    }
}
