using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing brands.
/// </summary>
public class BrandService : IBrandService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BrandService> _logger;

    public BrandService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<BrandService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var skip = (page - 1) * pageSize;

            var totalCount = await _context.Brands
                .WhereActiveTenant(currentTenantId.Value)
                .LongCountAsync(cancellationToken);

            var entities = await _context.Brands
                .WhereActiveTenant(currentTenantId.Value)
                .OrderBy(b => b.Name)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = BrandMapper.ToDtoList(entities);

            return new PagedResult<BrandDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands.");
            throw;
        }
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var brand = await _context.Brands
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return brand != null ? BrandMapper.ToDto(brand) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var brand = BrandMapper.ToEntity(createBrandDto);
            brand.TenantId = currentTenantId.Value;
            brand.CreatedBy = currentUser;
            brand.CreatedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;
            brand.ModifiedAt = DateTime.UtcNow;
            brand.IsActive = true;

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(brand, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Created brand {BrandId} with name {BrandName} by user {User}.",
                brand.Id, brand.Name, currentUser);

            return BrandMapper.ToDto(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand.");
            throw;
        }
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var brand = await _context.Brands
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand == null)
            {
                return null;
            }

            var originalBrand = new Brand
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description,
                Website = brand.Website,
                Country = brand.Country
            };

            BrandMapper.UpdateEntity(brand, updateBrandDto);
            brand.ModifiedBy = currentUser;
            brand.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(brand, "Update", currentUser, originalBrand, cancellationToken);

            _logger.LogInformation("Updated brand {BrandId} by user {User}.", id, currentUser);

            return BrandMapper.ToDto(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteBrandAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var brand = await _context.Brands
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand == null)
            {
                return false;
            }

            var originalBrand = new Brand
            {
                Id = brand.Id,
                Name = brand.Name,
                IsDeleted = brand.IsDeleted
            };

            brand.IsDeleted = true;
            brand.DeletedBy = currentUser;
            brand.DeletedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;
            brand.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(brand, "Delete", currentUser, originalBrand, cancellationToken);

            _logger.LogInformation("Deleted brand {BrandId} by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand {BrandId}.", id);
            throw;
        }
    }

    public async Task<bool> BrandExistsAsync(Guid brandId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Brands
                .AnyAsync(b => b.Id == brandId && !b.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if brand {BrandId} exists.", brandId);
            throw;
        }
    }
}
