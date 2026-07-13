using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
{
    public async Task<TenantResponseDto?> GetTenantAsync(Guid tenantId)
    {
        var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            logger.LogWarning("Accesso negato al tenant {TenantId}.", tenantId);
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant is not null ? TenantMapper.ToServerResponseDto(tenant) : null;
    }

    public async Task<IEnumerable<TenantResponseDto>> GetAllTenantsAsync()
    {
        if (!tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only super administrators can view all tenants.");

        var tenants = await context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return TenantMapper.ToServerResponseDtoCollection(tenants);
    }

    public async Task<TenantResponseDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto updateDto)
    {
        try
        {
            var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                logger.LogWarning("Accesso negato all'aggiornamento del tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                logger.LogWarning("Tenant {TenantId} non trovato per aggiornamento.", tenantId);
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            // Audit: copia originale
            var originalTenant = new Tenant
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                Domain = tenant.Domain,
                ContactEmail = tenant.ContactEmail,
                MaxUsers = tenant.MaxUsers,
                IsActive = tenant.IsActive,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                CreatedAt = tenant.CreatedAt,
                CreatedBy = tenant.CreatedBy,
                ModifiedAt = tenant.ModifiedAt,
                ModifiedBy = tenant.ModifiedBy
            };

            tenant.DisplayName = updateDto.DisplayName;
            tenant.Description = updateDto.Description;
            tenant.Domain = updateDto.Domain;
            tenant.ContactEmail = updateDto.ContactEmail;
            tenant.MaxUsers = updateDto.MaxUsers;
            tenant.SubscriptionExpiresAt = updateDto.SubscriptionExpiresAt;
            tenant.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Tenant {TenantId}.", tenantId);
                throw new InvalidOperationException("Il tenant è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log
            try
            {
                var currentUserId = tenantContext.CurrentUserId;
                if (currentUserId.HasValue)
                {
                    var auditTrail = new AuditTrail
                    {
                        TenantId = tenant.Id,
                        OperationType = AuthAuditOperationType.TenantUpdated,
                        PerformedByUserId = currentUserId.Value,
                        TargetTenantId = tenant.Id,
                        Details = $"Tenant aggiornato: {tenant.Name}",
                        WasSuccessful = true,
                        PerformedAt = DateTime.UtcNow
                    };
                    _ = context.AuditTrails.Add(auditTrail);
                    _ = await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante la scrittura dell'audit trail per l'aggiornamento tenant.");
            }

            return TenantMapper.ToServerResponseDto(tenant);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task SetTenantStatusAsync(Guid tenantId, bool isEnabled, string reason)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di cambio stato tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can change tenant status.");
        }

        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            logger.LogWarning("Tenant {TenantId} non trovato per cambio stato.", tenantId);
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        // Create copy for audit purposes
        var originalTenant = new Tenant
        {
            Id = tenant.Id,
            Name = tenant.Name,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsActive = tenant.IsActive,
            SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            ModifiedAt = tenant.ModifiedAt,
            ModifiedBy = tenant.ModifiedBy
        };

        tenant.IsActive = isEnabled;
        tenant.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync();

        // Audit log
        var currentUserId = tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenant.Id,
                OperationType = AuthAuditOperationType.TenantStatusChanged,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenant.Id,
                Details = $"Tenant {(isEnabled ? "enabled" : "disabled")}: {reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();
        }
    }

}
