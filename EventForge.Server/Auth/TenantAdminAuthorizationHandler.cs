using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Auth;

/// <summary>
/// Requirement for tenant admin authorization.
/// </summary>
public class TenantAdminRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Minimum required access level for this requirement.
    /// </summary>
    public AdminAccessLevel MinimumAccessLevel { get; }

    public TenantAdminRequirement(AdminAccessLevel minimumAccessLevel = AdminAccessLevel.TenantAdmin)
    {
        MinimumAccessLevel = minimumAccessLevel;
    }
}

/// <summary>
/// Authorization handler that validates tenant admin access.
/// Checks if the current user is a SuperAdmin (full access) or has appropriate 
/// tenant admin permissions for the current tenant.
/// </summary>
public class TenantAdminAuthorizationHandler : AuthorizationHandler<TenantAdminRequirement>
{
    private readonly ITenantContext _tenantContext;
    private readonly EventForgeDbContext _context;
    private readonly ILogger<TenantAdminAuthorizationHandler> _logger;

    public TenantAdminAuthorizationHandler(
        ITenantContext tenantContext,
        EventForgeDbContext context,
        ILogger<TenantAdminAuthorizationHandler> logger)
    {
        _tenantContext = tenantContext;
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAdminRequirement requirement)
    {
        // SuperAdmin has full access
        if (_tenantContext.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin access granted");
            context.Succeed(requirement);
            return;
        }

        // Check if user has tenant admin access
        var currentUserId = _tenantContext.CurrentUserId;
        var currentTenantId = _tenantContext.CurrentTenantId;

        if (!currentUserId.HasValue || !currentTenantId.HasValue)
        {
            _logger.LogWarning("Tenant admin authorization failed: Missing user or tenant ID");
            return;
        }

        // Query AdminTenants table to check if user has required access level
        var adminTenant = await _context.AdminTenants
            .Where(at => at.UserId == currentUserId.Value
                      && at.ManagedTenantId == currentTenantId.Value
                      && at.IsActive
                      && (!at.ExpiresAt.HasValue || at.ExpiresAt.Value > DateTime.UtcNow))
            .FirstOrDefaultAsync();

        if (adminTenant != null && adminTenant.AccessLevel >= requirement.MinimumAccessLevel)
        {
            _logger.LogDebug(
                "Tenant admin access granted for user {UserId} on tenant {TenantId} with access level {AccessLevel}",
                currentUserId.Value, currentTenantId.Value, adminTenant.AccessLevel);
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Tenant admin authorization failed for user {UserId} on tenant {TenantId}",
            currentUserId.Value, currentTenantId.Value);
    }
}
