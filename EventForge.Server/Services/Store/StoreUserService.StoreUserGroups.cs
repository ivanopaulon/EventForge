using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{

    public async Task<PagedResult<StoreUserGroupDto>> GetStoreUserGroupsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var query = context.StoreUserGroups
            .AsNoTracking()
            .Include(sug => sug.LogoDocument)
            .Where(sug => !sug.IsDeleted && sug.TenantId == currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var storeUserGroups = await query
            .OrderBy(sug => sug.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var storeUserGroupDtos = new List<StoreUserGroupDto>();
        foreach (var group in storeUserGroups)
        {
            var cashierCount = await context.StoreUsers
                .AsNoTracking()
                .CountAsync(su => su.CashierGroupId == group.Id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
            var privilegeCount = await context.StoreUserPrivileges
                .AsNoTracking()
                .CountAsync(sup => sup.Groups.Any(g => g.Id == group.Id) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

            storeUserGroupDtos.Add(MapToStoreUserGroupDto(group, cashierCount, privilegeCount));
        }

        return new PagedResult<StoreUserGroupDto>
        {
            Items = storeUserGroupDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StoreUserGroupDto?> GetStoreUserGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var storeUserGroup = await context.StoreUserGroups
            .AsNoTracking()
            .Include(sug => sug.LogoDocument)
            .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserGroup is null)
        {
            logger.LogWarning("Store user group with ID {StoreUserGroupId} not found.", id);
            return null;
        }

        var cashierCount = await context.StoreUsers
            .AsNoTracking()
            .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
        var privilegeCount = await context.StoreUserPrivileges
            .AsNoTracking()
            .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

        return MapToStoreUserGroupDto(storeUserGroup, cashierCount, privilegeCount);
    }

    public async Task<StoreUserGroupDto> CreateStoreUserGroupAsync(CreateStoreUserGroupDto createStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var storeUserGroup = new StoreUserGroup
        {
            TenantId = currentTenantId.Value,
            Code = createStoreUserGroupDto.Code,
            Name = createStoreUserGroupDto.Name,
            Description = createStoreUserGroupDto.Description,
            Status = (EventForge.Server.Data.Entities.Store.CashierGroupStatus)createStoreUserGroupDto.Status,
            LogoDocumentId = createStoreUserGroupDto.ImageDocumentId,
            // Issue #315: Branding Fields
            ColorHex = createStoreUserGroupDto.ColorHex,
            IsSystemGroup = createStoreUserGroupDto.IsSystemGroup,
            IsDefault = createStoreUserGroupDto.IsDefault,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.StoreUserGroups.Add(storeUserGroup);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(storeUserGroup, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Store user group {StoreUserGroupId} created by {User}.",
            storeUserGroup.Id, currentUser);

        return MapToStoreUserGroupDto(storeUserGroup, 0, 0);
    }

    public async Task<StoreUserGroupDto?> UpdateStoreUserGroupAsync(Guid id, UpdateStoreUserGroupDto updateStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var originalStoreUserGroup = await context.StoreUserGroups
            .AsNoTracking()
            .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStoreUserGroup is null)
        {
            logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        var storeUserGroup = await context.StoreUserGroups
            .AsNoTracking()
            .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserGroup is null)
        {
            logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        storeUserGroup.Code = updateStoreUserGroupDto.Code;
        storeUserGroup.Name = updateStoreUserGroupDto.Name;
        storeUserGroup.Description = updateStoreUserGroupDto.Description;
        storeUserGroup.Status = (EventForge.Server.Data.Entities.Store.CashierGroupStatus)updateStoreUserGroupDto.Status;
        storeUserGroup.LogoDocumentId = updateStoreUserGroupDto.ImageDocumentId;
        // Issue #315: Branding Fields
        storeUserGroup.ColorHex = updateStoreUserGroupDto.ColorHex;
        storeUserGroup.ModifiedAt = DateTime.UtcNow;
        storeUserGroup.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating StoreUserGroup {StoreUserGroupId}.", id);
            throw new InvalidOperationException("Il gruppo store user è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(storeUserGroup, "Update", currentUser, originalStoreUserGroup, cancellationToken);

        logger.LogInformation("Store user group {StoreUserGroupId} updated by {User}", id, currentUser);

        var cashierCount = await context.StoreUsers
            .AsNoTracking()
            .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
        var privilegeCount = await context.StoreUserPrivileges
            .AsNoTracking()
            .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

        return MapToStoreUserGroupDto(storeUserGroup, cashierCount, privilegeCount);
    }

    public async Task<bool> DeleteStoreUserGroupAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var originalStoreUserGroup = await context.StoreUserGroups
            .AsNoTracking()
            .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStoreUserGroup is null)
        {
            logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        var storeUserGroup = await context.StoreUserGroups
            .AsNoTracking()
            .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUserGroup is null)
        {
            logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        storeUserGroup.IsDeleted = true;
        storeUserGroup.DeletedAt = DateTime.UtcNow;
        storeUserGroup.DeletedBy = currentUser;
        storeUserGroup.ModifiedAt = DateTime.UtcNow;
        storeUserGroup.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting StoreUserGroup {StoreUserGroupId}.", id);
            throw new InvalidOperationException("Il gruppo store user è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(storeUserGroup, "Delete", currentUser, originalStoreUserGroup, cancellationToken);

        logger.LogInformation("Store user group {StoreUserGroupId} deleted by {User}", id, currentUser);

        return true;
    }

}
