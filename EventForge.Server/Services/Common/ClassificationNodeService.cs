using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing classification nodes in a hierarchical structure.
/// </summary>
public class ClassificationNodeService : IClassificationNodeService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ClassificationNodeService> _logger;

    public ClassificationNodeService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ClassificationNodeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ClassificationNodeDto>> GetClassificationNodesAsync(int page = 1, int pageSize = 20, Guid? parentId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in classification node queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for classification node operations.");
            }
            _logger.LogDebug("Getting classification nodes: page={Page}, pageSize={PageSize}, parentId={ParentId}", page, pageSize, parentId);

            var query = _context.ClassificationNodes.AsQueryable();

            if (parentId.HasValue)
            {
                query = query.Where(cn => cn.ParentId == parentId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (page - 1) * pageSize;

            var items = await query
                .Skip(skip)
                .Take(pageSize)
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
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving classification nodes.");
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> GetClassificationNodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting classification node by ID: {Id}", id);

            var node = await _context.ClassificationNodes
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
            _logger.LogError(ex, "Error retrieving classification node with ID {Id}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting root classification nodes");

            var nodes = await _context.ClassificationNodes
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
            _logger.LogError(ex, "Error retrieving root classification nodes.");
            throw;
        }
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting children for classification node: {ParentId}", parentId);

            var children = await _context.ClassificationNodes
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
            _logger.LogError(ex, "Error retrieving children for classification node {ParentId}.", parentId);
            throw;
        }
    }

    public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating classification node: {Name}", createDto.Name);

            // Validate parent exists if specified
            if (createDto.ParentId.HasValue)
            {
                var parentExists = await _context.ClassificationNodes
                    .AnyAsync(cn => cn.Id == createDto.ParentId.Value, cancellationToken);

                if (!parentExists)
                {
                    _logger.LogWarning("Parent classification node with ID {ParentId} not found.", createDto.ParentId);
                    throw new ArgumentException($"Parent classification node with ID {createDto.ParentId} not found.");
                }
            }

            // Check for duplicate code
            if (!string.IsNullOrEmpty(createDto.Code))
            {
                var codeExists = await _context.ClassificationNodes
                    .AnyAsync(cn => cn.Code == createDto.Code, cancellationToken);

                if (codeExists)
                {
                    _logger.LogWarning("Classification node with code '{Code}' already exists.", createDto.Code);
                    throw new ArgumentException($"Classification node with code '{createDto.Code}' already exists.");
                }
            }

            var node = new ClassificationNode
            {
                Id = Guid.NewGuid(),
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Type = createDto.Type?.ToEntity() ?? ProductClassificationType.Category,
                Level = createDto.Level ?? 0,
                Order = createDto.Order ?? 0,
                ParentId = createDto.ParentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.ClassificationNodes.Add(node);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(node, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Created classification node: {Id} - {Name}", node.Id, node.Name);

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
            _logger.LogError(ex, "Error creating classification node.");
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating classification node: {Id}", id);

            var originalNode = await _context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (originalNode == null)
            {
                _logger.LogWarning("Classification node with ID {Id} not found for update.", id);
                return null;
            }

            var node = await _context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (node == null)
            {
                _logger.LogWarning("Classification node with ID {Id} not found for update.", id);
                return null;
            }

            // Validate parent exists if specified and different from current
            if (updateDto.ParentId.HasValue && updateDto.ParentId != node.ParentId)
            {
                if (updateDto.ParentId == id)
                {
                    _logger.LogWarning("A classification node cannot be its own parent. Node ID: {Id}", id);
                    throw new ArgumentException("A classification node cannot be its own parent.");
                }

                var parentExists = await _context.ClassificationNodes
                    .AnyAsync(cn => cn.Id == updateDto.ParentId.Value, cancellationToken);

                if (!parentExists)
                {
                    _logger.LogWarning("Parent classification node with ID {ParentId} not found.", updateDto.ParentId);
                    throw new ArgumentException($"Parent classification node with ID {updateDto.ParentId} not found.");
                }
            }

            // Check for duplicate code if changed
            if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != node.Code)
            {
                var codeExists = await _context.ClassificationNodes
                    .AnyAsync(cn => cn.Code == updateDto.Code && cn.Id != id, cancellationToken);

                if (codeExists)
                {
                    _logger.LogWarning("Classification node with code '{Code}' already exists.", updateDto.Code);
                    throw new ArgumentException($"Classification node with code '{updateDto.Code}' already exists.");
                }
            }

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.Code))
                node.Code = updateDto.Code;
            if (!string.IsNullOrEmpty(updateDto.Name))
                node.Name = updateDto.Name;
            if (updateDto.Description != null)
                node.Description = updateDto.Description;
            if (updateDto.Type.HasValue)
                node.Type = updateDto.Type.Value.ToEntity();
            if (updateDto.Status.HasValue)
                if (updateDto.Level.HasValue)
                    node.Level = updateDto.Level.Value;
            if (updateDto.Order.HasValue)
                node.Order = updateDto.Order.Value;
            if (updateDto.ParentId.HasValue)
                node.ParentId = updateDto.ParentId;

            node.ModifiedAt = DateTime.UtcNow;
            node.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(node, "Update", currentUser, originalNode, cancellationToken);

            _logger.LogInformation("Updated classification node: {Id} - {Name}", node.Id, node.Name);

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
            _logger.LogError(ex, "Error updating classification node {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteClassificationNodeAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting classification node: {Id}", id);

            var originalNode = await _context.ClassificationNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (originalNode == null)
            {
                _logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            var node = await _context.ClassificationNodes
                .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

            if (node == null)
            {
                _logger.LogWarning("Classification node with ID {Id} not found for deletion.", id);
                return false;
            }

            // Check if it has children
            var hasChildren = await _context.ClassificationNodes
                .AnyAsync(cn => cn.ParentId == id, cancellationToken);

            if (hasChildren)
            {
                _logger.LogWarning("Cannot delete classification node {Id} because it has children.", id);
                throw new InvalidOperationException("Cannot delete classification node that has children. Delete or reassign children first.");
            }

            // Gestione concorrenza ottimistica tramite rowVersion
            if (node.RowVersion == null || !node.RowVersion.SequenceEqual(rowVersion))
            {
                _logger.LogWarning("Concurrency conflict when deleting classification node {Id}.", id);
                throw new DbUpdateConcurrencyException("The classification node was modified by another user.");
            }

            // Soft delete
            node.IsDeleted = true;
            node.DeletedAt = DateTime.UtcNow;
            node.DeletedBy = currentUser;
            node.ModifiedAt = DateTime.UtcNow;
            node.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(node, "Delete", currentUser, originalNode, cancellationToken);

            _logger.LogInformation("Deleted classification node: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting classification node {Id}.", id);
            throw;
        }
    }
}