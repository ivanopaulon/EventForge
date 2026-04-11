using System.Security.Cryptography;

namespace Prym.Agent.Security;

/// <summary>
/// PBKDF2-SHA256 password hashing utility for the Agent UI credentials.
/// Stored format: <c>v1:{base64salt}:{base64hash}</c> (100 000 iterations, 32-byte key).
/// Legacy plaintext values (no <c>v1:</c> prefix) are accepted during verification
/// to allow a transparent migration when the admin next saves the Settings page.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int KeyLength  = 32; // 256 bits
    private const int SaltLength = 16; // 128 bits
    private const string Prefix  = "v1:";

    /// <summary>Returns <see langword="true"/> if <paramref name="stored"/> looks like a hashed value.</summary>
    public static bool IsHashed(string stored) =>
        !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>
    /// Hashes <paramref name="password"/> with a fresh random salt and returns the stored string.
    /// </summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"{Prefix}{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifies <paramref name="password"/> against <paramref name="stored"/>.
    /// Accepts both hashed (<c>v1:…</c>) and legacy plaintext values.
    /// </summary>
    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored))
            return false;

        // Legacy plaintext — transparent migration path until admin re-saves Settings.
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            return password == stored;

        var rest  = stored[Prefix.Length..];
        var colon = rest.IndexOf(':');
        if (colon < 0) return false;

        byte[] salt, expectedHash;
        try
        {
            salt         = Convert.FromBase64String(rest[..colon]);
            expectedHash = Convert.FromBase64String(rest[(colon + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
