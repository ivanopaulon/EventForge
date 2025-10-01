using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing models.
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

            var skip = (page - 1) * pageSize;

            var totalCount = await _context.Models
                .WhereActiveTenant(currentTenantId.Value)
                .LongCountAsync(cancellationToken);

            var entities = await _context.Models
                .WhereActiveTenant(currentTenantId.Value)
                .Include(m => m.Brand)
                .OrderBy(m => m.Brand!.Name)
                .ThenBy(m => m.Name)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = ModelMapper.ToDtoList(entities);

            return new PagedResult<ModelDto>
            {
                Items = dtos,
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

    public async Task<IEnumerable<ModelDto>> GetModelsByBrandAsync(Guid brandId, CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _context.Models
                .Where(m => m.BrandId == brandId && !m.IsDeleted)
                .Include(m => m.Brand)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            return ModelMapper.ToDtoCollection(models);
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

            return model != null ? ModelMapper.ToDto(model) : null;
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            // Validate that the brand exists
            var brandExists = await _context.Brands
                .AnyAsync(b => b.Id == createModelDto.BrandId && !b.IsDeleted, cancellationToken);

            if (!brandExists)
            {
                throw new InvalidOperationException($"Brand with ID {createModelDto.BrandId} does not exist.");
            }

            var model = ModelMapper.ToEntity(createModelDto);
            model.TenantId = currentTenantId.Value;
            model.CreatedBy = currentUser;
            model.CreatedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;
            model.ModifiedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.Models.Add(model);
            await _context.SaveChangesAsync(cancellationToken);

            // Load brand for DTO mapping
            await _context.Entry(model).Reference(m => m.Brand).LoadAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Created model {ModelId} with name {ModelName} by user {User}.",
                model.Id, model.Name, currentUser);

            return ModelMapper.ToDto(model);
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
            var model = await _context.Models
                .Where(m => m.Id == id && !m.IsDeleted)
                .Include(m => m.Brand)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
            {
                return null;
            }

            var originalModel = new Model
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ManufacturerPartNumber = model.ManufacturerPartNumber
            };

            ModelMapper.UpdateEntity(model, updateModelDto);
            model.ModifiedBy = currentUser;
            model.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Update", currentUser, originalModel, cancellationToken);

            _logger.LogInformation("Updated model {ModelId} by user {User}.", id, currentUser);

            return ModelMapper.ToDto(model);
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
            var model = await _context.Models
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
            {
                return false;
            }

            var originalModel = new Model
            {
                Id = model.Id,
                Name = model.Name,
                IsDeleted = model.IsDeleted
            };

            model.IsDeleted = true;
            model.DeletedBy = currentUser;
            model.DeletedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;
            model.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(model, "Delete", currentUser, originalModel, cancellationToken);

            _logger.LogInformation("Deleted model {ModelId} by user {User}.", id, currentUser);

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
                .AnyAsync(m => m.Id == modelId && !m.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if model {ModelId} exists.", modelId);
            throw;
        }
    }
}
