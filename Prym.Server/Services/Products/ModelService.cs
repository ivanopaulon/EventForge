using Prym.DTOs.Products;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Products;

/// <summary>
/// Service implementation for managing product models.
/// </summary>
public class ModelService(
    PrymDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<ModelService> logger) : IModelService
{

    public async Task<PagedResult<ModelDto>> GetModelsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var query = context.Models
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Include(m => m.Brand);

            var totalCount = await query.CountAsync(cancellationToken);
            var modelDtos = await query
                .OrderBy(m => m.Brand!.Name)
                .ThenBy(m => m.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(m => new ModelDto
                {
                    Id = m.Id,
                    BrandId = m.BrandId,
                    BrandName = m.Brand != null ? m.Brand.Name : null,
                    Name = m.Name,
                    Description = m.Description,
                    ManufacturerPartNumber = m.ManufacturerPartNumber,
                    CreatedAt = m.CreatedAt,
                    CreatedBy = m.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ModelDto>
            {
                Items = modelDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving models.");
            throw;
        }
    }

    public async Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var query = context.Models
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Where(m => m.BrandId == brandId)
                .Include(m => m.Brand);

            var totalCount = await query.CountAsync(cancellationToken);
            var modelDtos = await query
                .OrderBy(m => m.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(m => new ModelDto
                {
                    Id = m.Id,
                    BrandId = m.BrandId,
                    BrandName = m.Brand != null ? m.Brand.Name : null,
                    Name = m.Name,
                    Description = m.Description,
                    ManufacturerPartNumber = m.ManufacturerPartNumber,
                    CreatedAt = m.CreatedAt,
                    CreatedBy = m.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ModelDto>
            {
                Items = modelDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving models for brand {BrandId}.", brandId);
            throw;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var model = await context.Models
                .Where(m => m.Id == id && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .Include(m => m.Brand)
                .FirstOrDefaultAsync(cancellationToken);

            return model is not null ? MapToModelDto(model) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving model {ModelId}.", id);
            throw;
        }
    }

    public async Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createModelDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            // Verify brand exists and belongs to the same tenant
            var brandExists = await context.Brands
                .Where(b => b.Id == createModelDto.BrandId && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!brandExists)
            {
                throw new ArgumentException($"Brand with ID {createModelDto.BrandId} not found.");
            }

            var model = new Model
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                BrandId = createModelDto.BrandId,
                Name = createModelDto.Name,
                Description = createModelDto.Description,
                ManufacturerPartNumber = createModelDto.ManufacturerPartNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.Models.Add(model);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(model, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Model {ModelId} created by {User}.", model.Id, currentUser);

            // Reload with Brand to get brand name
            var createdModel = await context.Models
                .Include(m => m.Brand)
                .FirstAsync(m => m.Id == model.Id, cancellationToken);

            return MapToModelDto(createdModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating model.");
            throw;
        }
    }

    public async Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateModelDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var originalModel = await context.Models
                .AsNoTracking()
                .Where(m => m.Id == id && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalModel is null) return null;

            // Verify brand exists and belongs to the same tenant
            var brandExists = await context.Brands
                .Where(b => b.Id == updateModelDto.BrandId && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!brandExists)
            {
                throw new ArgumentException($"Brand with ID {updateModelDto.BrandId} not found.");
            }

            var model = await context.Models
                .Where(m => m.Id == id && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (model is null) return null;

            model.BrandId = updateModelDto.BrandId;
            model.Name = updateModelDto.Name;
            model.Description = updateModelDto.Description;
            model.ManufacturerPartNumber = updateModelDto.ManufacturerPartNumber;
            model.ModifiedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Model {ModelId}.", id);
                throw new InvalidOperationException("Il modello è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(model, "Update", currentUser, originalModel, cancellationToken);

            logger.LogInformation("Model {ModelId} updated by {User}.", model.Id, currentUser);

            // Reload with Brand to get brand name
            var updatedModel = await context.Models
                .Include(m => m.Brand)
                .FirstAsync(m => m.Id == model.Id, cancellationToken);

            return MapToModelDto(updatedModel);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating model {ModelId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            var originalModel = await context.Models
                .AsNoTracking()
                .Where(m => m.Id == id && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalModel is null) return false;

            var model = await context.Models
                .Where(m => m.Id == id && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (model is null) return false;

            model.IsDeleted = true;
            model.ModifiedAt = DateTime.UtcNow;
            model.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Model {ModelId}.", id);
                throw new InvalidOperationException("Il modello è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(model, "Delete", currentUser, originalModel, cancellationToken);

            logger.LogInformation("Model {ModelId} deleted by {User}.", model.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting model {ModelId}.", id);
            throw;
        }
    }

    public async Task<bool> ModelExistsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for model operations.");
            }

            return await context.Models
                .Where(m => m.Id == modelId && m.TenantId == currentTenantId.Value && !m.IsDeleted)
                .AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if model {ModelId} exists.", modelId);
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
