using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
{
    public async Task<AdminTenantResponseDto> AddTenantAdminAsync(Guid tenantId, Guid userId, AdminAccessLevel accessLevel, string reason, DateTime expiresAt)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di aggiunta admin tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required to grant admin access.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("ExpiresAt must be in the future.");
        }

        if (expiresAt > DateTime.UtcNow.AddDays(MaxAdminGrantDurationDays))
        {
            throw new ArgumentException($"ExpiresAt cannot exceed {MaxAdminGrantDurationDays} days from now.");
        }

        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            logger.LogWarning("Tenant {TenantId} non trovato per aggiunta admin.", tenantId);
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            logger.LogWarning("Utente {UserId} non trovato per aggiunta admin.", userId);
            throw new ArgumentException($"User {userId} not found.");
        }

        var existingMapping = await context.AdminTenants
            .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
        if (existingMapping is not null)
        {
            logger.LogWarning("Utente {UserId} gi� admin per tenant {TenantId}.", userId, tenantId);
            throw new InvalidOperationException($"User {userId} is already an admin for tenant {tenantId}.");
        }

        var adminTenant = new AdminTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ManagedTenantId = tenantId,
            AccessLevel = accessLevel,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Reason = reason
        };

        _ = context.AdminTenants.Add(adminTenant);
        _ = await context.SaveChangesAsync();

        // Audit log
        var currentUserId = tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenantId,
                OperationType = AuthAuditOperationType.AdminTenantGranted,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenantId,
                TargetUserId = userId,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    AccessLevel = accessLevel.ToString(),
                    adminTenant.GrantedAt,
                    adminTenant.ExpiresAt,
                    adminTenant.Reason
                }),
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();
        }

        return new AdminTenantResponseDto
        {
            Id = adminTenant.Id,
            UserId = userId,
            ManagedTenantId = tenantId,
            AccessLevel = accessLevel.ToString(),
            GrantedAt = adminTenant.GrantedAt,
            ExpiresAt = adminTenant.ExpiresAt,
            Reason = adminTenant.Reason,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            TenantName = tenant.Name
        };
    }

    public async Task RemoveTenantAdminAsync(Guid tenantId, Guid userId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di rimozione admin tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
        }

        var adminTenant = await context.AdminTenants
            .Include(at => at.User)
            .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
        if (adminTenant is null)
        {
            logger.LogWarning("Admin mapping non trovato per utente {UserId} e tenant {TenantId}.", userId, tenantId);
            throw new ArgumentException($"Admin mapping not found for user {userId} and tenant {tenantId}.");
        }

        // Create copy for audit purposes
        var originalAdminTenant = new AdminTenant
        {
            Id = adminTenant.Id,
            UserId = adminTenant.UserId,
            ManagedTenantId = adminTenant.ManagedTenantId,
            AccessLevel = adminTenant.AccessLevel,
            GrantedAt = adminTenant.GrantedAt,
            ExpiresAt = adminTenant.ExpiresAt,
            Reason = adminTenant.Reason,
            CreatedAt = adminTenant.CreatedAt,
            CreatedBy = adminTenant.CreatedBy,
            ModifiedAt = adminTenant.ModifiedAt,
            ModifiedBy = adminTenant.ModifiedBy
        };

        _ = context.AdminTenants.Remove(adminTenant);
        _ = await context.SaveChangesAsync();

        // Audit log
        var currentUserId = tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenantId,
                OperationType = AuthAuditOperationType.AdminTenantRevoked,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenantId,
                TargetUserId = userId,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    adminTenant.User.Username,
                    AccessLevel = originalAdminTenant.AccessLevel.ToString(),
                    originalAdminTenant.GrantedAt,
                    originalAdminTenant.ExpiresAt,
                    originalAdminTenant.Reason
                }),
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AdminTenantResponseDto>> GetTenantAdminsAsync(Guid tenantId)
    {
        var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            logger.LogWarning("Accesso negato alla lista admin per tenant {TenantId}.", tenantId);
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var adminTenants = await context.AdminTenants
            .AsNoTracking()
            .Include(at => at.User)
            .Include(at => at.ManagedTenant)
            .Where(at => at.ManagedTenantId == tenantId)
            .ToListAsync();

        return adminTenants.Select(at => new AdminTenantResponseDto
        {
            Id = at.Id,
            UserId = at.UserId,
            ManagedTenantId = at.ManagedTenantId,
            AccessLevel = at.AccessLevel.ToString(),
            GrantedAt = at.GrantedAt,
            ExpiresAt = at.ExpiresAt,
            Reason = at.Reason,
            Username = at.User.Username,
            Email = at.User.Email,
            FullName = at.User.FullName,
            TenantName = at.ManagedTenant.Name
        });
    }

    public async Task<IEnumerable<AdminTenantResponseDto>> GetAllAdminTenantsAsync()
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di accesso alla lista admin cross-tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can view all admin tenant grants.");
        }

        var adminTenants = await context.AdminTenants
            .AsNoTracking()
            .Include(at => at.User)
            .Include(at => at.ManagedTenant)
            .OrderBy(at => at.ExpiresAt == null ? 0 : 1)
            .ThenBy(at => at.ExpiresAt)
            .ToListAsync();

        return adminTenants.Select(at => new AdminTenantResponseDto
        {
            Id = at.Id,
            UserId = at.UserId,
            ManagedTenantId = at.ManagedTenantId,
            AccessLevel = at.AccessLevel.ToString(),
            GrantedAt = at.GrantedAt,
            ExpiresAt = at.ExpiresAt,
            Reason = at.Reason,
            Username = at.User.Username,
            Email = at.User.Email,
            FullName = at.User.FullName,
            TenantName = at.ManagedTenant.Name
        });
    }

    public async Task ForcePasswordChangeAsync(Guid userId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di forzare cambio password non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can force password changes.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            logger.LogWarning("Utente {UserId} non trovato per forzatura cambio password.", userId);
            throw new ArgumentException($"User {userId} not found.");
        }

        // Create copy for audit purposes
        var originalUser = new User
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            MustChangePassword = user.MustChangePassword,
            PasswordChangedAt = user.PasswordChangedAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockedUntil = user.LockedUntil,
            LastLoginAt = user.LastLoginAt,
            LastFailedLoginAt = user.LastFailedLoginAt,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            ModifiedAt = user.ModifiedAt,
            ModifiedBy = user.ModifiedBy
        };

        user.MustChangePassword = true;
        user.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync();

        // Audit log
        var currentUserId = tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = user.TenantId, // Now non-nullable
                OperationType = AuthAuditOperationType.ForcePasswordChange,
                PerformedByUserId = currentUserId.Value,
                TargetUserId = userId,
                Details = $"Password change forced",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();
        }
    }

    public async Task<PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>> GetAuditTrailAsync(
        Guid? tenantId = null,
        AuditOperationType? operationType = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di accesso all'audit trail senza permessi.");
            throw new UnauthorizedAccessException("Only super administrators can view audit trails.");
        }

        var query = context.AuditTrails
            .AsNoTracking()
            .Include(at => at.PerformedByUser)
            .Include(at => at.SourceTenant)
            .Include(at => at.TargetTenant)
            .Include(at => at.TargetUser)
            .AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(at => at.SourceTenantId == tenantId.Value || at.TargetTenantId == tenantId.Value);
        }

        if (operationType.HasValue)
        {
            query = query.Where(at => at.OperationType == operationType.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(at => at.PerformedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(at => new Prym.DTOs.SuperAdmin.AuditTrailResponseDto
            {
                Id = at.Id,
                OperationType = at.OperationType,
                PerformedByUserId = at.PerformedByUserId,
                PerformedByUsername = at.PerformedByUser.Username,
                SourceTenantId = at.SourceTenantId,
                SourceTenantName = at.SourceTenant != null ? at.SourceTenant.Name : null,
                TargetTenantId = at.TargetTenantId,
                TargetTenantName = at.TargetTenant != null ? at.TargetTenant.Name : null,
                TargetUserId = at.TargetUserId,
                TargetUsername = at.TargetUser != null ? at.TargetUser.Username : null,
                SessionId = at.SessionId,
                IpAddress = at.IpAddress,
                UserAgent = at.UserAgent,
                Details = at.Details ?? string.Empty,
                WasSuccessful = at.WasSuccessful,
                ErrorMessage = at.ErrorMessage,
                PerformedAt = at.PerformedAt
            })
            .ToListAsync();

        return new PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

}
