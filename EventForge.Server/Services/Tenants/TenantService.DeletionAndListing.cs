using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
{
    public async Task SoftDeleteTenantAsync(Guid tenantId, string reason)
    {
        if (!tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only super administrators can delete tenants.");
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
            throw new ArgumentException($"Tenant {tenantId} not found.");

        if (tenant.IsDeleted)
            throw new InvalidOperationException("Tenant is already deleted.");

        tenant.IsDeleted = true;
        tenant.IsActive = false;
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
                Details = $"Tenant soft deleted: {reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all tenants with pagination (SuperAdmin only).
    /// </summary>
    public async Task<PagedResult<TenantResponseDto>> GetTenantsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        // NOTE: No tenant filter - SuperAdmin sees all tenants
        var query = context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(t => new TenantResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                DisplayName = t.DisplayName,
                Description = t.Description,
                Domain = t.Domain,
                ContactEmail = t.ContactEmail,
                MaxUsers = t.MaxUsers,
                IsActive = t.IsActive,
                SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.ModifiedAt ?? t.CreatedAt,
                CreatedBy = t.CreatedBy,
                ModifiedAt = t.ModifiedAt,
                ModifiedBy = t.ModifiedBy
            })
            .ToListAsync(ct);

        return new PagedResult<TenantResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets all active tenants with pagination (SuperAdmin only).
    /// </summary>
    public async Task<PagedResult<TenantResponseDto>> GetActiveTenantsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(t => new TenantResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                DisplayName = t.DisplayName,
                Description = t.Description,
                Domain = t.Domain,
                ContactEmail = t.ContactEmail,
                MaxUsers = t.MaxUsers,
                IsActive = t.IsActive,
                SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.ModifiedAt ?? t.CreatedAt,
                CreatedBy = t.CreatedBy,
                ModifiedAt = t.ModifiedAt,
                ModifiedBy = t.ModifiedBy
            })
            .ToListAsync(ct);

        return new PagedResult<TenantResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
