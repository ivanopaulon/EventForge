using Prym.UpdateShared.Security;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="PasswordHasher"/> in <c>Prym.UpdateShared.Security</c>.
/// Covers the canonical shared implementation. <c>Prym.Agent.Security.PasswordHasher</c>
/// and <c>Prym.ManagementHub.Security.PasswordHasher</c> are thin forwarding wrappers
/// to this class, so testing it here covers all three.
/// </summary>
public class SharedPasswordHasherTests
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
        Assert.StartsWith("v1:", PasswordHasher.Hash("secret"));
    }

    [Fact]
    public void Hash_DifferentCallsProduceDifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.Hash("secret"), PasswordHasher.Hash("secret"));
    }

    // ── Verify ──────────────────────────────────────────────────────────────

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        const string pwd = "MyP@ssw0rd!";
        Assert.True(PasswordHasher.Verify(pwd, PasswordHasher.Hash(pwd)));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("wrong", PasswordHasher.Hash("correct")));
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
    public void Verify_ForwardingWrapperIsConsistentWithSharedImpl()
    {
        // Verify that the Agent's forwarding wrapper produces the same result as the shared implementation.
        const string pwd = "ForwardingTest!";
        var hash = PasswordHasher.Hash(pwd);
        // Agent forwarding wrapper
        Assert.True(Prym.Agent.Security.PasswordHasher.Verify(pwd, hash));
        Assert.False(Prym.Agent.Security.PasswordHasher.Verify("wrong", hash));
        Assert.True(Prym.Agent.Security.PasswordHasher.IsHashed(hash));
    }
}
