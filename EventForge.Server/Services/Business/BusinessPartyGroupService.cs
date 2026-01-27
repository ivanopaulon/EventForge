using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service implementation for managing Business Party Groups.
/// </summary>
public class BusinessPartyGroupService : IBusinessPartyGroupService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BusinessPartyGroupService> _logger;

    public BusinessPartyGroupService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<BusinessPartyGroupService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region CRUD Groups

    public async Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(
        int page = 1, 
        int pageSize = 20, 
        BusinessPartyGroupType? groupType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var query = _context.BusinessPartyGroups
                .WhereActiveTenant(currentTenantId.Value)
                .Include(g => g.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value))
                .AsQueryable();

            if (groupType.HasValue)
            {
                query = query.Where(g => g.GroupType == groupType.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var groups = await query
                .OrderBy(g => g.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var groupDtos = groups.Select(MapToGroupDto);

            return new PagedResult<BusinessPartyGroupDto>
            {
                Items = groupDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Business Party Groups.");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var group = await _context.BusinessPartyGroups
                .Where(g => g.Id == id && g.TenantId == currentTenantId.Value && !g.IsDeleted)
                .Include(g => g.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value))
                .FirstOrDefaultAsync(cancellationToken);

            return group != null ? MapToGroupDto(group) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Business Party Group {GroupId}.", id);
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto> CreateGroupAsync(
        CreateBusinessPartyGroupDto dto, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            // Check for duplicate code if provided
            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var codeExists = await _context.BusinessPartyGroups
                    .AnyAsync(g => g.Code == dto.Code && g.TenantId == currentTenantId.Value && !g.IsDeleted, cancellationToken);
                
                if (codeExists)
                {
                    throw new InvalidOperationException($"A Business Party Group with code '{dto.Code}' already exists.");
                }
            }

            var group = new Data.Entities.Business.BusinessPartyGroup
            {
                TenantId = currentTenantId.Value,
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                GroupType = dto.GroupType,
                ColorHex = dto.ColorHex ?? "#1976D2",
                Icon = dto.Icon ?? "Group",
                Priority = dto.Priority,
                IsActive = dto.IsActive,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.BusinessPartyGroups.Add(group);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(group, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Business Party Group {GroupId} created by {User}.", group.Id, currentUser);

            return MapToGroupDto(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Business Party Group.");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto> UpdateGroupAsync(
        Guid id, 
        UpdateBusinessPartyGroupDto dto, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var originalGroup = await _context.BusinessPartyGroups
                .AsNoTracking()
                .Where(g => g.Id == id && g.TenantId == currentTenantId.Value && !g.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalGroup == null)
            {
                throw new InvalidOperationException($"Business Party Group with ID {id} not found.");
            }

            // Check for duplicate code if provided and changed
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != originalGroup.Code)
            {
                var codeExists = await _context.BusinessPartyGroups
                    .AnyAsync(g => g.Code == dto.Code && g.Id != id && g.TenantId == currentTenantId.Value && !g.IsDeleted, cancellationToken);
                
                if (codeExists)
                {
                    throw new InvalidOperationException($"A Business Party Group with code '{dto.Code}' already exists.");
                }
            }

            var group = await _context.BusinessPartyGroups
                .Where(g => g.Id == id && g.TenantId == currentTenantId.Value && !g.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (group == null)
            {
                throw new InvalidOperationException($"Business Party Group with ID {id} not found.");
            }

            group.Name = dto.Name;
            group.Code = dto.Code;
            group.Description = dto.Description;
            group.GroupType = dto.GroupType;
            group.ColorHex = dto.ColorHex;
            group.Icon = dto.Icon;
            group.Priority = dto.Priority;
            group.IsActive = dto.IsActive;
            group.ValidFrom = dto.ValidFrom;
            group.ValidTo = dto.ValidTo;
            group.ModifiedAt = DateTime.UtcNow;
            group.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(group, "Update", currentUser, originalGroup, cancellationToken);

            _logger.LogInformation("Business Party Group {GroupId} updated by {User}.", group.Id, currentUser);

            return MapToGroupDto(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Business Party Group {GroupId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var originalGroup = await _context.BusinessPartyGroups
                .AsNoTracking()
                .Where(g => g.Id == id && g.TenantId == currentTenantId.Value && !g.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalGroup == null)
            {
                _logger.LogWarning("Business Party Group with ID {GroupId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var group = await _context.BusinessPartyGroups
                .Where(g => g.Id == id && g.TenantId == currentTenantId.Value && !g.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (group == null)
            {
                _logger.LogWarning("Business Party Group with ID {GroupId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            group.IsDeleted = true;
            group.DeletedAt = DateTime.UtcNow;
            group.DeletedBy = currentUser;
            group.ModifiedAt = DateTime.UtcNow;
            group.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(group, "Delete", currentUser, originalGroup, cancellationToken);

            _logger.LogInformation("Business Party Group {GroupId} deleted by {User}.", group.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Business Party Group {GroupId}.", id);
            throw;
        }
    }

    #endregion

    #region Group Members Management

    public async Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(
        Guid groupId, 
        int page = 1, 
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var query = _context.BusinessPartyGroupMembers
                .Where(m => m.BusinessPartyGroupId == groupId && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .Include(m => m.Group)
                .Include(m => m.BusinessParty);

            var totalCount = await query.CountAsync(cancellationToken);
            var members = await query
                .OrderBy(m => m.BusinessParty.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var memberDtos = members.Select(MapToMemberDto);

            return new PagedResult<BusinessPartyGroupMemberDto>
            {
                Items = memberDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for Business Party Group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> AddMemberToGroupAsync(
        Guid groupId, 
        AddBusinessPartyToGroupDto dto, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            // Verify group exists
            var groupExists = await _context.BusinessPartyGroups
                .AnyAsync(g => g.Id == groupId && g.TenantId == currentTenantId.Value && !g.IsDeleted, cancellationToken);

            if (!groupExists)
            {
                throw new InvalidOperationException($"Business Party Group with ID {groupId} not found.");
            }

            // Verify business party exists
            var businessPartyExists = await _context.BusinessParties
                .AnyAsync(bp => bp.Id == dto.BusinessPartyId && bp.TenantId == currentTenantId.Value && !bp.IsDeleted, cancellationToken);

            if (!businessPartyExists)
            {
                throw new InvalidOperationException($"Business Party with ID {dto.BusinessPartyId} not found.");
            }

            // Check if already a member
            var existingMember = await _context.BusinessPartyGroupMembers
                .Where(m => m.BusinessPartyGroupId == groupId 
                    && m.BusinessPartyId == dto.BusinessPartyId 
                    && m.TenantId == currentTenantId.Value 
                    && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingMember != null)
            {
                throw new InvalidOperationException("Business Party is already a member of this group.");
            }

            var member = new Data.Entities.Business.BusinessPartyGroupMember
            {
                TenantId = currentTenantId.Value,
                BusinessPartyGroupId = groupId,
                BusinessPartyId = dto.BusinessPartyId,
                MemberSince = dto.MemberSince ?? DateTime.UtcNow,
                MemberUntil = dto.MemberUntil,
                Status = BusinessPartyGroupMemberStatus.Active,
                OverridePriority = dto.OverridePriority,
                Notes = dto.Notes,
                IsFeatured = dto.IsFeatured,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.BusinessPartyGroupMembers.Add(member);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(member, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Business Party {BPId} added to Group {GroupId} by {User}.", dto.BusinessPartyId, groupId, currentUser);

            // Load navigation properties for DTO mapping
            await _context.Entry(member).Reference(m => m.Group).LoadAsync(cancellationToken);
            await _context.Entry(member).Reference(m => m.BusinessParty).LoadAsync(cancellationToken);

            return MapToMemberDto(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to Business Party Group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<BulkOperationResultDto> BulkAddMembersAsync(
        BulkAddMembersDto dto, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var result = new BulkOperationResultDto();

            // Verify group exists
            var groupExists = await _context.BusinessPartyGroups
                .AnyAsync(g => g.Id == dto.BusinessPartyGroupId && g.TenantId == currentTenantId.Value && !g.IsDeleted, cancellationToken);

            if (!groupExists)
            {
                result.Errors.Add($"Business Party Group with ID {dto.BusinessPartyGroupId} not found.");
                result.FailureCount = dto.BusinessPartyIds.Count;
                return result;
            }

            foreach (var businessPartyId in dto.BusinessPartyIds)
            {
                try
                {
                    // Verify business party exists
                    var businessPartyExists = await _context.BusinessParties
                        .AnyAsync(bp => bp.Id == businessPartyId && bp.TenantId == currentTenantId.Value && !bp.IsDeleted, cancellationToken);

                    if (!businessPartyExists)
                    {
                        result.Errors.Add($"Business Party {businessPartyId} not found.");
                        result.FailureCount++;
                        continue;
                    }

                    // Check if already a member
                    var existingMember = await _context.BusinessPartyGroupMembers
                        .AnyAsync(m => m.BusinessPartyGroupId == dto.BusinessPartyGroupId 
                            && m.BusinessPartyId == businessPartyId 
                            && m.TenantId == currentTenantId.Value 
                            && !m.IsDeleted, cancellationToken);

                    if (existingMember)
                    {
                        result.Errors.Add($"Business Party {businessPartyId} is already a member.");
                        result.FailureCount++;
                        continue;
                    }

                    var member = new Data.Entities.Business.BusinessPartyGroupMember
                    {
                        TenantId = currentTenantId.Value,
                        BusinessPartyGroupId = dto.BusinessPartyGroupId,
                        BusinessPartyId = businessPartyId,
                        MemberSince = dto.MemberSince ?? DateTime.UtcNow,
                        MemberUntil = dto.MemberUntil,
                        Status = BusinessPartyGroupMemberStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };

                    _context.BusinessPartyGroupMembers.Add(member);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding Business Party {BPId} to group {GroupId}.", businessPartyId, dto.BusinessPartyGroupId);
                    result.Errors.Add($"Error adding Business Party {businessPartyId}: {ex.Message}");
                    result.FailureCount++;
                }
            }

            if (result.SuccessCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Bulk add members to Group {GroupId}: {Success} succeeded, {Failure} failed.", 
                dto.BusinessPartyGroupId, result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk add members operation.");
            throw;
        }
    }

    public async Task<bool> RemoveMemberFromGroupAsync(
        Guid groupId, 
        Guid businessPartyId, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var originalMember = await _context.BusinessPartyGroupMembers
                .AsNoTracking()
                .Where(m => m.BusinessPartyGroupId == groupId 
                    && m.BusinessPartyId == businessPartyId 
                    && m.TenantId == currentTenantId.Value 
                    && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalMember == null)
            {
                _logger.LogWarning("Member not found in group {GroupId}.", groupId);
                return false;
            }

            var member = await _context.BusinessPartyGroupMembers
                .Where(m => m.BusinessPartyGroupId == groupId 
                    && m.BusinessPartyId == businessPartyId 
                    && m.TenantId == currentTenantId.Value 
                    && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Member not found in group {GroupId}.", groupId);
                return false;
            }

            member.IsDeleted = true;
            member.DeletedAt = DateTime.UtcNow;
            member.DeletedBy = currentUser;
            member.ModifiedAt = DateTime.UtcNow;
            member.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);

            _logger.LogInformation("Business Party {BPId} removed from Group {GroupId} by {User}.", businessPartyId, groupId, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from Business Party Group {GroupId}.", groupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> UpdateMembershipAsync(
        Guid membershipId, 
        UpdateBusinessPartyGroupMemberDto dto, 
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var originalMember = await _context.BusinessPartyGroupMembers
                .AsNoTracking()
                .Where(m => m.Id == membershipId && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalMember == null)
            {
                throw new InvalidOperationException($"Membership with ID {membershipId} not found.");
            }

            var member = await _context.BusinessPartyGroupMembers
                .Include(m => m.Group)
                .Include(m => m.BusinessParty)
                .Where(m => m.Id == membershipId && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                throw new InvalidOperationException($"Membership with ID {membershipId} not found.");
            }

            member.MemberSince = dto.MemberSince;
            member.MemberUntil = dto.MemberUntil;
            member.Status = dto.Status;
            member.OverridePriority = dto.OverridePriority;
            member.Notes = dto.Notes;
            member.IsFeatured = dto.IsFeatured;
            member.ModifiedAt = DateTime.UtcNow;
            member.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(member, "Update", currentUser, originalMember, cancellationToken);

            _logger.LogInformation("Membership {MembershipId} updated by {User}.", membershipId, currentUser);

            return MapToMemberDto(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership {MembershipId}.", membershipId);
            throw;
        }
    }

    #endregion

    #region Query Helpers

    public async Task<List<BusinessPartyGroupDto>> GetGroupsForBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var groups = await _context.BusinessPartyGroupMembers
                .Where(m => m.BusinessPartyId == businessPartyId && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .Include(m => m.Group.Members.Where(mem => !mem.IsDeleted && mem.TenantId == currentTenantId.Value))
                .Select(m => m.Group)
                .Where(g => !g.IsDeleted)
                .Distinct()
                .ToListAsync(cancellationToken);

            return groups.Select(MapToGroupDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for Business Party {BPId}.", businessPartyId);
            throw;
        }
    }

    public async Task<List<Guid>> GetActiveGroupIdsForBusinessPartyAsync(
        Guid businessPartyId,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            var evalDate = evaluationDate ?? DateTime.UtcNow;

            var groupIds = await _context.BusinessPartyGroupMembers
                .Where(m => m.BusinessPartyId == businessPartyId 
                    && m.TenantId == currentTenantId.Value 
                    && !m.IsDeleted
                    && m.Status == BusinessPartyGroupMemberStatus.Active
                    && (!m.MemberSince.HasValue || m.MemberSince.Value <= evalDate)
                    && (!m.MemberUntil.HasValue || m.MemberUntil.Value >= evalDate))
                .Include(m => m.Group)
                .Where(m => !m.Group.IsDeleted 
                    && m.Group.IsActive
                    && (!m.Group.ValidFrom.HasValue || m.Group.ValidFrom.Value <= evalDate)
                    && (!m.Group.ValidTo.HasValue || m.Group.ValidTo.Value >= evalDate))
                .Select(m => m.BusinessPartyGroupId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return groupIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active group IDs for Business Party {BPId}.", businessPartyId);
            throw;
        }
    }

    public async Task<bool> IsBusinessPartyInGroupAsync(
        Guid businessPartyId, 
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for Business Party Group operations.");
            }

            return await _context.BusinessPartyGroupMembers
                .AnyAsync(m => m.BusinessPartyId == businessPartyId 
                    && m.BusinessPartyGroupId == groupId 
                    && m.TenantId == currentTenantId.Value 
                    && !m.IsDeleted
                    && m.Status == BusinessPartyGroupMemberStatus.Active, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Business Party {BPId} is in Group {GroupId}.", businessPartyId, groupId);
            throw;
        }
    }

    #endregion

    #region Private Mapping Methods

    private static BusinessPartyGroupDto MapToGroupDto(Data.Entities.Business.BusinessPartyGroup group)
    {
        var now = DateTime.UtcNow;
        var activeMembers = group.Members.Count(m => 
            !m.IsDeleted 
            && m.Status == BusinessPartyGroupMemberStatus.Active
            && (!m.MemberSince.HasValue || m.MemberSince.Value <= now)
            && (!m.MemberUntil.HasValue || m.MemberUntil.Value >= now));

        return new BusinessPartyGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Code = group.Code,
            Description = group.Description,
            GroupType = group.GroupType,
            ColorHex = group.ColorHex,
            Icon = group.Icon,
            Priority = group.Priority,
            IsActive = group.IsActive,
            ValidFrom = group.ValidFrom,
            ValidTo = group.ValidTo,
            ActiveMembersCount = activeMembers,
            TotalMembersCount = group.Members.Count(m => !m.IsDeleted),
            CreatedAt = group.CreatedAt,
            CreatedBy = group.CreatedBy,
            ModifiedAt = group.ModifiedAt,
            ModifiedBy = group.ModifiedBy
        };
    }

    private static BusinessPartyGroupMemberDto MapToMemberDto(Data.Entities.Business.BusinessPartyGroupMember member)
    {
        return new BusinessPartyGroupMemberDto
        {
            Id = member.Id,
            BusinessPartyGroupId = member.BusinessPartyGroupId,
            GroupName = member.Group?.Name ?? string.Empty,
            GroupColorHex = member.Group?.ColorHex,
            GroupIcon = member.Group?.Icon,
            BusinessPartyId = member.BusinessPartyId,
            BusinessPartyName = member.BusinessParty?.Name ?? string.Empty,
            BusinessPartyType = (DTOs.Common.BusinessPartyType)(member.BusinessParty?.PartyType ?? Data.Entities.Business.BusinessPartyType.Cliente),
            MemberSince = member.MemberSince,
            MemberUntil = member.MemberUntil,
            Status = member.Status,
            OverridePriority = member.OverridePriority,
            Notes = member.Notes,
            IsFeatured = member.IsFeatured,
            CreatedAt = member.CreatedAt,
            CreatedBy = member.CreatedBy
        };
    }

    #endregion
}
