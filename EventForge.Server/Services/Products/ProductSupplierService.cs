using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing product-supplier relationships with business rule enforcement.
/// </summary>
public class ProductSupplierService : IProductSupplierService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProductSupplierService> _logger;

    public ProductSupplierService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ProductSupplierService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ProductSupplierDto>> GetProductSuppliersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product-supplier operations.");
            }

            var skip = (page - 1) * pageSize;

            var totalCount = await _context.ProductSuppliers
                .WhereActiveTenant(currentTenantId.Value)
                .LongCountAsync(cancellationToken);

            var entities = await _context.ProductSuppliers
                .WhereActiveTenant(currentTenantId.Value)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .OrderBy(ps => ps.Product!.Name)
                .ThenBy(ps => ps.Supplier!.Name)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = ProductSupplierMapper.ToDtoList(entities);

            return new PagedResult<ProductSupplierDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product-supplier relationships.");
            throw;
        }
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetSuppliersByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSuppliers = await _context.ProductSuppliers
                .Where(ps => ps.ProductId == productId && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .OrderByDescending(ps => ps.Preferred)
                .ThenBy(ps => ps.Supplier!.Name)
                .ToListAsync(cancellationToken);

            return ProductSupplierMapper.ToDtoCollection(productSuppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suppliers for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetProductsBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSuppliers = await _context.ProductSuppliers
                .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .OrderBy(ps => ps.Product!.Name)
                .ToListAsync(cancellationToken);

            return ProductSupplierMapper.ToDtoCollection(productSuppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for supplier {SupplierId}.", supplierId);
            throw;
        }
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(cancellationToken);

            return productSupplier != null ? ProductSupplierMapper.ToDto(productSupplier) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product-supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    public async Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product-supplier operations.");
            }

            // Business Rule 1: Validate that product exists
            var product = await _context.Products
                .Where(p => p.Id == createProductSupplierDto.ProductId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID {createProductSupplierDto.ProductId} does not exist.");
            }

            // Business Rule 2: Bundle products cannot have suppliers
            if (product.IsBundle)
            {
                throw new InvalidOperationException("Bundle products cannot have suppliers. A product marked as a bundle cannot be associated with suppliers.");
            }

            // Business Rule 3: Validate supplier exists and has correct PartyType
            var supplier = await _context.BusinessParties
                .Where(bp => bp.Id == createProductSupplierDto.SupplierId && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
            {
                throw new InvalidOperationException($"Supplier with ID {createProductSupplierDto.SupplierId} does not exist.");
            }

            if (supplier.PartyType != Data.Entities.Business.BusinessPartyType.Fornitore && 
                supplier.PartyType != Data.Entities.Business.BusinessPartyType.ClienteFornitore)
            {
                throw new InvalidOperationException($"The business party '{supplier.Name}' must be of type 'Fornitore' or 'ClienteFornitore' to be used as a supplier. Current type: {supplier.PartyType}");
            }

            // Business Rule 4: Preferred supplier uniqueness
            if (createProductSupplierDto.Preferred)
            {
                // Auto-reset any existing preferred supplier for this product
                var existingPreferred = await _context.ProductSuppliers
                    .Where(ps => ps.ProductId == createProductSupplierDto.ProductId && ps.Preferred && !ps.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var ps in existingPreferred)
                {
                    ps.Preferred = false;
                    ps.ModifiedBy = currentUser;
                    ps.ModifiedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Reset {Count} existing preferred suppliers for product {ProductId} when creating new preferred supplier.",
                    existingPreferred.Count, createProductSupplierDto.ProductId);
            }

            var productSupplier = ProductSupplierMapper.ToEntity(createProductSupplierDto);
            productSupplier.TenantId = currentTenantId.Value;
            productSupplier.CreatedBy = currentUser;
            productSupplier.CreatedAt = DateTime.UtcNow;
            productSupplier.ModifiedBy = currentUser;
            productSupplier.ModifiedAt = DateTime.UtcNow;
            productSupplier.IsActive = true;

            _context.ProductSuppliers.Add(productSupplier);
            await _context.SaveChangesAsync(cancellationToken);

            // Load navigation properties for DTO mapping
            productSupplier.Product = product;
            productSupplier.Supplier = supplier;

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Created product-supplier relationship {ProductSupplierId} for product {ProductId} and supplier {SupplierId} by user {User}.",
                productSupplier.Id, createProductSupplierDto.ProductId, createProductSupplierDto.SupplierId, currentUser);

            return ProductSupplierMapper.ToDto(productSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product-supplier relationship.");
            throw;
        }
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(cancellationToken);

            if (productSupplier == null)
            {
                return null;
            }

            var originalProductSupplier = new ProductSupplier
            {
                Id = productSupplier.Id,
                ProductId = productSupplier.ProductId,
                SupplierId = productSupplier.SupplierId,
                Preferred = productSupplier.Preferred,
                UnitCost = productSupplier.UnitCost,
                Currency = productSupplier.Currency
            };

            // Business Rule: Preferred supplier uniqueness
            if (updateProductSupplierDto.Preferred && !productSupplier.Preferred)
            {
                // Auto-reset any existing preferred supplier for this product
                var existingPreferred = await _context.ProductSuppliers
                    .Where(ps => ps.ProductId == productSupplier.ProductId && ps.Preferred && ps.Id != id && !ps.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var ps in existingPreferred)
                {
                    ps.Preferred = false;
                    ps.ModifiedBy = currentUser;
                    ps.ModifiedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Reset {Count} existing preferred suppliers for product {ProductId} when updating supplier {ProductSupplierId} to preferred.",
                    existingPreferred.Count, productSupplier.ProductId, id);
            }

            ProductSupplierMapper.UpdateEntity(productSupplier, updateProductSupplierDto);
            productSupplier.ModifiedBy = currentUser;
            productSupplier.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Update", currentUser, originalProductSupplier, cancellationToken);

            _logger.LogInformation("Updated product-supplier {ProductSupplierId} by user {User}.", id, currentUser);

            return ProductSupplierMapper.ToDto(productSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product-supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(cancellationToken);

            if (productSupplier == null)
            {
                return false;
            }

            var originalProductSupplier = new ProductSupplier
            {
                Id = productSupplier.Id,
                ProductId = productSupplier.ProductId,
                SupplierId = productSupplier.SupplierId,
                IsDeleted = productSupplier.IsDeleted
            };

            productSupplier.IsDeleted = true;
            productSupplier.DeletedBy = currentUser;
            productSupplier.DeletedAt = DateTime.UtcNow;
            productSupplier.ModifiedBy = currentUser;
            productSupplier.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Delete", currentUser, originalProductSupplier, cancellationToken);

            _logger.LogInformation("Deleted product-supplier {ProductSupplierId} by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product-supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    public async Task<bool> ProductSupplierExistsAsync(Guid productSupplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ProductSuppliers
                .AnyAsync(ps => ps.Id == productSupplierId && !ps.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product-supplier {ProductSupplierId} exists.", productSupplierId);
            throw;
        }
    }
}
