using EventForge.DTOs.Products;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing product brands.
/// </summary>
public class BrandService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<BrandService> logger) : IBrandService
{

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var query = context.Brands
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var brandDtos = await query
                .OrderBy(b => b.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Website = b.Website,
                    Country = b.Country,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<BrandDto>
            {
                Items = brandDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving brands.");
            throw;
        }
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var brand = await context.Brands
                .AsNoTracking()
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return brand is not null ? MapToBrandDto(brand) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createBrandDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createBrandDto.Name,
                Description = createBrandDto.Description,
                Website = createBrandDto.Website,
                Country = createBrandDto.Country,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.Brands.Add(brand);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(brand, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Brand {BrandId} created by {User}.", brand.Id, currentUser);

            return MapToBrandDto(brand);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating brand.");
            throw;
        }
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateBrandDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var originalBrand = await context.Brands
                .AsNoTracking()
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBrand is null) return null;

            var brand = await context.Brands
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand is null) return null;

            brand.Name = updateBrandDto.Name;
            brand.Description = updateBrandDto.Description;
            brand.Website = updateBrandDto.Website;
            brand.Country = updateBrandDto.Country;
            brand.ModifiedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Brand {BrandId}.", id);
                throw new InvalidOperationException("Il brand è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(brand, "Update", currentUser, originalBrand, cancellationToken);

            logger.LogInformation("Brand {BrandId} updated by {User}.", brand.Id, currentUser);

            return MapToBrandDto(brand);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteBrandAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var originalBrand = await context.Brands
                .AsNoTracking()
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBrand is null) return false;

            var brand = await context.Brands
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand is null) return false;

            brand.IsDeleted = true;
            brand.ModifiedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Brand {BrandId}.", id);
                throw new InvalidOperationException("Il brand è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(brand, "Delete", currentUser, originalBrand, cancellationToken);

            logger.LogInformation("Brand {BrandId} deleted by {User}.", brand.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<bool> BrandExistsAsync(Guid brandId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            return await context.Brands
                .AsNoTracking()
                .Where(b => b.Id == brandId && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if brand {BrandId} exists.", brandId);
            throw;
        }
    }

    private static BrandDto MapToBrandDto(Brand brand)
    {
        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            Website = brand.Website,
            Country = brand.Country,
            CreatedAt = brand.CreatedAt,
            CreatedBy = brand.CreatedBy
        };
    }

    public async Task<PagedResult<BrandDto>> GetActiveBrandsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var query = context.Brands
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Where(b => b.IsActive);

            var totalCount = await query.CountAsync(cancellationToken);
            var brandDtos = await query
                .OrderBy(b => b.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Website = b.Website,
                    Country = b.Country,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<BrandDto>
            {
                Items = brandDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active brands.");
            throw;
        }
    }

}
