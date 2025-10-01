using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing product models.
/// </summary>
public class ModelService : IModelService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ModelService> _logger;

    public ModelService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ModelService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ModelDto>> GetModelsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var query = _context.Models
                .WhereActiveTenant(currentTenantId.Value)
                .Include(m => m.Brand);

            var totalCount = await query.CountAsync(cancellationToken);
            var models = await query
                .OrderBy(m => m.Brand!.Name)
                .ThenBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var modelDtos = models.Select(MapToModelDto);

            return new PagedResult<ModelDto>
            {
                Items = modelDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models.");
            throw;
        }
    }

    public async Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var query = _context.Models
                .WhereActiveTenant(currentTenantId.Value)
                .Where(m => m.BrandId == brandId)
                .Include(m => m.Brand);

            var totalCount = await query.CountAsync(cancellationToken);
            var models = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var modelDtos = models.Select(MapToModelDto);

            return new PagedResult<ModelDto>
            {
                Items = modelDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for brand {BrandId}.", brandId);
            throw;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _context.Models
                .Where(m => m.Id == id && !m.IsDeleted)
                .Include(m => m.Brand)
                .FirstOrDefaultAsync(cancellationToken);

            return model != null ? MapToModelDto(model) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model {ModelId}.", id);
            throw;
        }
    }

    public async Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createModelDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Verify brand exists
            var brandExists = await _context.Brands
                .Where(b => b.Id == createModelDto.BrandId && !b.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!brandExists)
            {
                throw new ArgumentException($"Brand with ID {createModelDto.BrandId} not found.");
            }

            var model = new Model
            {
                Id = Guid.NewGuid(),
                BrandId = createModelDto.BrandId,
                Name = createModelDto.Name,
                Description = createModelDto.Description,
                ManufacturerPartNumber = createModelDto.ManufacturerPartNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.Models.Add(model);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Model {ModelId} created by {User}.", model.Id, currentUser);

            // Reload with Brand to get brand name
            var createdModel = await _context.Models
                .Include(m => m.Brand)
                .FirstAsync(m => m.Id == model.Id, cancellationToken);

            return MapToModelDto(createdModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating model.");
            throw;
        }
    }

    public async Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateModelDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalModel = await _context.Models
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalModel == null) return null;

            // Verify brand exists
            var brandExists = await _context.Brands
                .Where(b => b.Id == updateModelDto.BrandId && !b.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!brandExists)
            {
                throw new ArgumentException($"Brand with ID {updateModelDto.BrandId} not found.");
            }

            var model = await _context.Models
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null) return null;

            model.BrandId = updateModelDto.BrandId;
            model.Name = updateModelDto.Name;
            model.Description = updateModelDto.Description;
            model.ManufacturerPartNumber = updateModelDto.ManufacturerPartNumber;
            model.ModifiedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Update", currentUser, originalModel, cancellationToken);

            _logger.LogInformation("Model {ModelId} updated by {User}.", model.Id, currentUser);

            // Reload with Brand to get brand name
            var updatedModel = await _context.Models
                .Include(m => m.Brand)
                .FirstAsync(m => m.Id == model.Id, cancellationToken);

            return MapToModelDto(updatedModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model {ModelId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalModel = await _context.Models
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalModel == null) return false;

            var model = await _context.Models
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null) return false;

            model.IsDeleted = true;
            model.ModifiedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Delete", currentUser, originalModel, cancellationToken);

            _logger.LogInformation("Model {ModelId} deleted by {User}.", model.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelId}.", id);
            throw;
        }
    }

    public async Task<bool> ModelExistsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Models
                .Where(m => m.Id == modelId && !m.IsDeleted)
                .AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if model {ModelId} exists.", modelId);
            throw;
        }
    }

    private static ModelDto MapToModelDto(Model model)
    {
        return new ModelDto
        {
            Id = model.Id,
            BrandId = model.BrandId,
            BrandName = model.Brand?.Name,
            Name = model.Name,
            Description = model.Description,
            ManufacturerPartNumber = model.ManufacturerPartNumber,
            CreatedAt = model.CreatedAt,
            CreatedBy = model.CreatedBy
        };
    }
}
