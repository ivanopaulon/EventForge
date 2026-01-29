namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Interface for managing tenant context in multi-tenant operations.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID for the request context.
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Gets the current user ID for the request context.
    /// </summary>
    Guid? CurrentUserId { get; }

    /// <summary>
    /// Indicates if the current user is a super administrator.
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Indicates if the current context is operating in impersonation mode.
    /// </summary>
    bool IsImpersonating { get; }

    /// <summary>
    /// Sets the tenant context for super admin operations.
    /// </summary>
    /// <param name="tenantId">The tenant ID to switch to</param>
    /// <param name="auditReason">Reason for the tenant switch</param>
    /// <param name="ct">Cancellation token</param>
    Task SetTenantContextAsync(Guid tenantId, string auditReason, CancellationToken ct = default);

    /// <summary>
    /// Starts impersonating a user (super admin only).
    /// </summary>
    /// <param name="userId">The user ID to impersonate</param>
    /// <param name="auditReason">Reason for impersonation</param>
    /// <param name="ct">Cancellation token</param>
    Task StartImpersonationAsync(Guid userId, string auditReason, CancellationToken ct = default);

    /// <summary>
    /// Ends impersonation and returns to original super admin context.
    /// </summary>
    /// <param name="auditReason">Reason for ending impersonation</param>
    /// <param name="ct">Cancellation token</param>
    Task EndImpersonationAsync(string auditReason, CancellationToken ct = default);

    /// <summary>
    /// Gets all tenants that the current super admin can manage.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task<IEnumerable<Guid>> GetManageableTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates if the current user can access the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate access for</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> CanAccessTenantAsync(Guid tenantId, CancellationToken ct = default);
}