using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
{
    public async Task<TenantStatisticsDto> GetTenantStatisticsAsync()
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant statistics.");
        }
        var totalTenants = await context.Tenants.AsNoTracking().CountAsync();
        var activeTenants = await context.Tenants.AsNoTracking().CountAsync(t => t.IsActive);
        var inactiveTenants = totalTenants - activeTenants;

        var totalUsers = await context.Users.AsNoTracking().CountAsync();
        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        var usersLastMonth = await context.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= oneMonthAgo);

        // Batch load user counts per tenant to avoid correlated subquery N+1
        var userCountsByTenant = await context.Users
            .AsNoTracking()
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var activeTenantsForLimit = await context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.MaxUsers })
            .ToListAsync();

        var tenantsNearLimit = activeTenantsForLimit
            .Count(t => userCountsByTenant.TryGetValue(t.Id, out var count) && count >= t.MaxUsers * 0.9);

        return new TenantStatisticsDto
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            InactiveTenants = inactiveTenants,
            TotalUsers = totalUsers,
            UsersLastMonth = usersLastMonth,
            TenantsNearLimit = tenantsNearLimit
        };
    }

    public async Task<PagedResult<TenantResponseDto>> SearchTenantsAsync(TenantSearchDto searchDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can search tenants.");
        }
        var query = context.Tenants.AsNoTracking().AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchDto.SearchTerm))
        {
            var term = searchDto.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.DisplayName.ToLower().Contains(term) ||
                (t.Domain != null && t.Domain.ToLower().Contains(term)));
        }

        if (!string.IsNullOrEmpty(searchDto.Status) && searchDto.Status != "all")
        {
            var isActive = searchDto.Status == "active";
            query = query.Where(t => t.IsActive == isActive);
        }

        if (searchDto.MaxUsers.HasValue)
        {
            query = query.Where(t => t.MaxUsers <= searchDto.MaxUsers.Value);
        }

        if (searchDto.CreatedAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= searchDto.CreatedAfter.Value);
        }

        if (searchDto.CreatedBefore.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= searchDto.CreatedBefore.Value);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(searchDto.SortBy))
        {
            var isDesc = searchDto.SortOrder?.ToLower() == "desc";
            query = searchDto.SortBy.ToLower() switch
            {
                "name" => isDesc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "displayname" => isDesc ? query.OrderByDescending(t => t.DisplayName) : query.OrderBy(t => t.DisplayName),
                "createdat" => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                "maxusers" => isDesc ? query.OrderByDescending(t => t.MaxUsers) : query.OrderBy(t => t.MaxUsers),
                _ => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        var totalCount = await query.CountAsync();
        var tenants = await query
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync();

        // Apply NearUserLimit filter in-memory after fetching to avoid correlated subquery N+1
        IEnumerable<Tenant> filteredTenants = tenants;
        if (searchDto.NearUserLimit.HasValue && searchDto.NearUserLimit.Value)
        {
            var tenantIds = tenants.Select(t => t.Id).ToList();
            var userCountsByTenant = await context.Users
                .AsNoTracking()
                .Where(u => tenantIds.Contains(u.TenantId))
                .GroupBy(u => u.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

            filteredTenants = tenants
                .Where(t => userCountsByTenant.TryGetValue(t.Id, out var count) && count >= t.MaxUsers * 0.9);
        }

        var tenantDtos = filteredTenants.Select(t => new TenantResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            DisplayName = t.DisplayName,
            Description = t.Description,
            Domain = t.Domain,
            ContactEmail = t.ContactEmail,
            MaxUsers = t.MaxUsers,
            IsActive = t.IsActive,
            SubscriptionExpiresAt = t.SubscriptionExpiresAt,
            CreatedAt = t.CreatedAt,
            CreatedBy = t.CreatedBy,
            ModifiedAt = t.ModifiedAt,
            ModifiedBy = t.ModifiedBy
        }).ToList();

        return new PagedResult<TenantResponseDto>
        {
            Items = tenantDtos,
            TotalCount = totalCount,
            Page = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };
    }

}
