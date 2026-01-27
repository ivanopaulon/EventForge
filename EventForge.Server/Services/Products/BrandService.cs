using EventForge.DTOs.Products;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing product brands.
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

            var query = _context.Brands
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var brandDtos = await query
                .OrderBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var brand = await _context.Brands
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return brand != null ? MapToBrandDto(brand) : null;
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
            ArgumentNullException.ThrowIfNull(createBrandDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
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

            _ = _context.Brands.Add(brand);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(brand, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Brand {BrandId} created by {User}.", brand.Id, currentUser);

            return MapToBrandDto(brand);
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
            ArgumentNullException.ThrowIfNull(updateBrandDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var originalBrand = await _context.Brands
                .AsNoTracking()
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBrand == null) return null;

            var brand = await _context.Brands
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand == null) return null;

            brand.Name = updateBrandDto.Name;
            brand.Description = updateBrandDto.Description;
            brand.Website = updateBrandDto.Website;
            brand.Country = updateBrandDto.Country;
            brand.ModifiedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(brand, "Update", currentUser, originalBrand, cancellationToken);

            _logger.LogInformation("Brand {BrandId} updated by {User}.", brand.Id, currentUser);

            return MapToBrandDto(brand);
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
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            var originalBrand = await _context.Brands
                .AsNoTracking()
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBrand == null) return false;

            var brand = await _context.Brands
                .Where(b => b.Id == id && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (brand == null) return false;

            brand.IsDeleted = true;
            brand.ModifiedAt = DateTime.UtcNow;
            brand.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(brand, "Delete", currentUser, originalBrand, cancellationToken);

            _logger.LogInformation("Brand {BrandId} deleted by {User}.", brand.Id, currentUser);

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
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for brand operations.");
            }

            return await _context.Brands
                .Where(b => b.Id == brandId && b.TenantId == currentTenantId.Value && !b.IsDeleted)
                .AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if brand {BrandId} exists.", brandId);
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
}
