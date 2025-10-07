using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthAuditOperationType = EventForge.DTOs.Common.AuditOperationType;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant context management for multi-tenant operations.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EventForgeDbContext _context;
    private readonly ILogger<TenantContext> _logger;

    // Session keys for tenant context
    private const string TenantIdSessionKey = "CurrentTenantId";
    private const string OriginalUserIdSessionKey = "OriginalUserId";
    private const string ImpersonatedUserIdSessionKey = "ImpersonatedUserId";
    private const string IsImpersonatingSessionKey = "IsImpersonating";

    public TenantContext(IHttpContextAccessor httpContextAccessor, EventForgeDbContext context, ILogger<TenantContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
        {
            if (!IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di cambio tenant non autorizzato da parte dell'utente {UserId}.", CurrentUserId);
                throw new UnauthorizedAccessException("Only super administrators can switch tenant context.");
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                _logger.LogError("Sessione non disponibile per il cambio tenant.");
                throw new InvalidOperationException("Session is not available for tenant switching.");
            }

            var canAccess = await CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                _logger.LogWarning("Accesso negato al tenant {TenantId} per l'utente {UserId}.", tenantId, CurrentUserId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var currentUserId = CurrentUserId;
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Impossibile determinare l'ID utente corrente durante il cambio tenant.");
                throw new InvalidOperationException("Unable to determine current user ID.");
            }

            var currentTenantId = CurrentTenantId;

            httpContext.Session.SetString(TenantIdSessionKey, tenantId.ToString());

            await CreateAuditTrailAsync(
                AuthAuditOperationType.TenantSwitch,
                currentUserId.Value,
                currentTenantId,
                tenantId,
                null,
                auditReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il cambio tenant.");
            throw;
        }
    }

    public async Task StartImpersonationAsync(Guid userId, string auditReason)
    {
        try
        {
            if (!IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di impersonificazione non autorizzato da parte dell'utente {UserId}.", CurrentUserId);
                throw new UnauthorizedAccessException("Only super administrators can impersonate users.");
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                _logger.LogError("Sessione non disponibile per impersonificazione.");
                throw new InvalidOperationException("Session is not available for impersonation.");
            }

            var currentUserId = CurrentUserId;
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Impossibile determinare l'ID utente corrente durante impersonificazione.");
                throw new InvalidOperationException("Unable to determine current user ID.");
            }

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato per impersonificazione.", userId);
                throw new ArgumentException($"User {userId} not found.");
            }

            // TODO: Add tenant validation - ensure user belongs to current tenant context

            httpContext.Session.SetString(OriginalUserIdSessionKey, currentUserId.Value.ToString());
            httpContext.Session.SetString(ImpersonatedUserIdSessionKey, userId.ToString());
            httpContext.Session.SetString(IsImpersonatingSessionKey, "true");

            await CreateAuditTrailAsync(
                AuthAuditOperationType.ImpersonationStart,
                currentUserId.Value,
                CurrentTenantId,
                targetUser.TenantId,
                userId,
                auditReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'inizio dell'impersonificazione.");
            throw;
        }
    }

    public async Task EndImpersonationAsync(string auditReason)
    {
        try
        {
            if (!IsImpersonating)
            {
                _logger.LogWarning("Tentativo di terminare impersonificazione quando non attiva.");
                throw new InvalidOperationException("Not currently impersonating a user.");
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session == null)
            {
                _logger.LogError("Sessione non disponibile per terminare impersonificazione.");
                throw new InvalidOperationException("Session is not available.");
            }

            var originalUserIdString = httpContext.Session.GetString(OriginalUserIdSessionKey);
            var impersonatedUserIdString = httpContext.Session.GetString(ImpersonatedUserIdSessionKey);

            if (!Guid.TryParse(originalUserIdString, out var originalUserId) ||
                !Guid.TryParse(impersonatedUserIdString, out var impersonatedUserId))
            {
                _logger.LogError("Stato sessione impersonificazione non valido.");
                throw new InvalidOperationException("Invalid impersonation session state.");
            }

            await CreateAuditTrailAsync(
                AuthAuditOperationType.ImpersonationEnd,
                originalUserId,
                CurrentTenantId,
                CurrentTenantId,
                impersonatedUserId,
                auditReason);

            httpContext.Session.Remove(OriginalUserIdSessionKey);
            httpContext.Session.Remove(ImpersonatedUserIdSessionKey);
            httpContext.Session.Remove(IsImpersonatingSessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la fine dell'impersonificazione.");
            throw;
        }
    }

    public async Task<IEnumerable<Guid>> GetManageableTenantsAsync()
    {
        try
        {
            if (!IsSuperAdmin)
            {
                _logger.LogWarning("Utente {UserId} ha tentato di accedere ai tenant gestibili senza permessi.", CurrentUserId);
                return Enumerable.Empty<Guid>();
            }

            var currentUserId = CurrentUserId;
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Impossibile determinare l'ID utente corrente per tenant gestibili.");
                return Enumerable.Empty<Guid>();
            }

            // First, check if SuperAdmin has specific AdminTenant entries
            var adminTenants = await _context.AdminTenants
                .Where(at => at.UserId == currentUserId.Value && at.ManagedTenant.IsActive && !at.ManagedTenant.IsDeleted)
                .Select(at => at.ManagedTenantId)
                .ToListAsync();

            // If SuperAdmin has specific tenant assignments, return those
            if (adminTenants.Any())
            {
                return adminTenants;
            }

            // If SuperAdmin has no specific assignments, they can access all active tenants
            var allTenants = await _context.Tenants
                .Where(t => t.IsActive && !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync();

            return allTenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei tenant gestibili.");
            throw;
        }
    }

    public async Task<bool> CanAccessTenantAsync(Guid tenantId)
    {
        try
        {
            if (!IsSuperAdmin)
            {
                return CurrentTenantId == tenantId;
            }

            var currentUserId = CurrentUserId;
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Impossibile determinare l'ID utente corrente per verifica accesso tenant.");
                return false;
            }

            // Check if SuperAdmin has specific tenant access defined
            var hasSpecificAccess = await _context.AdminTenants
                .AnyAsync(at => at.UserId == currentUserId.Value &&
                               at.ManagedTenantId == tenantId &&
                               at.ManagedTenant.IsActive &&
                               !at.ManagedTenant.IsDeleted);

            if (hasSpecificAccess)
            {
                return true;
            }

            // Check if SuperAdmin has any specific tenant assignments
            var hasAnySpecificAssignments = await _context.AdminTenants
                .AnyAsync(at => at.UserId == currentUserId.Value);

            // If SuperAdmin has no specific assignments, they can access all active tenants
            if (!hasAnySpecificAssignments)
            {
                var tenantExists = await _context.Tenants
                    .AnyAsync(t => t.Id == tenantId && t.IsActive && !t.IsDeleted);
                return tenantExists;
            }

            // SuperAdmin has specific assignments but not for this tenant
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la verifica di accesso al tenant {TenantId}.", tenantId);
            throw;
        }
    }

    private async Task CreateAuditTrailAsync(
        AuthAuditOperationType operationType,
        Guid performedByUserId,
        Guid? sourceTenantId,
        Guid? targetTenantId,
        Guid? targetUserId,
        string reason)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var auditTrail = new AuditTrail
        {
            TenantId = sourceTenantId ?? Guid.Empty,
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

        _ = _context.AuditTrails.Add(auditTrail);

        try
        {
            _ = await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossibile salvare l'audit trail per l'operazione {OperationType}.", operationType);
        }
    }
}