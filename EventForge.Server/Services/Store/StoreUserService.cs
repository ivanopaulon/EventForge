using EventForge.DTOs.Store;
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

    public StoreUserService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<StoreUserService> logger)
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
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var query = _context.StoreUsers
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Include(su => su.PhotoDocument)
                .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
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

    public async Task<StoreUserDto?> GetStoreUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Include(su => su.PhotoDocument)
                .Where(su => su.Username == username && !su.IsDeleted && su.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with username {Username} not found.", username);
                return null;
            }

            return MapToStoreUserDto(storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user with username {Username}", username);
            throw;
        }
    }

    public async Task<IEnumerable<StoreUserDto>> GetStoreUsersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUsers = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Where(su => su.CashierGroupId == groupId && !su.IsDeleted && su.TenantId == currentTenantId.Value)
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
            var currentTenantId = _tenantContext.CurrentTenantId;
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
                var groupExists = await _context.StoreUserGroups
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
                // Issue #315: Extended Fields
                PhotoConsent = createStoreUserDto.PhotoConsent,
                PhoneNumber = createStoreUserDto.PhoneNumber,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _ = _context.StoreUsers.Add(storeUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUser, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Store user {StoreUserName} created with ID {StoreUserId} by {User}",
                storeUser.Name, storeUser.Id, currentUser);

            // Reload with includes
            var createdStoreUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Include(su => su.PhotoDocument)
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = _context.Entry(storeUser).CurrentValues.Clone();
            var originalStoreUser = (StoreUser)originalValues.ToObject();

            storeUser.Name = updateStoreUserDto.Name;
            // Note: Username and PasswordHash are intentionally not updatable via this method
            storeUser.Email = updateStoreUserDto.Email;
            storeUser.Role = updateStoreUserDto.Role;
            storeUser.Status = (EventForge.Server.Data.Entities.Store.CashierStatus)updateStoreUserDto.Status;
            storeUser.Notes = updateStoreUserDto.Notes;
            storeUser.CashierGroupId = updateStoreUserDto.CashierGroupId;
            // Issue #315: Extended Fields
            storeUser.PhoneNumber = updateStoreUserDto.PhoneNumber;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUser, "Update", currentUser, originalStoreUser, cancellationToken);

            _logger.LogInformation("Store user {StoreUserId} updated by {User}", id, currentUser);

            // Reload with includes
            var updatedStoreUser = await _context.StoreUsers
                .Include(su => su.CashierGroup)
                .Include(su => su.PhotoDocument)
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Where(su => su.Id == id && !su.IsDeleted && su.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user with ID {StoreUserId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Create snapshot of original state before modifications
            var originalValues = _context.Entry(storeUser).CurrentValues.Clone();
            var originalStoreUser = (StoreUser)originalValues.ToObject();

            storeUser.IsDeleted = true;
            storeUser.DeletedAt = DateTime.UtcNow;
            storeUser.DeletedBy = currentUser;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUser, "Delete", currentUser, originalStoreUser, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var query = _context.StoreUserGroups
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
                var cashierCount = await _context.StoreUsers
                    .CountAsync(su => su.CashierGroupId == group.Id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
                var privilegeCount = await _context.StoreUserPrivileges
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var storeUserGroup = await _context.StoreUserGroups
                .Include(sug => sug.LogoDocument)
                .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found.", id);
                return null;
            }

            var cashierCount = await _context.StoreUsers
                .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
            var privilegeCount = await _context.StoreUserPrivileges
                .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
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
                // Issue #315: Branding Fields
                ColorHex = createStoreUserGroupDto.ColorHex,
                IsSystemGroup = createStoreUserGroupDto.IsSystemGroup,
                IsDefault = createStoreUserGroupDto.IsDefault,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _ = _context.StoreUserGroups.Add(storeUserGroup);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Insert", currentUser, null, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var originalStoreUserGroup = await _context.StoreUserGroups
                .AsNoTracking()
                .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var storeUserGroup = await _context.StoreUserGroups
                .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            storeUserGroup.Code = updateStoreUserGroupDto.Code;
            storeUserGroup.Name = updateStoreUserGroupDto.Name;
            storeUserGroup.Description = updateStoreUserGroupDto.Description;
            storeUserGroup.Status = (EventForge.Server.Data.Entities.Store.CashierGroupStatus)updateStoreUserGroupDto.Status;
            // Issue #315: Branding Fields
            storeUserGroup.ColorHex = updateStoreUserGroupDto.ColorHex;
            storeUserGroup.ModifiedAt = DateTime.UtcNow;
            storeUserGroup.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Update", currentUser, originalStoreUserGroup, cancellationToken);

            _logger.LogInformation("Store user group {StoreUserGroupId} updated by {User}", id, currentUser);

            var cashierCount = await _context.StoreUsers
                .CountAsync(su => su.CashierGroupId == id && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
            var privilegeCount = await _context.StoreUserPrivileges
                .CountAsync(sup => sup.Groups.Any(g => g.Id == id) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var originalStoreUserGroup = await _context.StoreUserGroups
                .AsNoTracking()
                .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserGroup == null)
            {
                _logger.LogWarning("Store user group with ID {StoreUserGroupId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var storeUserGroup = await _context.StoreUserGroups
                .Where(sug => sug.Id == id && !sug.IsDeleted && sug.TenantId == currentTenantId.Value)
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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserGroup, "Delete", currentUser, originalStoreUserGroup, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
            }

            var query = _context.StoreUserPrivileges
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
                var groupCount = await _context.StoreUserGroups
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
            }

            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (storeUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found.", id);
                return null;
            }

            var groupCount = await _context.StoreUserGroups
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
            }

            var storeUserPrivileges = await _context.StoreUserPrivileges
                .Where(sup => sup.Groups.Any(g => g.Id == groupId) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
                .OrderBy(sup => sup.SortOrder)
                .ThenBy(sup => sup.Name)
                .ToListAsync(cancellationToken);

            var storeUserPrivilegeDtos = new List<StoreUserPrivilegeDto>();
            foreach (var privilege in storeUserPrivileges)
            {
                var groupCount = await _context.StoreUserGroups
                    .CountAsync(sug => sug.Privileges.Any(p => p.Id == privilege.Id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
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
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _ = _context.StoreUserPrivileges.Add(storeUserPrivilege);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Insert", currentUser, null, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
            }

            var originalStoreUserPrivilege = await _context.StoreUserPrivileges
                .AsNoTracking()
                .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
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
            storeUserPrivilege.Status = (EventForge.Server.Data.Entities.Store.CashierPrivilegeStatus)updateStoreUserPrivilegeDto.Status;
            storeUserPrivilege.SortOrder = updateStoreUserPrivilegeDto.SortOrder;
            // Issue #315: Permission System Fields
            storeUserPrivilege.Resource = updateStoreUserPrivilegeDto.Resource;
            storeUserPrivilege.Action = updateStoreUserPrivilegeDto.Action;
            storeUserPrivilege.PermissionKey = updateStoreUserPrivilegeDto.PermissionKey;
            storeUserPrivilege.ModifiedAt = DateTime.UtcNow;
            storeUserPrivilege.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Update", currentUser, originalStoreUserPrivilege, cancellationToken);

            _logger.LogInformation("Store user privilege {StoreUserPrivilegeId} updated by {User}", id, currentUser);

            var groupCount = await _context.StoreUserGroups
                .CountAsync(sug => sug.Privileges.Any(p => p.Id == id) && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user privilege operations.");
            }

            var originalStoreUserPrivilege = await _context.StoreUserPrivileges
                .AsNoTracking()
                .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStoreUserPrivilege == null)
            {
                _logger.LogWarning("Store user privilege with ID {StoreUserPrivilegeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var storeUserPrivilege = await _context.StoreUserPrivileges
                .Where(sup => sup.Id == id && !sup.IsDeleted && sup.TenantId == currentTenantId.Value)
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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(storeUserPrivilege, "Delete", currentUser, originalStoreUserPrivilege, cancellationToken);

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
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        return await _context.StoreUsers
            .AnyAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
    }

    public async Task<bool> StoreUserGroupExistsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        return await _context.StoreUserGroups
            .AnyAsync(sug => sug.Id == groupId && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);
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
            Status = (EventForge.DTOs.Common.CashierStatus)storeUser.Status,
            LastLoginAt = storeUser.LastLoginAt,
            Notes = storeUser.Notes,
            CashierGroupId = storeUser.CashierGroupId,
            CashierGroupName = storeUser.CashierGroup?.Name,
            // Issue #315: Image Management & Extended Fields
            PhotoDocumentId = storeUser.PhotoDocumentId,
            PhotoUrl = storeUser.PhotoDocument?.StorageKey,
            PhotoThumbnailUrl = storeUser.PhotoDocument?.ThumbnailStorageKey,
            PhotoConsent = storeUser.PhotoConsent,
            PhotoConsentAt = storeUser.PhotoConsentAt,
            PhoneNumber = storeUser.PhoneNumber,
            LastPasswordChangedAt = storeUser.LastPasswordChangedAt,
            TwoFactorEnabled = storeUser.TwoFactorEnabled,
            ExternalId = storeUser.ExternalId,
            IsOnShift = storeUser.IsOnShift,
            ShiftId = storeUser.ShiftId,
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
            Status = (EventForge.DTOs.Common.CashierGroupStatus)storeUserGroup.Status,
            CashierCount = cashierCount,
            PrivilegeCount = privilegeCount,
            // Issue #315: Image Management & Branding Fields
            LogoDocumentId = storeUserGroup.LogoDocumentId,
            LogoUrl = storeUserGroup.LogoDocument?.StorageKey,
            LogoThumbnailUrl = storeUserGroup.LogoDocument?.ThumbnailStorageKey,
            ColorHex = storeUserGroup.ColorHex,
            IsSystemGroup = storeUserGroup.IsSystemGroup,
            IsDefault = storeUserGroup.IsDefault,
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
            Status = (EventForge.DTOs.Common.CashierPrivilegeStatus)storeUserPrivilege.Status,
            SortOrder = storeUserPrivilege.SortOrder,
            GroupCount = groupCount,
            // Issue #315: Permission System Fields
            IsSystemPrivilege = storeUserPrivilege.IsSystemPrivilege,
            DefaultAssigned = storeUserPrivilege.DefaultAssigned,
            Resource = storeUserPrivilege.Resource,
            Action = storeUserPrivilege.Action,
            PermissionKey = storeUserPrivilege.PermissionKey,
            CreatedAt = storeUserPrivilege.CreatedAt,
            CreatedBy = storeUserPrivilege.CreatedBy,
            ModifiedAt = storeUserPrivilege.ModifiedAt,
            ModifiedBy = storeUserPrivilege.ModifiedBy
        };
    }

    #endregion

    #region StorePos CRUD Operations

    public async Task<PagedResult<StorePosDto>> GetStorePosesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            _logger.LogDebug("Querying store POS terminals for tenant {TenantId}", currentTenantId.Value);

            var query = _context.StorePoses
                .Where(sp => !sp.IsDeleted && sp.TenantId == currentTenantId.Value)
                .OrderBy(sp => sp.Name);

            var totalCount = await query.CountAsync(cancellationToken);

            _logger.LogDebug("Found {Count} store POS terminals for tenant {TenantId}", totalCount, currentTenantId.Value);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToStorePosDto).ToList();

            return new PagedResult<StorePosDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store POS terminals for tenant {TenantId} (page: {Page}, pageSize: {PageSize})", _tenantContext.CurrentTenantId, page, pageSize);
            throw;
        }
    }

    public async Task<StorePosDto?> GetStorePosByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .Include(sp => sp.ImageDocument)
                .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            return storePos != null ? MapToStorePosDto(storePos) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store POS {Id}.", id);
            throw;
        }
    }

    public async Task<StorePosDto> CreateStorePosAsync(CreateStorePosDto createStorePosDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = new StorePos
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createStorePosDto.Name,
                Description = createStorePosDto.Description,
                Status = (EventForge.Server.Data.Entities.Store.CashRegisterStatus)createStorePosDto.Status,
                Location = createStorePosDto.Location,
                Notes = createStorePosDto.Notes,
                TerminalIdentifier = createStorePosDto.TerminalIdentifier,
                IPAddress = createStorePosDto.IPAddress,
                LocationLatitude = createStorePosDto.LocationLatitude,
                LocationLongitude = createStorePosDto.LocationLongitude,
                CurrencyCode = createStorePosDto.CurrencyCode,
                TimeZone = createStorePosDto.TimeZone,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsDeleted = false
            };

            _context.StorePoses.Add(storePos);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store POS {Name} created successfully by {User}.", storePos.Name, currentUser);
            return MapToStorePosDto(storePos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store POS.");
            throw;
        }
    }

    public async Task<StorePosDto?> UpdateStorePosAsync(Guid id, UpdateStorePosDto updateStorePosDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            if (storePos == null)
            {
                _logger.LogWarning("Store POS {Id} not found for update in tenant {TenantId}.", id, currentTenantId.Value);
                return null;
            }

            storePos.Name = updateStorePosDto.Name;
            storePos.Description = updateStorePosDto.Description;
            storePos.Status = (EventForge.Server.Data.Entities.Store.CashRegisterStatus)updateStorePosDto.Status;
            storePos.Location = updateStorePosDto.Location;
            storePos.Notes = updateStorePosDto.Notes;
            storePos.TerminalIdentifier = updateStorePosDto.TerminalIdentifier;
            storePos.IPAddress = updateStorePosDto.IPAddress;
            storePos.IsOnline = updateStorePosDto.IsOnline;
            storePos.ModifiedAt = DateTime.UtcNow;
            storePos.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store POS {Id} updated successfully by {User}.", id, currentUser);
            return MapToStorePosDto(storePos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store POS {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteStorePosAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            if (storePos == null)
            {
                _logger.LogWarning("Store POS {Id} not found for deletion in tenant {TenantId}.", id, currentTenantId.Value);
                return false;
            }

            storePos.IsDeleted = true;
            storePos.ModifiedAt = DateTime.UtcNow;
            storePos.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store POS {Id} deleted successfully by {User}.", id, currentUser);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store POS {Id}.", id);
            throw;
        }
    }

    #endregion

    #region Image Management Methods - Issue #315

    public async Task<StoreUserDto?> UploadStoreUserPhotoAsync(Guid storeUserId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Include(su => su.PhotoDocument)
                .Include(su => su.CashierGroup)
                .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

            if (storeUser == null)
            {
                _logger.LogWarning("Store user {StoreUserId} not found for photo upload in tenant {TenantId}.", storeUserId, currentTenantId.Value);
                return null;
            }

            // GDPR Compliance: Check photo consent
            if (!storeUser.PhotoConsent)
            {
                throw new InvalidOperationException("Photo upload requires explicit user consent (GDPR). PhotoConsent must be true.");
            }

            // Generate a unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"storeuser_{storeUserId}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/storeusers
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storeusers");
            _ = Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            var storageKey = $"/images/storeusers/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = currentTenantId.Value,
                OwnerId = storeUserId,
                OwnerType = "StoreUser",
                FileName = file.FileName,
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                FileSizeBytes = file.Length,
                Title = $"Store User {storeUser.Name} Photo",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            // If store user already has a photo, delete the old one first
            if (storeUser.PhotoDocumentId.HasValue)
            {
                var oldDocument = await _context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == storeUser.PhotoDocumentId.Value, cancellationToken);

                if (oldDocument != null)
                {
                    // Delete old physical file
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }

                    _ = _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _ = _context.DocumentReferences.Add(documentReference);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Update store user with new DocumentReference ID
            storeUser.PhotoDocumentId = documentReference.Id;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store user {StoreUserId} photo uploaded successfully as DocumentReference {DocumentId}.", storeUserId, documentReference.Id);

            // Reload to get the document reference
            storeUser.PhotoDocument = documentReference;
            return MapToStoreUserDto(storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for store user {StoreUserId}.", storeUserId);
            throw;
        }
    }

    public async Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStoreUserPhotoDocumentAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Include(su => su.PhotoDocument)
                .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

            if (storeUser?.PhotoDocument == null)
            {
                _logger.LogWarning("Store user {StoreUserId} not found or has no photo in tenant {TenantId}.", storeUserId, currentTenantId.Value);
                return null;
            }

            return MapToDocumentReferenceDto(storeUser.PhotoDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo document for store user {StoreUserId}.", storeUserId);
            throw;
        }
    }

    public async Task<bool> DeleteStoreUserPhotoAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user operations.");
            }

            var storeUser = await _context.StoreUsers
                .Include(su => su.PhotoDocument)
                .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

            if (storeUser?.PhotoDocument == null)
            {
                _logger.LogWarning("Store user {StoreUserId} not found or has no photo to delete in tenant {TenantId}.", storeUserId, currentTenantId.Value);
                return false;
            }

            // Delete physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storeUser.PhotoDocument.StorageKey.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Remove DocumentReference
            _ = _context.DocumentReferences.Remove(storeUser.PhotoDocument);

            // Update store user
            storeUser.PhotoDocumentId = null;
            storeUser.ModifiedAt = DateTime.UtcNow;
            storeUser.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store user {StoreUserId} photo deleted successfully.", storeUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo for store user {StoreUserId}.", storeUserId);
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> UploadStoreUserGroupLogoAsync(Guid groupId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var group = await _context.StoreUserGroups
                .Include(g => g.LogoDocument)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

            if (group == null)
            {
                _logger.LogWarning("Store user group {GroupId} not found for logo upload in tenant {TenantId}.", groupId, currentTenantId.Value);
                return null;
            }

            // Generate a unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"storegroup_{groupId}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/storegroups
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storegroups");
            _ = Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            var storageKey = $"/images/storegroups/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = currentTenantId.Value,
                OwnerId = groupId,
                OwnerType = "StoreUserGroup",
                FileName = file.FileName,
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                FileSizeBytes = file.Length,
                Title = $"Store User Group {group.Name} Logo",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            // If group already has a logo, delete the old one first
            if (group.LogoDocumentId.HasValue)
            {
                var oldDocument = await _context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == group.LogoDocumentId.Value, cancellationToken);

                if (oldDocument != null)
                {
                    // Delete old physical file
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }

                    _ = _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _ = _context.DocumentReferences.Add(documentReference);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Update group with new DocumentReference ID
            group.LogoDocumentId = documentReference.Id;
            group.ModifiedAt = DateTime.UtcNow;
            group.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store user group {GroupId} logo uploaded successfully as DocumentReference {DocumentId}.", groupId, documentReference.Id);

            // Reload to get the document reference
            group.LogoDocument = documentReference;

            // Get counts for DTO
            var cashierCount = await _context.StoreUsers.CountAsync(su => su.CashierGroupId == groupId && !su.IsDeleted, cancellationToken);
            var privilegeCount = 0; // Placeholder - would need StoreUserGroupPrivilege relationship

            return MapToStoreUserGroupDto(group, cashierCount, privilegeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for store user group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStoreUserGroupLogoDocumentAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var group = await _context.StoreUserGroups
                .Include(g => g.LogoDocument)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

            if (group?.LogoDocument == null)
            {
                _logger.LogWarning("Store user group {GroupId} not found or has no logo in tenant {TenantId}.", groupId, currentTenantId.Value);
                return null;
            }

            return MapToDocumentReferenceDto(group.LogoDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logo document for store user group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<bool> DeleteStoreUserGroupLogoAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store user group operations.");
            }

            var group = await _context.StoreUserGroups
                .Include(g => g.LogoDocument)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

            if (group?.LogoDocument == null)
            {
                _logger.LogWarning("Store user group {GroupId} not found or has no logo to delete in tenant {TenantId}.", groupId, currentTenantId.Value);
                return false;
            }

            // Delete physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", group.LogoDocument.StorageKey.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Remove DocumentReference
            _ = _context.DocumentReferences.Remove(group.LogoDocument);

            // Update group
            group.LogoDocumentId = null;
            group.ModifiedAt = DateTime.UtcNow;
            group.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store user group {GroupId} logo deleted successfully.", groupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting logo for store user group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<StorePosDto?> UploadStorePosImageAsync(Guid storePosId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .Include(sp => sp.ImageDocument)
                .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            if (storePos == null)
            {
                _logger.LogWarning("Store POS {StorePosId} not found for image upload in tenant {TenantId}.", storePosId, currentTenantId.Value);
                return null;
            }

            // Generate a unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"storepos_{storePosId}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/storepos
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storepos");
            _ = Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            var storageKey = $"/images/storepos/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = currentTenantId.Value,
                OwnerId = storePosId,
                OwnerType = "StorePos",
                FileName = file.FileName,
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                FileSizeBytes = file.Length,
                Title = $"Store POS {storePos.Name} Image",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            // If store POS already has an image, delete the old one first
            if (storePos.ImageDocumentId.HasValue)
            {
                var oldDocument = await _context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == storePos.ImageDocumentId.Value, cancellationToken);

                if (oldDocument != null)
                {
                    // Delete old physical file
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }

                    _ = _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _ = _context.DocumentReferences.Add(documentReference);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Update store POS with new DocumentReference ID
            storePos.ImageDocumentId = documentReference.Id;
            storePos.ModifiedAt = DateTime.UtcNow;
            storePos.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store POS {StorePosId} image uploaded successfully as DocumentReference {DocumentId}.", storePosId, documentReference.Id);

            // Reload to get the document reference
            storePos.ImageDocument = documentReference;
            return MapToStorePosDto(storePos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for store POS {StorePosId}.", storePosId);
            throw;
        }
    }

    public async Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStorePosImageDocumentAsync(Guid storePosId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .Include(sp => sp.ImageDocument)
                .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            if (storePos?.ImageDocument == null)
            {
                _logger.LogWarning("Store POS {StorePosId} not found or has no image in tenant {TenantId}.", storePosId, currentTenantId.Value);
                return null;
            }

            return MapToDocumentReferenceDto(storePos.ImageDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image document for store POS {StorePosId}.", storePosId);
            throw;
        }
    }

    public async Task<bool> DeleteStorePosImageAsync(Guid storePosId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for store POS operations.");
            }

            var storePos = await _context.StorePoses
                .Include(sp => sp.ImageDocument)
                .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

            if (storePos?.ImageDocument == null)
            {
                _logger.LogWarning("Store POS {StorePosId} not found or has no image to delete in tenant {TenantId}.", storePosId, currentTenantId.Value);
                return false;
            }

            // Delete physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storePos.ImageDocument.StorageKey.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Remove DocumentReference
            _ = _context.DocumentReferences.Remove(storePos.ImageDocument);

            // Update store POS
            storePos.ImageDocumentId = null;
            storePos.ModifiedAt = DateTime.UtcNow;
            storePos.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Store POS {StorePosId} image deleted successfully.", storePosId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image for store POS {StorePosId}.", storePosId);
            throw;
        }
    }

    private static EventForge.DTOs.Teams.DocumentReferenceDto MapToDocumentReferenceDto(EventForge.Server.Data.Entities.Teams.DocumentReference documentReference)
    {
        return new EventForge.DTOs.Teams.DocumentReferenceDto
        {
            Id = documentReference.Id,
            OwnerId = documentReference.OwnerId,
            OwnerType = documentReference.OwnerType,
            FileName = documentReference.FileName,
            Type = documentReference.Type,
            SubType = documentReference.SubType,
            MimeType = documentReference.MimeType,
            StorageKey = documentReference.StorageKey,
            Url = documentReference.Url,
            ThumbnailStorageKey = documentReference.ThumbnailStorageKey,
            Expiry = documentReference.Expiry,
            FileSizeBytes = documentReference.FileSizeBytes,
            Title = documentReference.Title,
            Notes = documentReference.Notes,
            CreatedAt = documentReference.CreatedAt,
            CreatedBy = documentReference.CreatedBy,
            ModifiedAt = documentReference.ModifiedAt,
            ModifiedBy = documentReference.ModifiedBy
        };
    }

    private StorePosDto MapToStorePosDto(EventForge.Server.Data.Entities.Store.StorePos storePos)
    {
        return new StorePosDto
        {
            Id = storePos.Id,
            Name = storePos.Name,
            Description = storePos.Description,
            Status = (EventForge.DTOs.Common.CashRegisterStatus)storePos.Status,
            Location = storePos.Location,
            LastOpenedAt = storePos.LastOpenedAt,
            Notes = storePos.Notes,
            // Issue #315: Image Management & Extended Fields
            ImageDocumentId = storePos.ImageDocumentId,
            ImageUrl = storePos.ImageDocument?.Url,
            ImageThumbnailUrl = storePos.ImageDocument?.ThumbnailStorageKey,
            TerminalIdentifier = storePos.TerminalIdentifier,
            IPAddress = storePos.IPAddress,
            IsOnline = storePos.IsOnline,
            LastSyncAt = storePos.LastSyncAt,
            LocationLatitude = storePos.LocationLatitude,
            LocationLongitude = storePos.LocationLongitude,
            CurrencyCode = storePos.CurrencyCode,
            TimeZone = storePos.TimeZone,
            CreatedAt = storePos.CreatedAt,
            CreatedBy = storePos.CreatedBy,
            ModifiedAt = storePos.ModifiedAt,
            ModifiedBy = storePos.ModifiedBy
        };
    }

    #endregion
}