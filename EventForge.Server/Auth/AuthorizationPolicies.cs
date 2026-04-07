namespace EventForge.Server.Auth;

/// <summary>
/// Constants for authorization policy names.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy that requires SuperAdmin role.
    /// </summary>
    public const string RequireSuperAdmin = "RequireSuperAdmin";

    /// <summary>
    /// Policy that requires TenantAdmin role.
    /// </summary>
    public const string RequireTenantAdmin = "RequireTenantAdmin";

    /// <summary>
    /// Policy that requires at least Admin role (Admin or SuperAdmin).
    /// </summary>
    public const string RequireAdmin = "RequireAdmin";
}
