using EventForge.Server.DTOs.Store;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service implementation for managing store users, groups, and privileges.
/// </summary>
public class StoreUserService : IStoreUserService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StoreUserService> _logger;

    public StoreUserService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<StoreUserService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region StoreUser Operations

    public async Task<PagedResult<StoreUserDto>> GetStoreUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in store user queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var query = _context.StoreUsers
                .WhereActiveTenant(currentTenantId.Value)
                .Include(su => su.CashierGroup)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store users");
            throw;
        }
    }

    public async Task<StoreUserDto?> GetStoreUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Where(su => su.Id == id && !su.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found.", id);
                return null;
            }

            return MapToStoreUserDto(storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user with ID {StoreUserId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StoreUserDto>> GetStoreUsersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUsers = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Where(su => su.CashierGroupId == groupId && !su.IsDeleted)
                .OrderBy(su => su.Name)
                .ToListAsync(cancellationToken);

            return storeUsers.Select(MapToStoreUserDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store users for group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<StoreUserDto> CreateStoreUserAsync(CreateStoreUserDto createStoreUserDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUser = new StoreUser
            {
                Name = createStoreUserDto.Name,
                Username = createStoreUserDto.Username,
                Email = createStoreUserDto.Email,
                PasswordHash = createStoreUserDto.PasswordHash,
                Role = createStoreUserDto.Role,
                Notes = createStoreUserDto.Notes,
                CashierGroupId = createStoreUserDto.CashierGroupId,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _context.StoreUsers.Add(storeUser);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUser, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Store user {StoreUserName} created with ID {StoreUserId} by {User}",
                storeUser.Name, storeUser.Id, currentUser);

            // Reload with includes
            var createdStoreUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .FirstAsync(su => su.Id == storeUser.Id, cancellationToken);

            return MapToStoreUserDto(createdStoreUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user");
            throw;
        }
    }

    public async Task<StoreUserDto?> UpdateStoreUserAsync(Guid id, UpdateStoreUserDto updateStoreUserDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUser = await _context.StoreUsers
                .AsNoTracking()
                .Where(su => su.Id == id && !su.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var storeUser = await _context.StoreUsers
                .Where(su => su.Id == id && !su.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            storeUser.Name = updateStoreUserDto.Name;
            // Note: Username and PasswordHash are intentionally not updatable via this method
            storeUser.Email = updateStoreUserDto.Email;
            storeUser.Role = updateStoreUserDto.Role;
            storeUser.Status = updateStoreUserDto.Status;
            storeUser.Notes = updateStoreUserDto.Notes;
            storeUser.CashierGroupId = updateStoreUserDto.CashierGroupId;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUser, "Update", currentUser, originalStoreUser, cancellationToken);

            _logger.LogInformation("Store user {StoreUserId} updated by {User}", id, currentUser);

            // Reload with includes
            var updatedStoreUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .FirstAsync(su => su.Id == id, cancellationToken);

            return MapToStoreUserDto(updatedStoreUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user with ID {StoreUserId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteStoreUserAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUser = await _context.StoreUsers
                .AsNoTracking()
                .Where(su => su.Id == id && !su.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var storeUser = await _context.StoreUsers
                .Where(su => su.Id == id && !su.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            storeUser.IsDeleted = true;
            storeUser.DeletedAt = DateTime.UtcNow;
            storeUser.DeletedBy = currentUser;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUser, "Delete", currentUser, originalStoreUser, cancellationToken);

            _logger.LogInformation("Store user {StoreUserId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user with ID {StoreUserId}", id);
            throw;
        }
    }

    #endregion

    #region StoreUserGroup Operations

    public async Task<PagedResult<StoreUserGroupDto>> GetStoreUserGroupsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.StoreUserGroups
                .Where(sug => !sug.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var storeUserGroups = await query
                .OrderBy(sug => sug.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var storeUserGroupDtos = new List<StoreUserGroupDto>();
            foreach (var group in storeUserGroups)
            {
                var cashierCount = await _context.StoreUsers
                    .CountAsync(su => su.CashierGroupId == group.Id && !su.IsDeleted, cancellationToken);
                var privilegeCount = await _context.StoreUserPrivileges
                    .CountAsync(sup => sup.Groups.Any(g => g.Id == group.Id) && !sup.IsDeleted, cancellationToken);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user groups");
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> GetStoreUserGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserGroup = await _context.StoreUserGroups
                .Where(sug => sug.Id == id && !sug.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found.", id);
                return null;
            }

            var cashierCount = await _context.StoreUsers
                .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted, cancellationToken);
            var privilegeCount = await _context.StoreUserPrivileges
                .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted, cancellationToken);

            return MapToStoreUserGroupDto(storeUserGroup, cashierCount, privilegeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user group with ID {StoreUserGroupId}", id);
            throw;
        }
    }

    public async Task<StoreUserGroupDto> CreateStoreUserGroupAsync(CreateStoreUserGroupDto createStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserGroup = new StoreUserGroup
            {
                Code = createStoreUserGroupDto.Code,
                Name = createStoreUserGroupDto.Name,
                Description = createStoreUserGroupDto.Description,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _context.StoreUserGroups.Add(storeUserGroup);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Store user group {StoreUserGroupName} created with ID {StoreUserGroupId} by {User}",
                storeUserGroup.Name, storeUserGroup.Id, currentUser);

            return MapToStoreUserGroupDto(storeUserGroup, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user group");
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> UpdateStoreUserGroupAsync(Guid id, UpdateStoreUserGroupDto updateStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUserGroup = await _context.StoreUserGroups
                .AsNoTracking()
                .Where(sug => sug.Id == id && !sug.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var storeUserGroup = await _context.StoreUserGroups
                .Where(sug => sug.Id == id && !sug.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            storeUserGroup.Code = updateStoreUserGroupDto.Code;
            storeUserGroup.Name = updateStoreUserGroupDto.Name;
            storeUserGroup.Description = updateStoreUserGroupDto.Description;
            storeUserGroup.ModifiedAt = DateTime.UtcNow;
            storeUserGroup.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Update", currentUser, originalStoreUserGroup, cancellationToken);

            _logger.LogInformation("Store user group {StoreUserGroupId} updated by {User}", id, currentUser);

            var cashierCount = await _context.StoreUsers
                .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted, cancellationToken);
            var privilegeCount = await _context.StoreUserPrivileges
                .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted, cancellationToken);

            return MapToStoreUserGroupDto(storeUserGroup, cashierCount, privilegeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user group with ID {StoreUserGroupId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteStoreUserGroupAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUserGroup = await _context.StoreUserGroups
                .AsNoTracking()
                .Where(sug => sug.Id == id && !sug.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var storeUserGroup = await _context.StoreUserGroups
                .Where(sug => sug.Id == id && !sug.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            storeUserGroup.IsDeleted = true;
            storeUserGroup.DeletedAt = DateTime.UtcNow;
            storeUserGroup.DeletedBy = currentUser;
            storeUserGroup.ModifiedAt = DateTime.UtcNow;
            storeUserGroup.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Delete", currentUser, originalStoreUserGroup, cancellationToken);

            _logger.LogInformation("Store user group {StoreUserGroupId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user group with ID {StoreUserGroupId}", id);
            throw;
        }
    }

    #endregion

    #region StoreUserPrivilege Operations

    public async Task<PagedResult<StoreUserPrivilegeDto>> GetStoreUserPrivilegesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.StoreUserPrivileges
                .Where(sup => !sup.IsDeleted);

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
                var groupCount = await _context.StoreUserGroups
                    .CountAsync(sug => sug.Privileges.Any(p => p.Id == privilege.Id) && !sug.IsDeleted, cancellationToken);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user privileges");
            throw;
        }
    }

    public async Task<StoreUserPrivilegeDto?> GetStoreUserPrivilegeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found.", id);
                return null;
            }

            var groupCount = await _context.StoreUserGroups
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted, cancellationToken);

            return MapToStoreUserPrivilegeDto(storeUserPrivilege, groupCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user privilege with ID {StoreUserPrivilegeId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StoreUserPrivilegeDto>> GetStoreUserPrivilegesByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserPrivileges = await _context.StoreUserPrivileges
                .Where(sup => sup.Groups.Any(g => g.Id == groupId) && !sup.IsDeleted)
                .OrderBy(sup => sup.SortOrder)
                .ThenBy(sup => sup.Name)
                .ToListAsync(cancellationToken);

            var storeUserPrivilegeDtos = new List<StoreUserPrivilegeDto>();
            foreach (var privilege in storeUserPrivileges)
            {
                var groupCount = await _context.StoreUserGroups
                    .CountAsync(sug => sug.Privileges.Any(p => p.Id == privilege.Id) && !sug.IsDeleted, cancellationToken);

                storeUserPrivilegeDtos.Add(MapToStoreUserPrivilegeDto(privilege, groupCount));
            }

            return storeUserPrivilegeDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user privileges for group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<StoreUserPrivilegeDto> CreateStoreUserPrivilegeAsync(CreateStoreUserPrivilegeDto createStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserPrivilege = new StoreUserPrivilege
            {
                Code = createStoreUserPrivilegeDto.Code,
                Name = createStoreUserPrivilegeDto.Name,
                Category = createStoreUserPrivilegeDto.Category,
                Description = createStoreUserPrivilegeDto.Description,
                SortOrder = createStoreUserPrivilegeDto.SortOrder,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _context.StoreUserPrivileges.Add(storeUserPrivilege);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Store user privilege {StoreUserPrivilegeName} created with ID {StoreUserPrivilegeId} by {User}",
                storeUserPrivilege.Name, storeUserPrivilege.Id, currentUser);

            return MapToStoreUserPrivilegeDto(storeUserPrivilege, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user privilege");
            throw;
        }
    }

    public async Task<StoreUserPrivilegeDto?> UpdateStoreUserPrivilegeAsync(Guid id, UpdateStoreUserPrivilegeDto updateStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUserPrivilege = await _context.StoreUserPrivileges
                .AsNoTracking()
                .Where(sup => sup.Id == id && !sup.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            storeUserPrivilege.Code = updateStoreUserPrivilegeDto.Code;
            storeUserPrivilege.Name = updateStoreUserPrivilegeDto.Name;
            storeUserPrivilege.Category = updateStoreUserPrivilegeDto.Category;
            storeUserPrivilege.Description = updateStoreUserPrivilegeDto.Description;
            storeUserPrivilege.SortOrder = updateStoreUserPrivilegeDto.SortOrder;
            storeUserPrivilege.ModifiedAt = DateTime.UtcNow;
            storeUserPrivilege.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Update", currentUser, originalStoreUserPrivilege, cancellationToken);

            _logger.LogInformation("Store user privilege {StoreUserPrivilegeId} updated by {User}", id, currentUser);

            var groupCount = await _context.StoreUserGroups
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted, cancellationToken);

            return MapToStoreUserPrivilegeDto(storeUserPrivilege, groupCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user privilege with ID {StoreUserPrivilegeId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteStoreUserPrivilegeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalStoreUserPrivilege = await _context.StoreUserPrivileges
                .AsNoTracking()
                .Where(sup => sup.Id == id && !sup.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            storeUserPrivilege.IsDeleted = true;
            storeUserPrivilege.DeletedAt = DateTime.UtcNow;
            storeUserPrivilege.DeletedBy = currentUser;
            storeUserPrivilege.ModifiedAt = DateTime.UtcNow;
            storeUserPrivilege.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Delete", currentUser, originalStoreUserPrivilege, cancellationToken);

            _logger.LogInformation("Store user privilege {StoreUserPrivilegeId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user privilege with ID {StoreUserPrivilegeId}", id);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    public async Task<bool> StoreUserExistsAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        return await _context.StoreUsers
            .AnyAsync(su => su.Id == storeUserId && !su.IsDeleted, cancellationToken);
    }

    public async Task<bool> StoreUserGroupExistsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.StoreUserGroups
            .AnyAsync(sug => sug.Id == groupId && !sug.IsDeleted, cancellationToken);
    }

    private static StoreUserDto MapToStoreUserDto(StoreUser storeUser)
    {
        return new StoreUserDto
        {
            Id = storeUser.Id,
            Name = storeUser.Name,
            Username = storeUser.Username,
            Email = storeUser.Email,
            Role = storeUser.Role,
            LastLoginAt = storeUser.LastLoginAt,
            Notes = storeUser.Notes,
            CashierGroupId = storeUser.CashierGroupId,
            CashierGroupName = storeUser.CashierGroup?.Name,
            CreatedAt = storeUser.CreatedAt,
            CreatedBy = storeUser.CreatedBy,
            ModifiedAt = storeUser.ModifiedAt,
            ModifiedBy = storeUser.ModifiedBy
        };
    }

    private static StoreUserGroupDto MapToStoreUserGroupDto(StoreUserGroup storeUserGroup, int cashierCount, int privilegeCount)
    {
        return new StoreUserGroupDto
        {
            Id = storeUserGroup.Id,
            Code = storeUserGroup.Code,
            Name = storeUserGroup.Name,
            Description = storeUserGroup.Description,
            CashierCount = cashierCount,
            PrivilegeCount = privilegeCount,
            CreatedAt = storeUserGroup.CreatedAt,
            CreatedBy = storeUserGroup.CreatedBy,
            ModifiedAt = storeUserGroup.ModifiedAt,
            ModifiedBy = storeUserGroup.ModifiedBy
        };
    }

    private static StoreUserPrivilegeDto MapToStoreUserPrivilegeDto(StoreUserPrivilege storeUserPrivilege, int groupCount)
    {
        return new StoreUserPrivilegeDto
        {
            Id = storeUserPrivilege.Id,
            Code = storeUserPrivilege.Code,
            Name = storeUserPrivilege.Name,
            Category = storeUserPrivilege.Category,
            Description = storeUserPrivilege.Description,
            SortOrder = storeUserPrivilege.SortOrder,
            GroupCount = groupCount,
            CreatedAt = storeUserPrivilege.CreatedAt,
            CreatedBy = storeUserPrivilege.CreatedBy,
            ModifiedAt = storeUserPrivilege.ModifiedAt,
            ModifiedBy = storeUserPrivilege.ModifiedBy
        };
    }

    #endregion
}