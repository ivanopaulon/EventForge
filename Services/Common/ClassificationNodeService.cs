using EventForge.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Common;

/// <summary>
/// Service implementation for managing classification nodes in a hierarchical structure.
/// </summary>
public class ClassificationNodeService : IClassificationNodeService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<ClassificationNodeService> _logger;

    public ClassificationNodeService(EventForgeDbContext context, ILogger<ClassificationNodeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ClassificationNodeDto>> GetClassificationNodesAsync(int page = 1, int pageSize = 20, Guid? parentId = null, CancellationToken cancellationToken = default)
    {
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
                Type = cn.Type,
                Status = cn.Status,
                Level = cn.Level,
                Order = cn.Order,
                ParentId = cn.ParentId,
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

    public async Task<ClassificationNodeDto?> GetClassificationNodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
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
                Type = cn.Type,
                Status = cn.Status,
                Level = cn.Level,
                Order = cn.Order,
                ParentId = cn.ParentId,
                CreatedAt = cn.CreatedAt,
                CreatedBy = cn.CreatedBy,
                ModifiedAt = cn.ModifiedAt,
                ModifiedBy = cn.ModifiedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return node;
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken cancellationToken = default)
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
                Type = cn.Type,
                Status = cn.Status,
                Level = cn.Level,
                Order = cn.Order,
                ParentId = cn.ParentId,
                CreatedAt = cn.CreatedAt,
                CreatedBy = cn.CreatedBy,
                ModifiedAt = cn.ModifiedAt,
                ModifiedBy = cn.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return nodes;
    }

    public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
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
                Type = cn.Type,
                Status = cn.Status,
                Level = cn.Level,
                Order = cn.Order,
                ParentId = cn.ParentId,
                CreatedAt = cn.CreatedAt,
                CreatedBy = cn.CreatedBy,
                ModifiedAt = cn.ModifiedAt,
                ModifiedBy = cn.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return children;
    }

    public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating classification node: {Name}", createDto.Name);

        // Validate parent exists if specified
        if (createDto.ParentId.HasValue)
        {
            var parentExists = await _context.ClassificationNodes
                .AnyAsync(cn => cn.Id == createDto.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
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
                throw new ArgumentException($"Classification node with code '{createDto.Code}' already exists.");
            }
        }

        var node = new ClassificationNode
        {
            Id = Guid.NewGuid(),
            Code = createDto.Code,
            Name = createDto.Name,
            Description = createDto.Description,
            Type = createDto.Type ?? ProductClassificationType.Category,
            Status = createDto.Status ?? ProductClassificationNodeStatus.Active,
            Level = createDto.Level ?? 0,
            Order = createDto.Order ?? 0,
            ParentId = createDto.ParentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        _context.ClassificationNodes.Add(node);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created classification node: {Id} - {Name}", node.Id, node.Name);

        return new ClassificationNodeDto
        {
            Id = node.Id,
            Code = node.Code,
            Name = node.Name,
            Description = node.Description,
            Type = node.Type,
            Status = node.Status,
            Level = node.Level,
            Order = node.Order,
            ParentId = node.ParentId,
            CreatedAt = node.CreatedAt,
            CreatedBy = node.CreatedBy,
            ModifiedAt = node.ModifiedAt,
            ModifiedBy = node.ModifiedBy
        };
    }

    public async Task<ClassificationNodeDto?> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating classification node: {Id}", id);

        var node = await _context.ClassificationNodes.FindAsync(new object[] { id }, cancellationToken);
        if (node == null)
        {
            return null;
        }

        // Validate parent exists if specified and different from current
        if (updateDto.ParentId.HasValue && updateDto.ParentId != node.ParentId)
        {
            // Prevent circular reference
            if (updateDto.ParentId == id)
            {
                throw new ArgumentException("A classification node cannot be its own parent.");
            }

            var parentExists = await _context.ClassificationNodes
                .AnyAsync(cn => cn.Id == updateDto.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
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
            node.Type = updateDto.Type.Value;
        if (updateDto.Status.HasValue)
            node.Status = updateDto.Status.Value;
        if (updateDto.Level.HasValue)
            node.Level = updateDto.Level.Value;
        if (updateDto.Order.HasValue)
            node.Order = updateDto.Order.Value;
        if (updateDto.ParentId.HasValue)
            node.ParentId = updateDto.ParentId;

        node.ModifiedAt = DateTime.UtcNow;
        node.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated classification node: {Id} - {Name}", node.Id, node.Name);

        return new ClassificationNodeDto
        {
            Id = node.Id,
            Code = node.Code,
            Name = node.Name,
            Description = node.Description,
            Type = node.Type,
            Status = node.Status,
            Level = node.Level,
            Order = node.Order,
            ParentId = node.ParentId,
            CreatedAt = node.CreatedAt,
            CreatedBy = node.CreatedBy,
            ModifiedAt = node.ModifiedAt,
            ModifiedBy = node.ModifiedBy
        };
    }

    public async Task<bool> DeleteClassificationNodeAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting classification node: {Id}", id);

        var node = await _context.ClassificationNodes.FindAsync(new object[] { id }, cancellationToken);
        if (node == null)
        {
            return false;
        }

        // Check if it has children
        var hasChildren = await _context.ClassificationNodes
            .AnyAsync(cn => cn.ParentId == id, cancellationToken);

        if (hasChildren)
        {
            throw new InvalidOperationException("Cannot delete classification node that has children. Delete or reassign children first.");
        }

        // Soft delete
        node.IsDeleted = true;
        node.DeletedAt = DateTime.UtcNow;
        node.DeletedBy = currentUser;
        node.ModifiedAt = DateTime.UtcNow;
        node.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted classification node: {Id}", id);
        return true;
    }
}