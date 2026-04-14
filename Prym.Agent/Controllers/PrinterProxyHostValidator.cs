namespace Prym.Agent.Controllers;

/// <summary>
/// Evaluates whether a host (IP address or hostname) is permitted by a configured
/// allowlist pattern list. Extracted for unit-testability.
/// </summary>
internal static class PrinterProxyHostValidator
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="host"/> matches at least one
    /// entry in <paramref name="patterns"/>.
    /// Supports exact matches and wildcard-affix patterns:
    /// <list type="bullet">
    ///   <item><c>*.example.com</c> — matches any subdomain of example.com (and example.com itself)</item>
    ///   <item><c>192.168.1.*</c> — matches any host in the 192.168.1.0/24 subnet (single trailing octet)</item>
    /// </list>
    /// </summary>
    internal static bool IsHostAllowed(string host, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                // *.example.com matches any subdomain of example.com AND example.com itself.
                // suffix = ".example.com", domain = "example.com"
                var suffix = pattern[1..]; // ".example.com"
                var domain = suffix[1..];  // "example.com"
                if (host.Equals(domain, StringComparison.OrdinalIgnoreCase))
                    return true; // exact: host IS example.com
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true; // subdomain: "sub.example.com" ends with ".example.com"
            }
            else if (pattern.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
            {
                // 192.168.1.* matches 192.168.1.100 but not 192.168.10.5
                var prefix = pattern[..^1]; // "192.168.1."
                if (host.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    !host[prefix.Length..].Contains('.')) // no further dots = single octet
                    return true;
            }
            else if (host.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
