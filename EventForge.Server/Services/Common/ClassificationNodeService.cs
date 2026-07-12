using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing classification nodes in a hierarchical structure.
/// </summary>
public class ClassificationNodeService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<ClassificationNodeService> logger) : IClassificationNodeService
{

    public async Task<PagedResult<ClassificationNodeDto>> GetClassificationNodesAsync(PaginationParameters pagination, Guid? parentId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for classification node operations.");
            }
            logger.LogDebug("Getting classification nodes: page={Page}, pageSize={PageSize}, parentId={ParentId}", pagination.Page, pagination.PageSize, parentId);

            var query = context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => cn.TenantId == currentTenantId.Value);

            if (parentId.HasValue)
            {
                query = query.Where(cn => cn.ParentId == parentId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
                    ApplicableTo = cn.ApplicableTo,
                    Order = cn.Order,
                    ParentId = cn.ParentId,
                    IsActive = cn.IsActive,
                    CreatedAt = cn.CreatedAt,
                    CreatedBy = cn.CreatedBy,
                    ModifiedAt = cn.ModifiedAt,
                    ModifiedBy = cn.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ClassificationNodeDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> GetClassificationNodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting classification node by ID: {Id}", id);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            var node = await context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => cn.Id == id && cn.TenantId == currentTenantId)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
                    ApplicableTo = cn.ApplicableTo,
                    Order = cn.Order,
                    ParentId = cn.ParentId,
                    IsActive = cn.IsActive,
                    CreatedAt = cn.CreatedAt,
                    CreatedBy = cn.CreatedBy,
                    ModifiedAt = cn.ModifiedAt,
                    ModifiedBy = cn.ModifiedBy
                })
                .FirstOrDefaultAsync(cancellationToken);

            return node;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting root classification nodes");

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            var nodes = await context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => cn.ParentId == null && cn.TenantId == currentTenantId)
                .OrderBy(cn => cn.Order)
                .ThenBy(cn => cn.Name)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
                    ApplicableTo = cn.ApplicableTo,
                    Order = cn.Order,
                    ParentId = cn.ParentId,
                    IsActive = cn.IsActive,
                    CreatedAt = cn.CreatedAt,
                    CreatedBy = cn.CreatedBy,
                    ModifiedAt = cn.ModifiedAt,
                    ModifiedBy = cn.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            return nodes;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting children for classification node: {ParentId}", parentId);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            var children = await context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => cn.ParentId == parentId && cn.TenantId == currentTenantId)
                .OrderBy(cn => cn.Order)
                .ThenBy(cn => cn.Name)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
                    ApplicableTo = cn.ApplicableTo,
                    Order = cn.Order,
                    ParentId = cn.ParentId,
                    IsActive = cn.IsActive,
                    CreatedAt = cn.CreatedAt,
                    CreatedBy = cn.CreatedBy,
                    ModifiedAt = cn.ModifiedAt,
                    ModifiedBy = cn.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            return children;
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Creating classification node: {Name}", createDto.Name);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            // Validate parent exists if specified
            if (createDto.ParentId.HasValue)
            {
                var parentExists = await context.ClassificationNodes
                    .AsNoTracking()
                    .AnyAsync(cn => cn.Id == createDto.ParentId.Value && cn.TenantId == currentTenantId, cancellationToken);

                if (!parentExists)
                {
                    logger.LogWarning("Parent classification node with ID {ParentId} not found.", createDto.ParentId);
                    throw new ArgumentException($"Parent classification node with ID {createDto.ParentId} not found.");
                }
            }

            // Check for duplicate code
            if (!string.IsNullOrEmpty(createDto.Code))
            {
                var codeExists = await context.ClassificationNodes
                    .AsNoTracking()
                    .AnyAsync(cn => cn.Code == createDto.Code && cn.TenantId == currentTenantId, cancellationToken);

                if (codeExists)
                {
                    logger.LogWarning("Classification node with code '{Code}' already exists.", createDto.Code);
                    throw new ArgumentException($"Classification node with code '{createDto.Code}' already exists.");
                }
            }

            var node = new ClassificationNode
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Type = createDto.Type.ToEntity(),
                Status = createDto.Status.ToEntity(),
                Level = createDto.Level,
                Order = createDto.Order,
                ParentId = createDto.ParentId,
                ApplicableTo = createDto.ApplicableTo,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.ClassificationNodes.Add(node);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(node, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Created classification node: {Id} - {Name}", node.Id, node.Name);

            return new ClassificationNodeDto
            {
                Id = node.Id,
                Code = node.Code,
                Name = node.Name,
                Description = node.Description,
                Type = node.Type.ToDto(),
                Status = node.Status.ToDto(),
                Level = node.Level,
                ApplicableTo = node.ApplicableTo,
                Order = node.Order,
                ParentId = node.ParentId,
                IsActive = node.IsActive,
                CreatedAt = node.CreatedAt,
                CreatedBy = node.CreatedBy,
                ModifiedAt = node.ModifiedAt,
                ModifiedBy = node.ModifiedBy
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Updating classification node: {Id}", id);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            var originalNode = await context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id && cn.TenantId == currentTenantId, cancellationToken);

            if (originalNode is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for update.", id);
                return null;
            }

            var node = await context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id && cn.TenantId == currentTenantId, cancellationToken);

            if (node is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for update.", id);
                return null;
            }

            // Validate parent exists if specified and different from current
            if (updateDto.ParentId.HasValue && updateDto.ParentId != node.ParentId)
            {
                if (updateDto.ParentId == id)
                {
                    logger.LogWarning("A classification node cannot be its own parent. Node ID: {Id}", id);
                    throw new ArgumentException("A classification node cannot be its own parent.");
                }

                var parentExists = await context.ClassificationNodes
                    .AsNoTracking()
                    .AnyAsync(cn => cn.Id == updateDto.ParentId.Value && cn.TenantId == currentTenantId, cancellationToken);

                if (!parentExists)
                {
                    logger.LogWarning("Parent classification node with ID {ParentId} not found.", updateDto.ParentId);
                    throw new ArgumentException($"Parent classification node with ID {updateDto.ParentId} not found.");
                }
            }

            // Check for duplicate code if changed
            if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != node.Code)
            {
                var codeExists = await context.ClassificationNodes
                    .AsNoTracking()
                    .AnyAsync(cn => cn.Code == updateDto.Code && cn.Id != id && cn.TenantId == currentTenantId, cancellationToken);

                if (codeExists)
                {
                    logger.LogWarning("Classification node with code '{Code}' already exists.", updateDto.Code);
                    throw new ArgumentException($"Classification node with code '{updateDto.Code}' already exists.");
                }
            }

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.Code))
                node.Code = updateDto.Code;
            if (!string.IsNullOrEmpty(updateDto.Name))
                node.Name = updateDto.Name;
            if (updateDto.Description is not null)
                node.Description = updateDto.Description;

            node.Type = updateDto.Type.ToEntity();
            node.Status = updateDto.Status.ToEntity();
            node.Level = updateDto.Level;
            node.Order = updateDto.Order;
            node.ApplicableTo = updateDto.ApplicableTo;

            if (updateDto.ParentId.HasValue)
                node.ParentId = updateDto.ParentId;

            node.ModifiedAt = DateTime.UtcNow;
            node.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(node, "Update", currentUser, originalNode, cancellationToken);

            logger.LogInformation("Updated classification node: {Id} - {Name}", node.Id, node.Name);

            return new ClassificationNodeDto
            {
                Id = node.Id,
                Code = node.Code,
                Name = node.Name,
                Description = node.Description,
                Type = node.Type.ToDto(),
                Status = node.Status.ToDto(),
                Level = node.Level,
                ApplicableTo = node.ApplicableTo,
                Order = node.Order,
                ParentId = node.ParentId,
                IsActive = node.IsActive,
                CreatedAt = node.CreatedAt,
                CreatedBy = node.CreatedBy,
                ModifiedAt = node.ModifiedAt,
                ModifiedBy = node.ModifiedBy
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting full classification node tree");

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for classification node operations.");
            }

            // Load ALL non-deleted nodes in a single query
            var allNodes = await context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => cn.TenantId == currentTenantId.Value)
                .OrderBy(cn => cn.Order)
                .ThenBy(cn => cn.Name)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
                    ApplicableTo = cn.ApplicableTo,
                    Order = cn.Order,
                    ParentId = cn.ParentId,
                    IsActive = cn.IsActive,
                    CreatedAt = cn.CreatedAt,
                    CreatedBy = cn.CreatedBy,
                    ModifiedAt = cn.ModifiedAt,
                    ModifiedBy = cn.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            // Build the tree in memory
            var lookup = allNodes.ToDictionary(n => n.Id);
            var roots = new List<ClassificationNodeDto>();

            foreach (var node in allNodes)
            {
                if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteClassificationNodeAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Deleting classification node: {Id}", id);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for classification node operations.");

            var originalNode = await context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id && cn.TenantId == currentTenantId, cancellationToken);

            if (originalNode is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            var node = await context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id && cn.TenantId == currentTenantId, cancellationToken);

            if (node is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            // Check if it has children
            var hasChildren = await context.ClassificationNodes
                .AsNoTracking()
                .AnyAsync(cn => cn.ParentId == id && cn.TenantId == currentTenantId, cancellationToken);

            if (hasChildren)
            {
                logger.LogWarning("Cannot delete classification node {Id} because it has children.", id);
                throw new InvalidOperationException("Cannot delete classification node that has children. Delete or reassign children first.");
            }

            // Gestione concorrenza ottimistica tramite rowVersion
            if (node.RowVersion is null || !node.RowVersion.SequenceEqual(rowVersion))
            {
                logger.LogWarning("Concurrency conflict when deleting classification node {Id}.", id);
                throw new DbUpdateConcurrencyException("The classification node was modified by another user.");
            }

            // Soft delete
            node.IsDeleted = true;
            node.DeletedAt = DateTime.UtcNow;
            node.DeletedBy = currentUser;
            node.ModifiedAt = DateTime.UtcNow;
            node.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(node, "Delete", currentUser, originalNode, cancellationToken);

            logger.LogInformation("Deleted classification node: {Id}", id);
            return true;
        }
        catch
        {
            throw;
        }
    }

}
