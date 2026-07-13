using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{

    public async Task<PagedResult<StoreUserDto>> GetStoreUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // NOTE: Tenant isolation test coverage should be expanded in future test iterations
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var query = context.StoreUsers
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value)
            .Include(su => su.CashierGroup)
            .Include(su => su.PhotoDocument)
            .Where(su => !su.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);
        var storeUsers = await query
            .OrderBy(su => su.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var storeUserDtos = storeUsers.Select(MapToStoreUserDto);

        return new PagedResult<StoreUserDto>
        {
            Items = storeUserDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StoreUserDto?> GetStoreUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .AsNoTracking()
            .Include(su => su.CashierGroup)
            .Include(su => su.PhotoDocument)
            .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUser is null)
        {
            logger.LogWarning("Store user with ID {StoreUserId} not found.", id);
            return null;
        }

        return MapToStoreUserDto(storeUser);
    }

    public async Task<StoreUserDto?> GetStoreUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .AsNoTracking()
            .Include(su => su.CashierGroup)
            .Include(su => su.PhotoDocument)
            .Where(su => su.Username == username && !su.IsDeleted && su.TenantId == currentTenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeUser is null)
        {
            logger.LogWarning("Store user with username {Username} not found.", username);
            return null;
        }

        return MapToStoreUserDto(storeUser);
    }

    public async Task<IEnumerable<StoreUserDto>> GetStoreUsersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUsers = await context.StoreUsers
            .AsNoTracking()
            .Include(su => su.CashierGroup)
            .Where(su => su.CashierGroupId == groupId && !su.IsDeleted && su.TenantId == currentTenantId.Value)
            .OrderBy(su => su.Name)
            .ToListAsync(cancellationToken);

        return storeUsers.Select(MapToStoreUserDto);
    }

    public async Task<StoreUserDto> CreateStoreUserAsync(CreateStoreUserDto createStoreUserDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        // Validate CashierGroupId
        if (createStoreUserDto.CashierGroupId == Guid.Empty)
        {
            throw new InvalidOperationException("Cashier group ID cannot be an empty GUID. Use null to indicate no group.");
        }

        if (createStoreUserDto.CashierGroupId.HasValue)
        {
            var groupExists = await context.StoreUserGroups
                .AsNoTracking()
                .AnyAsync(g => g.Id == createStoreUserDto.CashierGroupId.Value
                            && g.TenantId == currentTenantId.Value
                            && !g.IsDeleted,
                        cancellationToken);

            if (!groupExists)
            {
                throw new InvalidOperationException($"Cashier group with ID {createStoreUserDto.CashierGroupId.Value} does not exist.");
            }
        }

        var storeUser = new StoreUser
        {
            TenantId = currentTenantId.Value,
            Name = createStoreUserDto.Name,
            Username = createStoreUserDto.Username,
            Email = createStoreUserDto.Email,
            PasswordHash = createStoreUserDto.PasswordHash,
            Role = createStoreUserDto.Role,
            Status = (EventForge.Server.Data.Entities.Store.CashierStatus)createStoreUserDto.Status,
            Notes = createStoreUserDto.Notes,
            CashierGroupId = createStoreUserDto.CashierGroupId,
            PhotoDocumentId = createStoreUserDto.ImageDocumentId,
            // Issue #315: Extended Fields
            PhotoConsent = createStoreUserDto.PhotoConsent,
            PhoneNumber = createStoreUserDto.PhoneNumber,
            DateOfBirth = createStoreUserDto.DateOfBirth,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.StoreUsers.Add(storeUser);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(storeUser, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Store user {StoreUserId} created by {User}.",
            storeUser.Id, currentUser);

        // Reload with includes
        var createdStoreUser = await context.StoreUsers
            .AsNoTracking()
            .Include(su => su.CashierGroup)
            .Include(su => su.PhotoDocument)
            .FirstAsync(su => su.Id == storeUser.Id, cancellationToken);

        return MapToStoreUserDto(createdStoreUser);
    }

    public async Task<StoreUserDto?> UpdateStoreUserAsync(Guid id, UpdateStoreUserDto updateStoreUserDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await context.StoreUsers
                .AsNoTracking()
                .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser is null)
            {
                logger.LogWarning("Store user with ID {StoreUserId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(storeUser).CurrentValues.Clone();
            var originalStoreUser = (StoreUser)originalValues.ToObject();

            storeUser.Name = updateStoreUserDto.Name;
            // Note: Username and PasswordHash are intentionally not updatable via this method
            storeUser.Email = updateStoreUserDto.Email;
            storeUser.Role = updateStoreUserDto.Role;
            storeUser.Status = (EventForge.Server.Data.Entities.Store.CashierStatus)updateStoreUserDto.Status;
            storeUser.Notes = updateStoreUserDto.Notes;
            storeUser.CashierGroupId = updateStoreUserDto.CashierGroupId;
            storeUser.PhotoDocumentId = updateStoreUserDto.ImageDocumentId;
            // Issue #315: Extended Fields
            storeUser.PhoneNumber = updateStoreUserDto.PhoneNumber;
            storeUser.DateOfBirth = updateStoreUserDto.DateOfBirth;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating StoreUser {StoreUserId}.", id);
                throw new InvalidOperationException("Lo store user è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(storeUser, "Update", currentUser, originalStoreUser, cancellationToken);

            logger.LogInformation("Store user {StoreUserId} updated by {User}", id, currentUser);

            // Reload with includes
            var updatedStoreUser = await context.StoreUsers
                .AsNoTracking()
                .Include(su => su.CashierGroup)
                .Include(su => su.PhotoDocument)
                .FirstAsync(su => su.Id == id, cancellationToken);

            return MapToStoreUserDto(updatedStoreUser);
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

    public async Task<bool> DeleteStoreUserAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await context.StoreUsers
                .AsNoTracking()
                .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser is null)
            {
                logger.LogWarning("Store user with ID {StoreUserId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(storeUser).CurrentValues.Clone();
            var originalStoreUser = (StoreUser)originalValues.ToObject();

            storeUser.IsDeleted = true;
            storeUser.DeletedAt = DateTime.UtcNow;
            storeUser.DeletedBy = currentUser;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting StoreUser {StoreUserId}.", id);
                throw new InvalidOperationException("Lo store user è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(storeUser, "Delete", currentUser, originalStoreUser, cancellationToken);

            logger.LogInformation("Store user {StoreUserId} deleted by {User}", id, currentUser);

            return true;
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

}
