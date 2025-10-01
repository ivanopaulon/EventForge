using EventForge.DTOs.Products;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing products and related entities.
/// </summary>
public class ProductService : IProductService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ProductService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Product CRUD operations

    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            var query = _context.Products
                .WhereActiveTenant(currentTenantId.Value)
                .Include(p => p.Codes.Where(c => !c.IsDeleted && c.TenantId == currentTenantId.Value))
                .Include(p => p.Units.Where(u => !u.IsDeleted && u.TenantId == currentTenantId.Value))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted && bi.TenantId == currentTenantId.Value))
                .Include(p => p.ImageDocument);

            var totalCount = await query.CountAsync(cancellationToken);
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var productDtos = products.Select(MapToProductDto);

            return new PagedResult<ProductDto>
            {
                Items = productDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products.");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(cancellationToken);

            return product != null ? MapToProductDto(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}.", id);
            throw;
        }
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(cancellationToken);

            return product != null ? MapToProductDetailDto(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product detail {ProductId}.", id);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var product = new Product
            {
                TenantId = currentTenantId.Value,
                Name = createProductDto.Name,
                ShortDescription = createProductDto.ShortDescription,
                Description = createProductDto.Description,
                Code = createProductDto.Code,
#pragma warning disable CS0618 // Type or member is obsolete
                ImageUrl = createProductDto.ImageUrl,
#pragma warning restore CS0618 // Type or member is obsolete
                ImageDocumentId = createProductDto.ImageDocumentId,
                Status = (EventForge.Server.Data.Entities.Products.ProductStatus)createProductDto.Status,
                IsVatIncluded = createProductDto.IsVatIncluded,
                DefaultPrice = createProductDto.DefaultPrice,
                VatRateId = createProductDto.VatRateId,
                UnitOfMeasureId = createProductDto.UnitOfMeasureId,
                CategoryNodeId = createProductDto.CategoryNodeId,
                FamilyNodeId = createProductDto.FamilyNodeId,
                GroupNodeId = createProductDto.GroupNodeId,
                StationId = createProductDto.StationId,
                IsBundle = createProductDto.IsBundle,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product
            await _auditLogService.TrackEntityChangesAsync(product, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Product created with ID {ProductId} by user {User}.", product.Id, currentUser);

            return MapToProductDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for user {User}.", currentUser);
            throw;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var product = await _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Store original for audit
            var originalProduct = new Product
            {
                Id = product.Id,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Code = product.Code,
#pragma warning disable CS0618 // Type or member is obsolete
                ImageUrl = product.ImageUrl,
#pragma warning restore CS0618 // Type or member is obsolete
                ImageDocumentId = product.ImageDocumentId,
                IsVatIncluded = product.IsVatIncluded,
                DefaultPrice = product.DefaultPrice,
                VatRateId = product.VatRateId,
                UnitOfMeasureId = product.UnitOfMeasureId,
                CategoryNodeId = product.CategoryNodeId,
                FamilyNodeId = product.FamilyNodeId,
                GroupNodeId = product.GroupNodeId,
                StationId = product.StationId,
                IsBundle = product.IsBundle,
                CreatedBy = product.CreatedBy,
                CreatedAt = product.CreatedAt,
                ModifiedBy = product.ModifiedBy,
                ModifiedAt = product.ModifiedAt
            };

            // Update properties
            product.Name = updateProductDto.Name;
            product.ShortDescription = updateProductDto.ShortDescription;
            product.Description = updateProductDto.Description;
            // Note: Code and IsBundle are intentionally not updatable after creation
#pragma warning disable CS0618 // Type or member is obsolete
            product.ImageUrl = updateProductDto.ImageUrl;
#pragma warning restore CS0618 // Type or member is obsolete
            product.ImageDocumentId = updateProductDto.ImageDocumentId;
            product.Status = (EventForge.Server.Data.Entities.Products.ProductStatus)updateProductDto.Status;
            product.IsVatIncluded = updateProductDto.IsVatIncluded;
            product.DefaultPrice = updateProductDto.DefaultPrice;
            product.VatRateId = updateProductDto.VatRateId;
            product.UnitOfMeasureId = updateProductDto.UnitOfMeasureId;
            product.CategoryNodeId = updateProductDto.CategoryNodeId;
            product.FamilyNodeId = updateProductDto.FamilyNodeId;
            product.GroupNodeId = updateProductDto.GroupNodeId;
            product.StationId = updateProductDto.StationId;
            product.ModifiedBy = currentUser;
            product.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product
            await _auditLogService.TrackEntityChangesAsync(product, "Update", currentUser, originalProduct, cancellationToken);

            _logger.LogInformation("Product {ProductId} updated by user {User}.", id, currentUser);

            return MapToProductDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var product = await _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalProduct = new Product
            {
                Id = product.Id,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Code = product.Code,
                ImageUrl = product.ImageUrl,
                IsVatIncluded = product.IsVatIncluded,
                DefaultPrice = product.DefaultPrice,
                VatRateId = product.VatRateId,
                UnitOfMeasureId = product.UnitOfMeasureId,
                CategoryNodeId = product.CategoryNodeId,
                FamilyNodeId = product.FamilyNodeId,
                GroupNodeId = product.GroupNodeId,
                StationId = product.StationId,
                IsBundle = product.IsBundle,
                CreatedBy = product.CreatedBy,
                CreatedAt = product.CreatedAt,
                ModifiedBy = product.ModifiedBy,
                ModifiedAt = product.ModifiedAt,
                IsDeleted = product.IsDeleted,
                DeletedBy = product.DeletedBy,
                DeletedAt = product.DeletedAt
            };

            // Soft delete the product and all related entities
            product.IsDeleted = true;
            product.DeletedBy = currentUser;
            product.DeletedAt = DateTime.UtcNow;

            // Soft delete related codes
            foreach (var code in product.Codes.Where(c => !c.IsDeleted))
            {
                code.IsDeleted = true;
                code.DeletedBy = currentUser;
                code.DeletedAt = DateTime.UtcNow;
            }

            // Soft delete related units
            foreach (var unit in product.Units.Where(u => !u.IsDeleted))
            {
                unit.IsDeleted = true;
                unit.DeletedBy = currentUser;
                unit.DeletedAt = DateTime.UtcNow;
            }

            // Soft delete related bundle items
            foreach (var bundleItem in product.BundleItems.Where(bi => !bi.IsDeleted))
            {
                bundleItem.IsDeleted = true;
                bundleItem.DeletedBy = currentUser;
                bundleItem.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product
            await _auditLogService.TrackEntityChangesAsync(product, "Delete", currentUser, originalProduct, cancellationToken);

            _logger.LogInformation("Product {ProductId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId} for user {User}.", id, currentUser);
            throw;
        }
    }

    // Product Code management operations

    public async Task<IEnumerable<ProductCodeDto>> GetProductCodesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var codes = await _context.ProductCodes
                .Where(pc => pc.ProductId == productId && !pc.IsDeleted)
                .OrderBy(pc => pc.CodeType)
                .ThenBy(pc => pc.Code)
                .ToListAsync(cancellationToken);

            return codes.Select(MapToProductCodeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product codes for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var code = await _context.ProductCodes
                .Where(pc => pc.Id == id && !pc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return code != null ? MapToProductCodeDto(code) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product code {ProductCodeId}.", id);
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string codeValue, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codeValue);

            var productCode = await _context.ProductCodes
                .Where(pc => pc.Code == codeValue && !pc.IsDeleted)
                .Include(pc => pc.Product)
                .FirstOrDefaultAsync(cancellationToken);

            if (productCode?.Product == null || productCode.Product.IsDeleted)
            {
                return null;
            }

            return MapToProductDto(productCode.Product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by code {CodeValue}.", codeValue);
            throw;
        }
    }

    public async Task<ProductCodeDto> AddProductCodeAsync(CreateProductCodeDto createProductCodeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductCodeDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Check if product exists
            if (!await ProductExistsAsync(createProductCodeDto.ProductId, cancellationToken))
            {
                throw new ArgumentException($"Product with ID {createProductCodeDto.ProductId} does not exist.");
            }

            var productCode = new ProductCode
            {
                ProductId = createProductCodeDto.ProductId,
                CodeType = createProductCodeDto.CodeType,
                Code = createProductCodeDto.Code,
                AlternativeDescription = createProductCodeDto.AlternativeDescription,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductCodes.Add(productCode);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product code
            await _auditLogService.TrackEntityChangesAsync(productCode, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Product code created with ID {ProductCodeId} for product {ProductId} by user {User}.",
                productCode.Id, createProductCodeDto.ProductId, currentUser);

            return MapToProductCodeDto(productCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product code for product {ProductId} by user {User}.",
                createProductCodeDto.ProductId, currentUser);
            throw;
        }
    }

    public async Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateProductCodeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductCodeDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var productCode = await _context.ProductCodes
                .Where(pc => pc.Id == id && !pc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productCode == null)
            {
                _logger.LogWarning("Product code with ID {ProductCodeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Store original for audit
            var originalProductCode = new ProductCode
            {
                Id = productCode.Id,
                ProductId = productCode.ProductId,
                CodeType = productCode.CodeType,
                Code = productCode.Code,
                AlternativeDescription = productCode.AlternativeDescription,
                CreatedBy = productCode.CreatedBy,
                CreatedAt = productCode.CreatedAt,
                ModifiedBy = productCode.ModifiedBy,
                ModifiedAt = productCode.ModifiedAt
            };

            // Update properties
            productCode.CodeType = updateProductCodeDto.CodeType;
            productCode.Code = updateProductCodeDto.Code;
            productCode.AlternativeDescription = updateProductCodeDto.AlternativeDescription;
            productCode.ModifiedBy = currentUser;
            productCode.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product code
            await _auditLogService.TrackEntityChangesAsync(productCode, "Update", currentUser, originalProductCode, cancellationToken);

            _logger.LogInformation("Product code {ProductCodeId} updated by user {User}.", id, currentUser);

            return MapToProductCodeDto(productCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product code {ProductCodeId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> RemoveProductCodeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var productCode = await _context.ProductCodes
                .Where(pc => pc.Id == id && !pc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productCode == null)
            {
                _logger.LogWarning("Product code with ID {ProductCodeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalProductCode = new ProductCode
            {
                Id = productCode.Id,
                ProductId = productCode.ProductId,
                CodeType = productCode.CodeType,
                Code = productCode.Code,
                AlternativeDescription = productCode.AlternativeDescription,
                CreatedBy = productCode.CreatedBy,
                CreatedAt = productCode.CreatedAt,
                ModifiedBy = productCode.ModifiedBy,
                ModifiedAt = productCode.ModifiedAt,
                IsDeleted = productCode.IsDeleted,
                DeletedBy = productCode.DeletedBy,
                DeletedAt = productCode.DeletedAt
            };

            // Soft delete the product code
            productCode.IsDeleted = true;
            productCode.DeletedBy = currentUser;
            productCode.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product code
            await _auditLogService.TrackEntityChangesAsync(productCode, "Delete", currentUser, originalProductCode, cancellationToken);

            _logger.LogInformation("Product code {ProductCodeId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product code {ProductCodeId} for user {User}.", id, currentUser);
            throw;
        }
    }

    // Product Unit management operations

    public async Task<IEnumerable<ProductUnitDto>> GetProductUnitsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var units = await _context.ProductUnits
                .Where(pu => pu.ProductId == productId && !pu.IsDeleted)
                .OrderBy(pu => pu.UnitType)
                .ThenBy(pu => pu.ConversionFactor)
                .ToListAsync(cancellationToken);

            return units.Select(MapToProductUnitDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product units for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var unit = await _context.ProductUnits
                .Where(pu => pu.Id == id && !pu.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return unit != null ? MapToProductUnitDto(unit) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product unit {ProductUnitId}.", id);
            throw;
        }
    }

    public async Task<ProductUnitDto> AddProductUnitAsync(CreateProductUnitDto createProductUnitDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductUnitDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Check if product exists
            if (!await ProductExistsAsync(createProductUnitDto.ProductId, cancellationToken))
            {
                throw new ArgumentException($"Product with ID {createProductUnitDto.ProductId} does not exist.");
            }

            var productUnit = new ProductUnit
            {
                ProductId = createProductUnitDto.ProductId,
                UnitOfMeasureId = createProductUnitDto.UnitOfMeasureId,
                ConversionFactor = createProductUnitDto.ConversionFactor,
                UnitType = createProductUnitDto.UnitType,
                Description = createProductUnitDto.Description,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductUnits.Add(productUnit);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product unit
            await _auditLogService.TrackEntityChangesAsync(productUnit, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Product unit created with ID {ProductUnitId} for product {ProductId} by user {User}.",
                productUnit.Id, createProductUnitDto.ProductId, currentUser);

            return MapToProductUnitDto(productUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product unit for product {ProductId} by user {User}.",
                createProductUnitDto.ProductId, currentUser);
            throw;
        }
    }

    public async Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateProductUnitDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductUnitDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var productUnit = await _context.ProductUnits
                .Where(pu => pu.Id == id && !pu.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productUnit == null)
            {
                _logger.LogWarning("Product unit with ID {ProductUnitId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Store original for audit
            var originalProductUnit = new ProductUnit
            {
                Id = productUnit.Id,
                ProductId = productUnit.ProductId,
                UnitOfMeasureId = productUnit.UnitOfMeasureId,
                ConversionFactor = productUnit.ConversionFactor,
                UnitType = productUnit.UnitType,
                Description = productUnit.Description,
                CreatedBy = productUnit.CreatedBy,
                CreatedAt = productUnit.CreatedAt,
                ModifiedBy = productUnit.ModifiedBy,
                ModifiedAt = productUnit.ModifiedAt
            };

            // Update properties
            productUnit.UnitOfMeasureId = updateProductUnitDto.UnitOfMeasureId;
            productUnit.ConversionFactor = updateProductUnitDto.ConversionFactor;
            productUnit.UnitType = updateProductUnitDto.UnitType;
            productUnit.Description = updateProductUnitDto.Description;
            productUnit.ModifiedBy = currentUser;
            productUnit.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product unit
            await _auditLogService.TrackEntityChangesAsync(productUnit, "Update", currentUser, originalProductUnit, cancellationToken);

            _logger.LogInformation("Product unit {ProductUnitId} updated by user {User}.", id, currentUser);

            return MapToProductUnitDto(productUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product unit {ProductUnitId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> RemoveProductUnitAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var productUnit = await _context.ProductUnits
                .Where(pu => pu.Id == id && !pu.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productUnit == null)
            {
                _logger.LogWarning("Product unit with ID {ProductUnitId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalProductUnit = new ProductUnit
            {
                Id = productUnit.Id,
                ProductId = productUnit.ProductId,
                UnitOfMeasureId = productUnit.UnitOfMeasureId,
                ConversionFactor = productUnit.ConversionFactor,
                UnitType = productUnit.UnitType,
                Description = productUnit.Description,
                CreatedBy = productUnit.CreatedBy,
                CreatedAt = productUnit.CreatedAt,
                ModifiedBy = productUnit.ModifiedBy,
                ModifiedAt = productUnit.ModifiedAt,
                IsDeleted = productUnit.IsDeleted,
                DeletedBy = productUnit.DeletedBy,
                DeletedAt = productUnit.DeletedAt
            };

            // Soft delete the product unit
            productUnit.IsDeleted = true;
            productUnit.DeletedBy = currentUser;
            productUnit.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product unit
            await _auditLogService.TrackEntityChangesAsync(productUnit, "Delete", currentUser, originalProductUnit, cancellationToken);

            _logger.LogInformation("Product unit {ProductUnitId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product unit {ProductUnitId} for user {User}.", id, currentUser);
            throw;
        }
    }

    // Product Bundle Item management operations

    public async Task<IEnumerable<ProductBundleItemDto>> GetProductBundleItemsAsync(Guid bundleProductId, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundleItems = await _context.ProductBundleItems
                .Where(pbi => pbi.BundleProductId == bundleProductId && !pbi.IsDeleted)
                .OrderBy(pbi => pbi.ComponentProductId)
                .ToListAsync(cancellationToken);

            return bundleItems.Select(MapToProductBundleItemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bundle items for bundle {BundleProductId}.", bundleProductId);
            throw;
        }
    }

    public async Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundleItem = await _context.ProductBundleItems
                .Where(pbi => pbi.Id == id && !pbi.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return bundleItem != null ? MapToProductBundleItemDto(bundleItem) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bundle item {BundleItemId}.", id);
            throw;
        }
    }

    public async Task<ProductBundleItemDto> AddProductBundleItemAsync(CreateProductBundleItemDto createProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductBundleItemDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Check if bundle product exists
            if (!await ProductExistsAsync(createProductBundleItemDto.BundleProductId, cancellationToken))
            {
                throw new ArgumentException($"Bundle product with ID {createProductBundleItemDto.BundleProductId} does not exist.");
            }

            // Check if component product exists
            if (!await ProductExistsAsync(createProductBundleItemDto.ComponentProductId, cancellationToken))
            {
                throw new ArgumentException($"Component product with ID {createProductBundleItemDto.ComponentProductId} does not exist.");
            }

            var bundleItem = new ProductBundleItem
            {
                BundleProductId = createProductBundleItemDto.BundleProductId,
                ComponentProductId = createProductBundleItemDto.ComponentProductId,
                Quantity = createProductBundleItemDto.Quantity,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductBundleItems.Add(bundleItem);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created bundle item
            await _auditLogService.TrackEntityChangesAsync(bundleItem, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Bundle item created with ID {BundleItemId} for bundle {BundleProductId} by user {User}.",
                bundleItem.Id, createProductBundleItemDto.BundleProductId, currentUser);

            return MapToProductBundleItemDto(bundleItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bundle item for bundle {BundleProductId} by user {User}.",
                createProductBundleItemDto.BundleProductId, currentUser);
            throw;
        }
    }

    public async Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductBundleItemDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var bundleItem = await _context.ProductBundleItems
                .Where(pbi => pbi.Id == id && !pbi.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bundleItem == null)
            {
                _logger.LogWarning("Bundle item with ID {BundleItemId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Check if component product exists
            if (!await ProductExistsAsync(updateProductBundleItemDto.ComponentProductId, cancellationToken))
            {
                throw new ArgumentException($"Component product with ID {updateProductBundleItemDto.ComponentProductId} does not exist.");
            }

            // Store original for audit
            var originalBundleItem = new ProductBundleItem
            {
                Id = bundleItem.Id,
                BundleProductId = bundleItem.BundleProductId,
                ComponentProductId = bundleItem.ComponentProductId,
                Quantity = bundleItem.Quantity,
                CreatedBy = bundleItem.CreatedBy,
                CreatedAt = bundleItem.CreatedAt,
                ModifiedBy = bundleItem.ModifiedBy,
                ModifiedAt = bundleItem.ModifiedAt
            };

            // Update properties
            bundleItem.ComponentProductId = updateProductBundleItemDto.ComponentProductId;
            bundleItem.Quantity = updateProductBundleItemDto.Quantity;
            bundleItem.ModifiedBy = currentUser;
            bundleItem.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated bundle item
            await _auditLogService.TrackEntityChangesAsync(bundleItem, "Update", currentUser, originalBundleItem, cancellationToken);

            _logger.LogInformation("Bundle item {BundleItemId} updated by user {User}.", id, currentUser);

            return MapToProductBundleItemDto(bundleItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle item {BundleItemId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> RemoveProductBundleItemAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var bundleItem = await _context.ProductBundleItems
                .Where(pbi => pbi.Id == id && !pbi.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bundleItem == null)
            {
                _logger.LogWarning("Bundle item with ID {BundleItemId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalBundleItem = new ProductBundleItem
            {
                Id = bundleItem.Id,
                BundleProductId = bundleItem.BundleProductId,
                ComponentProductId = bundleItem.ComponentProductId,
                Quantity = bundleItem.Quantity,
                CreatedBy = bundleItem.CreatedBy,
                CreatedAt = bundleItem.CreatedAt,
                ModifiedBy = bundleItem.ModifiedBy,
                ModifiedAt = bundleItem.ModifiedAt,
                IsDeleted = bundleItem.IsDeleted,
                DeletedBy = bundleItem.DeletedBy,
                DeletedAt = bundleItem.DeletedAt
            };

            // Soft delete the bundle item
            bundleItem.IsDeleted = true;
            bundleItem.DeletedBy = currentUser;
            bundleItem.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted bundle item
            await _auditLogService.TrackEntityChangesAsync(bundleItem, "Delete", currentUser, originalBundleItem, cancellationToken);

            _logger.LogInformation("Bundle item {BundleItemId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bundle item {BundleItemId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<ProductDto?> UpdateProductImageAsync(Guid productId, string imageUrl, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for image update by user {User}.", productId, currentUser);
                return null;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            product.ImageUrl = imageUrl;
#pragma warning restore CS0618 // Type or member is obsolete
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} image updated successfully by user {User}.", productId, currentUser);
            return MapToProductDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image for product {ProductId} by user {User}.", productId, currentUser);
            throw;
        }
    }

    public async Task<ProductDto?> UploadProductImageAsync(Guid productId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            var product = await _context.Products
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for image upload in tenant {TenantId}.", productId, currentTenantId.Value);
                return null;
            }

            // Generate a unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"product_{productId}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/products (in production, use cloud storage)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            var storageKey = $"/images/products/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = currentTenantId.Value,
                OwnerId = productId,
                OwnerType = "Product",
                FileName = file.FileName,
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto, // Using ProfilePhoto as generic image type
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                FileSizeBytes = file.Length,
                Title = $"Product {product.Name} Image",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System" // In real implementation, get from auth context
            };

            // If product already has an image, delete the old one first
            if (product.ImageDocumentId.HasValue)
            {
                var oldDocument = await _context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == product.ImageDocumentId.Value, cancellationToken);

                if (oldDocument != null)
                {
                    // Delete old physical file
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }

                    _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _context.DocumentReferences.Add(documentReference);
            await _context.SaveChangesAsync(cancellationToken);

            // Update product with new DocumentReference ID
            product.ImageDocumentId = documentReference.Id;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = "System";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} image uploaded successfully as DocumentReference {DocumentId}.", productId, documentReference.Id);

            // Reload to get the document reference
            product.ImageDocument = documentReference;
            return MapToProductDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetProductImageDocumentAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            var product = await _context.Products
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

            if (product?.ImageDocument == null)
            {
                _logger.LogWarning("Product {ProductId} not found or has no image in tenant {TenantId}.", productId, currentTenantId.Value);
                return null;
            }

            return MapToDocumentReferenceDto(product.ImageDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image document for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<bool> DeleteProductImageAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            var product = await _context.Products
                .Include(p => p.ImageDocument)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

            if (product?.ImageDocument == null)
            {
                _logger.LogWarning("Product {ProductId} not found or has no image to delete in tenant {TenantId}.", productId, currentTenantId.Value);
                return false;
            }

            // Delete physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageDocument.StorageKey.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Remove DocumentReference
            _context.DocumentReferences.Remove(product.ImageDocument);

            // Update product
            product.ImageDocumentId = null;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = "System";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} image deleted successfully.", productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image for product {ProductId}.", productId);
            throw;
        }
    }

    // Private mapping methods

    private static ProductDto MapToProductDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Code = product.Code,
#pragma warning disable CS0618 // Type or member is obsolete
            ImageUrl = product.ImageUrl,
#pragma warning restore CS0618 // Type or member is obsolete
            ImageDocumentId = product.ImageDocumentId,
            ThumbnailUrl = product.ImageDocument?.ThumbnailStorageKey,
            Status = (EventForge.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            UnitOfMeasureId = product.UnitOfMeasureId,
            CategoryNodeId = product.CategoryNodeId,
            FamilyNodeId = product.FamilyNodeId,
            GroupNodeId = product.GroupNodeId,
            StationId = product.StationId,
            IsBundle = product.IsBundle,
            CodeCount = product.Codes.Count(c => !c.IsDeleted),
            UnitCount = product.Units.Count(u => !u.IsDeleted),
            BundleItemCount = product.BundleItems.Count(bi => !bi.IsDeleted),
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            ModifiedAt = product.ModifiedAt,
            ModifiedBy = product.ModifiedBy
        };
    }

    private static ProductDetailDto MapToProductDetailDto(Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Code = product.Code,
#pragma warning disable CS0618 // Type or member is obsolete
            ImageUrl = product.ImageUrl,
#pragma warning restore CS0618 // Type or member is obsolete
            ImageDocumentId = product.ImageDocumentId,
            ThumbnailUrl = product.ImageDocument?.ThumbnailStorageKey,
            Status = (EventForge.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            UnitOfMeasureId = product.UnitOfMeasureId,
            CategoryNodeId = product.CategoryNodeId,
            FamilyNodeId = product.FamilyNodeId,
            GroupNodeId = product.GroupNodeId,
            StationId = product.StationId,
            IsBundle = product.IsBundle,
            Codes = product.Codes.Where(c => !c.IsDeleted).Select(MapToProductCodeDto),
            Units = product.Units.Where(u => !u.IsDeleted).Select(MapToProductUnitDto),
            BundleItems = product.BundleItems.Where(bi => !bi.IsDeleted).Select(MapToProductBundleItemDto),
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            ModifiedAt = product.ModifiedAt,
            ModifiedBy = product.ModifiedBy
        };
    }

    private static ProductCodeDto MapToProductCodeDto(ProductCode productCode)
    {
        return new ProductCodeDto
        {
            Id = productCode.Id,
            ProductId = productCode.ProductId,
            CodeType = productCode.CodeType,
            Code = productCode.Code,
            AlternativeDescription = productCode.AlternativeDescription,
            CreatedAt = productCode.CreatedAt,
            CreatedBy = productCode.CreatedBy,
            ModifiedAt = productCode.ModifiedAt,
            ModifiedBy = productCode.ModifiedBy
        };
    }

    private static ProductUnitDto MapToProductUnitDto(ProductUnit productUnit)
    {
        return new ProductUnitDto
        {
            Id = productUnit.Id,
            ProductId = productUnit.ProductId,
            UnitOfMeasureId = productUnit.UnitOfMeasureId,
            ConversionFactor = productUnit.ConversionFactor,
            UnitType = productUnit.UnitType,
            Description = productUnit.Description,
            CreatedAt = productUnit.CreatedAt,
            CreatedBy = productUnit.CreatedBy,
            ModifiedAt = productUnit.ModifiedAt,
            ModifiedBy = productUnit.ModifiedBy
        };
    }

    private static ProductBundleItemDto MapToProductBundleItemDto(ProductBundleItem bundleItem)
    {
        return new ProductBundleItemDto
        {
            Id = bundleItem.Id,
            BundleProductId = bundleItem.BundleProductId,
            ComponentProductId = bundleItem.ComponentProductId,
            Quantity = bundleItem.Quantity,
            CreatedAt = bundleItem.CreatedAt,
            CreatedBy = bundleItem.CreatedBy,
            ModifiedAt = bundleItem.ModifiedAt,
            ModifiedBy = bundleItem.ModifiedBy
        };
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} exists.", productId);
            throw;
        }
    }

    private static EventForge.DTOs.Teams.DocumentReferenceDto MapToDocumentReferenceDto(EventForge.Server.Data.Entities.Teams.DocumentReference documentReference)
    {
        return new EventForge.DTOs.Teams.DocumentReferenceDto
        {
            Id = documentReference.Id,
            OwnerId = documentReference.OwnerId,
            OwnerType = documentReference.OwnerType,
            FileName = documentReference.FileName,
            Type = documentReference.Type,
            SubType = documentReference.SubType,
            MimeType = documentReference.MimeType,
            StorageKey = documentReference.StorageKey,
            Url = documentReference.Url,
            ThumbnailStorageKey = documentReference.ThumbnailStorageKey,
            Expiry = documentReference.Expiry,
            FileSizeBytes = documentReference.FileSizeBytes,
            Title = documentReference.Title,
            Notes = documentReference.Notes,
            CreatedAt = documentReference.CreatedAt,
            CreatedBy = documentReference.CreatedBy,
            ModifiedAt = documentReference.ModifiedAt,
            ModifiedBy = documentReference.ModifiedBy
        };
    }
}