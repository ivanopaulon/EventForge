using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using EntityBusinessPartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing product-supplier relationships.
/// Enforces business rules documented in Issue #353.
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
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            var query = _context.ProductSuppliers
                .WhereActiveTenant(currentTenantId.Value)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier);

            var totalCount = await query.CountAsync(cancellationToken);
            var productSuppliers = await query
                .OrderBy(ps => ps.Product!.Name)
                .ThenBy(ps => ps.Supplier!.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var productSupplierDtos = productSuppliers.Select(MapToProductSupplierDto);

            return new PagedResult<ProductSupplierDto>
            {
                Items = productSupplierDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product suppliers.");
            throw;
        }
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetProductSuppliersByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
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

            return productSuppliers.Select(MapToProductSupplierDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product suppliers for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<PagedResult<ProductSupplierDto>> GetProductSuppliersBySupplierIdAsync(Guid supplierId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.ProductSuppliers
                .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier);

            var totalCount = await query.CountAsync(cancellationToken);
            var productSuppliers = await query
                .OrderBy(ps => ps.Product!.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var productSupplierDtos = productSuppliers.Select(MapToProductSupplierDto);

            return new PagedResult<ProductSupplierDto>
            {
                Items = productSupplierDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product suppliers for supplier {SupplierId}.", supplierId);
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

            return productSupplier != null ? MapToProductSupplierDto(productSupplier) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    public async Task<ProductSupplierDto?> GetPreferredSupplierAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.ProductId == productId && ps.Preferred && !ps.IsDeleted)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(cancellationToken);

            return productSupplier != null ? MapToProductSupplierDto(productSupplier) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving preferred supplier for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductSupplierDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Validate product exists and get IsBundle status
            var product = await _context.Products
                .Where(p => p.Id == createProductSupplierDto.ProductId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                throw new ArgumentException($"Product with ID {createProductSupplierDto.ProductId} not found.");
            }

            // Business Rule 2: Bundle products cannot have suppliers
            if (product.IsBundle)
            {
                throw new InvalidOperationException("Bundle products cannot have suppliers.");
            }

            // Validate supplier exists and get PartyType
            var supplier = await _context.BusinessParties
                .Where(bp => bp.Id == createProductSupplierDto.SupplierId && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
            {
                throw new ArgumentException($"Supplier with ID {createProductSupplierDto.SupplierId} not found.");
            }

            // Business Rule 3: Supplier must be Fornitore or ClienteFornitore
            if (supplier.PartyType != EntityBusinessPartyType.Fornitore && supplier.PartyType != EntityBusinessPartyType.ClienteFornitore)
            {
                throw new InvalidOperationException("Supplier must be of type 'Fornitore' or 'ClienteFornitore'.");
            }

            // Business Rule 1: Only one preferred supplier per product
            if (createProductSupplierDto.Preferred)
            {
                await ResetPreferredSupplierAsync(createProductSupplierDto.ProductId, currentUser, cancellationToken);
            }

            var productSupplier = new ProductSupplier
            {
                Id = Guid.NewGuid(),
                ProductId = createProductSupplierDto.ProductId,
                SupplierId = createProductSupplierDto.SupplierId,
                SupplierProductCode = createProductSupplierDto.SupplierProductCode,
                PurchaseDescription = createProductSupplierDto.PurchaseDescription,
                UnitCost = createProductSupplierDto.UnitCost,
                Currency = createProductSupplierDto.Currency,
                MinOrderQty = createProductSupplierDto.MinOrderQty,
                IncrementQty = createProductSupplierDto.IncrementQty,
                LeadTimeDays = createProductSupplierDto.LeadTimeDays,
                LastPurchasePrice = createProductSupplierDto.LastPurchasePrice,
                LastPurchaseDate = createProductSupplierDto.LastPurchaseDate,
                Preferred = createProductSupplierDto.Preferred,
                Notes = createProductSupplierDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.ProductSuppliers.Add(productSupplier);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Product supplier {ProductSupplierId} created by {User}.", productSupplier.Id, currentUser);

            // Reload with related entities
            var createdProductSupplier = await _context.ProductSuppliers
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstAsync(ps => ps.Id == productSupplier.Id, cancellationToken);

            return MapToProductSupplierDto(createdProductSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product supplier.");
            throw;
        }
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductSupplierDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalProductSupplier = await _context.ProductSuppliers
                .AsNoTracking()
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalProductSupplier == null) return null;

            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productSupplier == null) return null;

            // Validate product exists and get IsBundle status
            var product = await _context.Products
                .Where(p => p.Id == updateProductSupplierDto.ProductId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                throw new ArgumentException($"Product with ID {updateProductSupplierDto.ProductId} not found.");
            }

            // Business Rule 2: Bundle products cannot have suppliers
            if (product.IsBundle)
            {
                throw new InvalidOperationException("Bundle products cannot have suppliers.");
            }

            // Validate supplier exists and get PartyType
            var supplier = await _context.BusinessParties
                .Where(bp => bp.Id == updateProductSupplierDto.SupplierId && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
            {
                throw new ArgumentException($"Supplier with ID {updateProductSupplierDto.SupplierId} not found.");
            }

            // Business Rule 3: Supplier must be Fornitore or ClienteFornitore
            if (supplier.PartyType != EntityBusinessPartyType.Fornitore && supplier.PartyType != EntityBusinessPartyType.ClienteFornitore)
            {
                throw new InvalidOperationException("Supplier must be of type 'Fornitore' or 'ClienteFornitore'.");
            }

            // Business Rule 1: Only one preferred supplier per product
            if (updateProductSupplierDto.Preferred && !originalProductSupplier.Preferred)
            {
                await ResetPreferredSupplierAsync(updateProductSupplierDto.ProductId, currentUser, cancellationToken, id);
            }

            productSupplier.ProductId = updateProductSupplierDto.ProductId;
            productSupplier.SupplierId = updateProductSupplierDto.SupplierId;
            productSupplier.SupplierProductCode = updateProductSupplierDto.SupplierProductCode;
            productSupplier.PurchaseDescription = updateProductSupplierDto.PurchaseDescription;
            productSupplier.UnitCost = updateProductSupplierDto.UnitCost;
            productSupplier.Currency = updateProductSupplierDto.Currency;
            productSupplier.MinOrderQty = updateProductSupplierDto.MinOrderQty;
            productSupplier.IncrementQty = updateProductSupplierDto.IncrementQty;
            productSupplier.LeadTimeDays = updateProductSupplierDto.LeadTimeDays;
            productSupplier.LastPurchasePrice = updateProductSupplierDto.LastPurchasePrice;
            productSupplier.LastPurchaseDate = updateProductSupplierDto.LastPurchaseDate;
            productSupplier.Preferred = updateProductSupplierDto.Preferred;
            productSupplier.Notes = updateProductSupplierDto.Notes;
            productSupplier.ModifiedAt = DateTime.UtcNow;
            productSupplier.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Update", currentUser, originalProductSupplier, cancellationToken);

            _logger.LogInformation("Product supplier {ProductSupplierId} updated by {User}.", productSupplier.Id, currentUser);

            // Reload with related entities
            var updatedProductSupplier = await _context.ProductSuppliers
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstAsync(ps => ps.Id == productSupplier.Id, cancellationToken);

            return MapToProductSupplierDto(updatedProductSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalProductSupplier = await _context.ProductSuppliers
                .AsNoTracking()
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalProductSupplier == null) return false;

            var productSupplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productSupplier == null) return false;

            productSupplier.IsDeleted = true;
            productSupplier.ModifiedAt = DateTime.UtcNow;
            productSupplier.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(productSupplier, "Delete", currentUser, originalProductSupplier, cancellationToken);

            _logger.LogInformation("Product supplier {ProductSupplierId} deleted by {User}.", productSupplier.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product supplier {ProductSupplierId}.", id);
            throw;
        }
    }

    /// <summary>
    /// Resets the preferred flag on all suppliers for a product except the one being excluded.
    /// Business Rule 1: Only one preferred supplier per product.
    /// </summary>
    private async Task ResetPreferredSupplierAsync(Guid productId, string currentUser, CancellationToken cancellationToken, Guid? excludeId = null)
    {
        var existingPreferred = await _context.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.Preferred && !ps.IsDeleted)
            .Where(ps => !excludeId.HasValue || ps.Id != excludeId.Value)
            .ToListAsync(cancellationToken);

        foreach (var ps in existingPreferred)
        {
            ps.Preferred = false;
            ps.ModifiedAt = DateTime.UtcNow;
            ps.ModifiedBy = currentUser;
        }

        if (existingPreferred.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reset preferred flag for {Count} suppliers of product {ProductId}.", existingPreferred.Count, productId);
        }
    }

    private static ProductSupplierDto MapToProductSupplierDto(ProductSupplier productSupplier)
    {
        return new ProductSupplierDto
        {
            Id = productSupplier.Id,
            ProductId = productSupplier.ProductId,
            ProductName = productSupplier.Product?.Name,
            SupplierId = productSupplier.SupplierId,
            SupplierName = productSupplier.Supplier?.Name,
            SupplierProductCode = productSupplier.SupplierProductCode,
            PurchaseDescription = productSupplier.PurchaseDescription,
            UnitCost = productSupplier.UnitCost,
            Currency = productSupplier.Currency,
            MinOrderQty = productSupplier.MinOrderQty,
            IncrementQty = productSupplier.IncrementQty,
            LeadTimeDays = productSupplier.LeadTimeDays,
            LastPurchasePrice = productSupplier.LastPurchasePrice,
            LastPurchaseDate = productSupplier.LastPurchaseDate,
            Preferred = productSupplier.Preferred,
            Notes = productSupplier.Notes,
            CreatedAt = productSupplier.CreatedAt,
            CreatedBy = productSupplier.CreatedBy
        };
    }
}
