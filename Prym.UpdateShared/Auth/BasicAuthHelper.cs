using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Prym.UpdateShared.Security;

namespace Prym.UpdateShared.Auth;

/// <summary>
/// Shared HTTP Basic Authentication helper used by both Prym.Agent and Prym.ManagementHub.
/// Provides a single timing-safe <see cref="TryAuthenticate"/> implementation so both
/// applications follow identical security rules.
/// </summary>
public static class BasicAuthHelper
{
    /// <summary>
    /// Parses an HTTP <c>Authorization: Basic …</c> header value and verifies the supplied
    /// credentials against the expected username and stored (possibly hashed) password.
    /// </summary>
    /// <param name="authorizationHeader">
    /// Raw value of the <c>Authorization</c> HTTP request header, or <see langword="null"/>
    /// / empty when the header is absent.
    /// </param>
    /// <param name="expectedUser">Expected username (plaintext).</param>
    /// <param name="storedPassword">
    /// Stored password value — either a <c>v1:…</c> PBKDF2 hash produced by
    /// <see cref="PasswordHasher.Hash"/> or a legacy plaintext value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when both username and password match; <see langword="false"/>
    /// in all other cases (missing header, wrong scheme, parse error, credential mismatch).
    /// </returns>
    /// <remarks>
    /// The implementation is designed to resist timing-based username enumeration:
    /// <list type="bullet">
    ///   <item>The username is compared with <see cref="CryptographicOperations.FixedTimeEquals"/>.</item>
    ///   <item>
    ///     <see cref="PasswordHasher.Verify"/> is <em>always</em> called even when the username
    ///     comparison fails, so that bad-username and bad-password responses take the same time.
    ///   </item>
    /// </list>
    /// </remarks>
    public static bool TryAuthenticate(string? authorizationHeader, string expectedUser, string storedPassword)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return false;

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(authorizationHeader);
            if (!string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
                return false;

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2) return false;

            // Timing-safe username comparison — prevents enumerating valid usernames via timing.
            var suppliedUserBytes = Encoding.UTF8.GetBytes(credentials[0]);
            var expectedUserBytes = Encoding.UTF8.GetBytes(expectedUser);
            var userOk = suppliedUserBytes.Length == expectedUserBytes.Length
                && CryptographicOperations.FixedTimeEquals(suppliedUserBytes, expectedUserBytes);

            // Always verify the password even when the username is wrong — this prevents a
            // timing oracle that would allow an attacker to enumerate valid usernames by
            // observing that bad-username responses arrive faster than bad-password responses.
            var passOk = PasswordHasher.Verify(credentials[1], storedPassword);

            return userOk && passOk;
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return false;
        }
    }
}
