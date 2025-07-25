using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant context management for multi-tenant operations.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EventForgeDbContext _context;

    // Session keys for tenant context
    private const string TenantIdSessionKey = "CurrentTenantId";
    private const string OriginalUserIdSessionKey = "OriginalUserId";
    private const string ImpersonatedUserIdSessionKey = "ImpersonatedUserId";
    private const string IsImpersonatingSessionKey = "IsImpersonating";

    public TenantContext(IHttpContextAccessor httpContextAccessor, EventForgeDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                var tenantIdString = httpContext.Session.GetString(TenantIdSessionKey);
                if (Guid.TryParse(tenantIdString, out var tenantId))
                {
                    return tenantId;
                }
            }

            // Fallback to user's tenant if not in session (normal operation)
            var userTenantId = httpContext?.User?.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(userTenantId, out var userTenant))
            {
                return userTenant;
            }

            return null;
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null && IsImpersonating)
            {
                var impersonatedUserIdString = httpContext.Session.GetString(ImpersonatedUserIdSessionKey);
                if (Guid.TryParse(impersonatedUserIdString, out var impersonatedUserId))
                {
                    return impersonatedUserId;
                }
            }

            // Normal user ID from JWT token
            var userIdString = httpContext?.User?.FindFirst("user_id")?.Value ??
                              httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }

            return null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.IsInRole("SuperAdmin") == true ||
                   httpContext?.User?.HasClaim("permission", "System.Admin.FullAccess") == true;
        }
    }

    public bool IsImpersonating
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                return httpContext.Session.GetString(IsImpersonatingSessionKey) == "true";
            }
            return false;
        }
    }

    public async Task SetTenantContextAsync(Guid tenantId, string auditReason)
    {
        if (!IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can switch tenant context.");
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            throw new InvalidOperationException("Session is not available for tenant switching.");
        }

        // Validate that the tenant exists and is accessible
        var canAccess = await CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var currentUserId = CurrentUserId;
        if (!currentUserId.HasValue)
        {
            throw new InvalidOperationException("Unable to determine current user ID.");
        }

        var currentTenantId = CurrentTenantId;

        // Set new tenant context in session
        httpContext.Session.SetString(TenantIdSessionKey, tenantId.ToString());

        // Create audit trail entry
        await CreateAuditTrailAsync(
            AuditOperationType.TenantSwitch,
            currentUserId.Value,
            currentTenantId,
            tenantId,
            null,
            auditReason);
    }

    public async Task StartImpersonationAsync(Guid userId, string auditReason)
    {
        if (!IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can impersonate users.");
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            throw new InvalidOperationException("Session is not available for impersonation.");
        }

        var currentUserId = CurrentUserId;
        if (!currentUserId.HasValue)
        {
            throw new InvalidOperationException("Unable to determine current user ID.");
        }

        // Validate that the target user exists
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (targetUser == null)
        {
            throw new ArgumentException($"User {userId} not found.");
        }

        // TODO: Add tenant validation - ensure user belongs to current tenant context
        
        // Store original user info and set impersonation
        httpContext.Session.SetString(OriginalUserIdSessionKey, currentUserId.Value.ToString());
        httpContext.Session.SetString(ImpersonatedUserIdSessionKey, userId.ToString());
        httpContext.Session.SetString(IsImpersonatingSessionKey, "true");

        // Create audit trail entry
        await CreateAuditTrailAsync(
            AuditOperationType.ImpersonationStart,
            currentUserId.Value,
            CurrentTenantId,
            targetUser.TenantId,
            userId,
            auditReason);
    }

    public async Task EndImpersonationAsync(string auditReason)
    {
        if (!IsImpersonating)
        {
            throw new InvalidOperationException("Not currently impersonating a user.");
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            throw new InvalidOperationException("Session is not available.");
        }

        var originalUserIdString = httpContext.Session.GetString(OriginalUserIdSessionKey);
        var impersonatedUserIdString = httpContext.Session.GetString(ImpersonatedUserIdSessionKey);

        if (!Guid.TryParse(originalUserIdString, out var originalUserId) ||
            !Guid.TryParse(impersonatedUserIdString, out var impersonatedUserId))
        {
            throw new InvalidOperationException("Invalid impersonation session state.");
        }

        // Create audit trail entry before clearing session
        await CreateAuditTrailAsync(
            AuditOperationType.ImpersonationEnd,
            originalUserId,
            CurrentTenantId,
            CurrentTenantId,
            impersonatedUserId,
            auditReason);

        // Clear impersonation session data
        httpContext.Session.Remove(OriginalUserIdSessionKey);
        httpContext.Session.Remove(ImpersonatedUserIdSessionKey);
        httpContext.Session.Remove(IsImpersonatingSessionKey);
    }

    public async Task<IEnumerable<Guid>> GetManageableTenantsAsync()
    {
        if (!IsSuperAdmin)
        {
            return Enumerable.Empty<Guid>();
        }

        var currentUserId = CurrentUserId;
        if (!currentUserId.HasValue)
        {
            return Enumerable.Empty<Guid>();
        }

        var adminTenants = await _context.AdminTenants
            .Where(at => at.UserId == currentUserId.Value && at.ManagedTenant.IsActive && !at.ManagedTenant.IsDeleted)
            .Select(at => at.ManagedTenantId)
            .ToListAsync();

        return adminTenants;
    }

    public async Task<bool> CanAccessTenantAsync(Guid tenantId)
    {
        if (!IsSuperAdmin)
        {
            return CurrentTenantId == tenantId;
        }

        var currentUserId = CurrentUserId;
        if (!currentUserId.HasValue)
        {
            return false;
        }

        // Super admins can access any tenant they have admin rights to
        var hasAccess = await _context.AdminTenants
            .AnyAsync(at => at.UserId == currentUserId.Value && 
                           at.ManagedTenantId == tenantId && 
                           at.ManagedTenant.IsActive && 
                           !at.ManagedTenant.IsDeleted);

        return hasAccess;
    }

    private async Task CreateAuditTrailAsync(
        AuditOperationType operationType,
        Guid performedByUserId,
        Guid? sourceTenantId,
        Guid? targetTenantId,
        Guid? targetUserId,
        string reason)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        var auditTrail = new AuditTrail
        {
            TenantId = sourceTenantId ?? Guid.Empty, // Use source tenant or empty for system operations
            OperationType = operationType,
            PerformedByUserId = performedByUserId,
            SourceTenantId = sourceTenantId,
            TargetTenantId = targetTenantId,
            TargetUserId = targetUserId,
            SessionId = httpContext?.Session?.Id,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
            Details = reason,
            WasSuccessful = true,
            PerformedAt = DateTime.UtcNow
        };

        _context.AuditTrails.Add(auditTrail);
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the operation
            // TODO: Add proper logging when ILogger is injected
            Console.WriteLine($"Failed to create audit trail: {ex.Message}");
        }
    }
}