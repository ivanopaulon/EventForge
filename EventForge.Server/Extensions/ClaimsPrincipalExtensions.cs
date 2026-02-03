using System.Security.Claims;

namespace EventForge.Server.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information from JWT claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the username from claims.
    /// </summary>
    public static string? Username(this ClaimsPrincipal principal)
    {
        if (principal == null) return null;
        return principal.FindFirst("username")?.Value 
            ?? principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the email from claims.
    /// </summary>
    public static string? Email(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the first name from claims.
    /// </summary>
    public static string? FirstName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.GivenName)?.Value;
    }

    /// <summary>
    /// Gets the last name from claims.
    /// </summary>
    public static string? LastName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Surname)?.Value;
    }

    /// <summary>
    /// Gets the tenant ID from claims.
    /// </summary>
    public static Guid? TenantId(this ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal?.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Gets the user ID from claims.
    /// </summary>
    public static Guid? UserId(this ClaimsPrincipal principal)
    {
        if (principal == null) return null;
        var userIdClaim = principal.FindFirst("user_id")?.Value 
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the tenant code from claims.
    /// </summary>
    public static string? TenantCode(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("tenant_code")?.Value;
    }

    /// <summary>
    /// Gets the full name from claims.
    /// </summary>
    public static string? FullName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("full_name")?.Value;
    }
}
