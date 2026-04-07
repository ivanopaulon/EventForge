namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Admin access levels for tenant management.
    /// </summary>
    public enum AdminAccessLevel
    {
        /// <summary>
        /// Can view tenant information only.
        /// </summary>
        ReadOnly = 0,

        /// <summary>
        /// Can manage tenant users and settings.
        /// </summary>
        TenantAdmin = 1,

        /// <summary>
        /// Full administrative access to the tenant.
        /// </summary>
        FullAccess = 2
    }

    /// <summary>
    /// Types of audit operations for tracking system changes.
    /// </summary>
    public enum AuditOperationType
    {
        /// <summary>
        /// Super admin switched to a different tenant context.
        /// </summary>
        TenantSwitch = 0,

        /// <summary>
        /// Super admin started impersonating a user.
        /// </summary>
        ImpersonationStart = 1,

        /// <summary>
        /// Super admin ended impersonation and returned to original session.
        /// </summary>
        ImpersonationEnd = 2,

        /// <summary>
        /// Admin tenant access was granted to a user.
        /// </summary>
        AdminTenantGranted = 3,

        /// <summary>
        /// Admin tenant access was revoked from a user.
        /// </summary>
        AdminTenantRevoked = 4,

        /// <summary>
        /// Tenant was disabled/enabled.
        /// </summary>
        TenantStatusChanged = 5,

        /// <summary>
        /// Tenant was created.
        /// </summary>
        TenantCreated = 6,

        /// <summary>
        /// Tenant was updated.
        /// </summary>
        TenantUpdated = 7,

        /// <summary>
        /// Forced password change for a user.
        /// </summary>
        ForcePasswordChange = 8
    }
}