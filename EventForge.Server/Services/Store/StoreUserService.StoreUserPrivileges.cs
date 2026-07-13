using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{

    public async Task<PagedResult<StoreUserPrivilegeDto>> GetStoreUserPrivilegesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var query = context.StoreUserPrivileges
            .AsNoTracking()
            .Include(sup => sup.ImageDocument)
            .Where(sup => !sup.IsDeleted && sup.TenantId == currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var storeUserPrivileges = await query
            .OrderBy(sup => sup.SortOrder)
            .ThenBy(sup => sup.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var storeUserPrivilegeDtos = new List<StoreUserPrivilegeDto>();
        foreach (var privilege in storeUserPrivileges)
        {
            var groupCount = await context.StoreUserGroups
                .AsNoTracking()
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == privilege.Id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

            storeUserPrivilegeDtos.Add(MapToStoreUserPrivilegeDto(privilege, groupCount));
        }

        return new PagedResult<StoreUserPrivilegeDto>
        {
            Items = storeUserPrivilegeDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StoreUserPrivilegeDto?> GetStoreUserPrivilegeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var storeUserPrivilege = await context.StoreUserPrivileges
            .AsNoTracking()
            .Include(sup => sup.ImageDocument)
            .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserPrivilege is null)
        {
            logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found.", id);
            return null;
        }

        var groupCount = await context.StoreUserGroups
            .AsNoTracking()
            .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

        return MapToStoreUserPrivilegeDto(storeUserPrivilege, groupCount);
    }

    public async Task<IEnumerable<StoreUserPrivilegeDto>> GetStoreUserPrivilegesByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var storeUserPrivileges = await context.StoreUserPrivileges
            .AsNoTracking()
            .Include(sup => sup.ImageDocument)
            .Where(sup => sup.Groups.Any(g => g.Id == groupId) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .OrderBy(sup => sup.SortOrder)
            .ThenBy(sup => sup.Name)
            .ToListAsync(cancellationToken);

        var storeUserPrivilegeDtos = new List<StoreUserPrivilegeDto>();
        foreach (var privilege in storeUserPrivileges)
        {
            var groupCount = await context.StoreUserGroups
                .AsNoTracking()
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == privilege.Id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

            storeUserPrivilegeDtos.Add(MapToStoreUserPrivilegeDto(privilege, groupCount));
        }

        return storeUserPrivilegeDtos;
    }

    public async Task<StoreUserPrivilegeDto> CreateStoreUserPrivilegeAsync(CreateStoreUserPrivilegeDto createStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var storeUserPrivilege = new StoreUserPrivilege
        {
            TenantId = currentTenantId.Value,
            Code = createStoreUserPrivilegeDto.Code,
            Name = createStoreUserPrivilegeDto.Name,
            Category = createStoreUserPrivilegeDto.Category,
            Description = createStoreUserPrivilegeDto.Description,
            Status = (EventForge.Server.Data.Entities.Store.CashierPrivilegeStatus)createStoreUserPrivilegeDto.Status,
            SortOrder = createStoreUserPrivilegeDto.SortOrder,
            // Issue #315: Permission System Fields
            IsSystemPrivilege = createStoreUserPrivilegeDto.IsSystemPrivilege,
            DefaultAssigned = createStoreUserPrivilegeDto.DefaultAssigned,
            Resource = createStoreUserPrivilegeDto.Resource,
            Action = createStoreUserPrivilegeDto.Action,
            PermissionKey = createStoreUserPrivilegeDto.PermissionKey,
            ImageDocumentId = createStoreUserPrivilegeDto.ImageDocumentId,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.StoreUserPrivileges.Add(storeUserPrivilege);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Store user privilege {StoreUserPrivilegeId} created by {User}.",
            storeUserPrivilege.Id, currentUser);

        return MapToStoreUserPrivilegeDto(storeUserPrivilege, 0);
    }

    public async Task<StoreUserPrivilegeDto?> UpdateStoreUserPrivilegeAsync(Guid id, UpdateStoreUserPrivilegeDto updateStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var originalStoreUserPrivilege = await context.StoreUserPrivileges
            .AsNoTracking()
            .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStoreUserPrivilege is null)
        {
            logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        var storeUserPrivilege = await context.StoreUserPrivileges
            .AsNoTracking()
            .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserPrivilege is null)
        {
            logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        storeUserPrivilege.Code = updateStoreUserPrivilegeDto.Code;
        storeUserPrivilege.Name = updateStoreUserPrivilegeDto.Name;
        storeUserPrivilege.Category = updateStoreUserPrivilegeDto.Category;
        storeUserPrivilege.Description = updateStoreUserPrivilegeDto.Description;
        storeUserPrivilege.Status = (EventForge.Server.Data.Entities.Store.CashierPrivilegeStatus)updateStoreUserPrivilegeDto.Status;
        storeUserPrivilege.SortOrder = updateStoreUserPrivilegeDto.SortOrder;
        // Issue #315: Permission System Fields
        storeUserPrivilege.Resource = updateStoreUserPrivilegeDto.Resource;
        storeUserPrivilege.Action = updateStoreUserPrivilegeDto.Action;
        storeUserPrivilege.PermissionKey = updateStoreUserPrivilegeDto.PermissionKey;
        storeUserPrivilege.ImageDocumentId = updateStoreUserPrivilegeDto.ImageDocumentId;
        storeUserPrivilege.ModifiedAt = DateTime.UtcNow;
        storeUserPrivilege.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Update", currentUser, originalStoreUserPrivilege, cancellationToken);

        logger.LogInformation("Store user privilege {StoreUserPrivilegeId} updated by {User}", id, currentUser);

        var groupCount = await context.StoreUserGroups
            .AsNoTracking()
            .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

        return MapToStoreUserPrivilegeDto(storeUserPrivilege, groupCount);
    }

    public async Task<bool> DeleteStoreUserPrivilegeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
        }

        var originalStoreUserPrivilege = await context.StoreUserPrivileges
            .AsNoTracking()
            .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStoreUserPrivilege is null)
        {
            logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        var storeUserPrivilege = await context.StoreUserPrivileges
            .AsNoTracking()
            .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserPrivilege is null)
        {
            logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        storeUserPrivilege.IsDeleted = true;
        storeUserPrivilege.DeletedAt = DateTime.UtcNow;
        storeUserPrivilege.DeletedBy = currentUser;
        storeUserPrivilege.ModifiedAt = DateTime.UtcNow;
        storeUserPrivilege.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Delete", currentUser, originalStoreUserPrivilege, cancellationToken);

        logger.LogInformation("Store user privilege {StoreUserPrivilegeId} deleted by {User}", id, currentUser);

        return true;
    }

}
