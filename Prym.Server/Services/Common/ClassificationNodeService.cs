using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Common;

/// <summary>
/// Service implementation for managing classification nodes in a hierarchical structure.
/// </summary>
public class ClassificationNodeService(
    PrymDbContext context,
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
                .AsQueryable();

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving classification nodes.");
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> GetClassificationNodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting classification node by ID: {Id}", id);

            var node = await context.ClassificationNodes
                .Where(cn => cn.Id == id)
                .Select(cn => new ClassificationNodeDto
                {
                    Id = cn.Id,
                    Code = cn.Code,
                    Name = cn.Name,
                    Description = cn.Description,
                    Type = cn.Type.ToDto(),
                    Status = cn.Status.ToDto(),
                    Level = cn.Level,
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving classification node with ID {Id}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting root classification nodes");

            var nodes = await context.ClassificationNodes
                .Where(cn => cn.ParentId == null)
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving root classification nodes.");
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting children for classification node: {ParentId}", parentId);

            var children = await context.ClassificationNodes
                .Where(cn => cn.ParentId == parentId)
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving children for classification node {ParentId}.", parentId);
            throw;
        }
    }

    public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Creating classification node: {Name}", createDto.Name);

            // Validate parent exists if specified
            if (createDto.ParentId.HasValue)
            {
                var parentExists = await context.ClassificationNodes
                    .AnyAsync(cn => cn.Id == createDto.ParentId.Value, cancellationToken);

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
                    .AnyAsync(cn => cn.Code == createDto.Code, cancellationToken);

                if (codeExists)
                {
                    logger.LogWarning("Classification node with code '{Code}' already exists.", createDto.Code);
                    throw new ArgumentException($"Classification node with code '{createDto.Code}' already exists.");
                }
            }

            var node = new ClassificationNode
            {
                Id = Guid.NewGuid(),
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Type = createDto.Type.ToEntity(),
                Status = createDto.Status.ToEntity(),
                Level = createDto.Level,
                Order = createDto.Order,
                ParentId = createDto.ParentId,
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
                Order = node.Order,
                ParentId = node.ParentId,
                IsActive = node.IsActive,
                CreatedAt = node.CreatedAt,
                CreatedBy = node.CreatedBy,
                ModifiedAt = node.ModifiedAt,
                ModifiedBy = node.ModifiedBy
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating classification node.");
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Updating classification node: {Id}", id);

            var originalNode = await context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (originalNode is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for update.", id);
                return null;
            }

            var node = await context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

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
                    .AnyAsync(cn => cn.Id == updateDto.ParentId.Value, cancellationToken);

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
                    .AnyAsync(cn => cn.Code == updateDto.Code && cn.Id != id, cancellationToken);

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
                Order = node.Order,
                ParentId = node.ParentId,
                IsActive = node.IsActive,
                CreatedAt = node.CreatedAt,
                CreatedBy = node.CreatedBy,
                ModifiedAt = node.ModifiedAt,
                ModifiedBy = node.ModifiedBy
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating classification node {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteClassificationNodeAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Deleting classification node: {Id}", id);

            var originalNode = await context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (originalNode is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            var node = await context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (node is null)
            {
                logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            // Check if it has children
            var hasChildren = await context.ClassificationNodes
                .AnyAsync(cn => cn.ParentId == id, cancellationToken);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting classification node {Id}.", id);
            throw;
        }
    }

}
