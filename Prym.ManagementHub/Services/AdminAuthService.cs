using System.Security.Cryptography;
using System.Text;

namespace Prym.ManagementHub.Services;

/// <summary>
/// Centralised admin-key authentication helper.
/// Both <c>EnrollmentsController</c> and <c>InstallationsController</c> delegate to this
/// service so the timing-safe comparison logic lives in exactly one place.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Returns <see langword="true"/> when the <c>X-Admin-Key</c> header present in
    /// <paramref name="headers"/> matches the configured <see cref="ManagementHubOptions.AdminApiKey"/>
    /// using a constant-time comparison to prevent timing attacks.
    /// </summary>
    bool IsAuthorized(IHeaderDictionary headers);
}

/// <inheritdoc />
public class AdminAuthService(ManagementHubOptions hubOptions) : IAdminAuthService
{
    public bool IsAuthorized(IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(hubOptions.AdminApiKey))
            return false;

        headers.TryGetValue("X-Admin-Key", out var headerValues);
        var supplied = headerValues.ToString();

        if (string.IsNullOrEmpty(supplied))
            return false;

        var expected = Encoding.UTF8.GetBytes(hubOptions.AdminApiKey);
        var actual   = Encoding.UTF8.GetBytes(supplied);

        // FixedTimeEquals requires equal-length arrays; pad/truncate is intentionally avoided —
        // different lengths already reveal a mismatch, so we use a constant-time compare only
        // when lengths are equal, and reject immediately when they differ.
        return expected.Length == actual.Length
            && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
