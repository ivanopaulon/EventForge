using EventForge.DTOs.PriceHistory;
using EventForge.DTOs.Products;
using EventForge.Server.Services.CodeGeneration;
using EventForge.Server.Services.PriceHistory;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using EntityProductCodeStatus = EventForge.Server.Data.Entities.Products.ProductCodeStatus;
using EntityProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;
using EntityProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;

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
    private readonly IDailyCodeGenerator _codeGenerator;
    private readonly ISupplierProductPriceHistoryService _priceHistoryService;

    // Default currency for product transactions
    private const string DefaultCurrency = "EUR";

    // Maximum retry attempts for unique constraint violations
    private const int MaxRetryAttempts = 3;

    public ProductService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ProductService> logger,
        IDailyCodeGenerator codeGenerator,
        ISupplierProductPriceHistoryService priceHistoryService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _priceHistoryService = priceHistoryService ?? throw new ArgumentNullException(nameof(priceHistoryService));
    }

    // Product CRUD operations

    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            var query = _context.Products.WhereActiveTenant(currentTenantId.Value);

            // Apply search filter if provided (before includes to avoid type conversion issues)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Code.ToLower().Contains(lowerSearchTerm) ||
                    p.Name.ToLower().Contains(lowerSearchTerm) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(lowerSearchTerm)));
            }

            query = query
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
                .Include(p => p.Brand)
                .Include(p => p.Model)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null) return null;

            string? preferredSupplierName = null;
            if (product.PreferredSupplierId.HasValue)
            {
                var currentTenantId = _tenantContext.CurrentTenantId;
                preferredSupplierName = await _context.BusinessParties
                    .Where(bp => bp.Id == product.PreferredSupplierId.Value && !bp.IsDeleted && (!currentTenantId.HasValue || bp.TenantId == currentTenantId.Value))
                    .Select(bp => bp.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var dto = MapToProductDto(product);
            dto.PreferredSupplierName = preferredSupplierName;
            return dto;
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
                .Include(p => p.Brand)
                .Include(p => p.Model)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null) return null;

            string? preferredSupplierName = null;
            if (product.PreferredSupplierId.HasValue)
            {
                var currentTenantId = _tenantContext.CurrentTenantId;
                preferredSupplierName = await _context.BusinessParties
                    .Where(bp => bp.Id == product.PreferredSupplierId.Value && !bp.IsDeleted && (!currentTenantId.HasValue || bp.TenantId == currentTenantId.Value))
                    .Select(bp => bp.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var dto = MapToProductDetailDto(product);
            dto.PreferredSupplierName = preferredSupplierName;
            return dto;
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

            // Generate code if not provided
            if (string.IsNullOrWhiteSpace(createProductDto.Code))
            {
                createProductDto.Code = await _codeGenerator.GenerateDailyCodeAsync(cancellationToken);
                _logger.LogInformation("Auto-generated product code: {Code}", createProductDto.Code);
            }

            // Retry logic for unique constraint violations
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
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
                        Status = (EntityProductStatus)createProductDto.Status,
                        IsVatIncluded = createProductDto.IsVatIncluded,
                        DefaultPrice = createProductDto.DefaultPrice,
                        VatRateId = createProductDto.VatRateId,
                        UnitOfMeasureId = createProductDto.UnitOfMeasureId,
                        CategoryNodeId = createProductDto.CategoryNodeId,
                        FamilyNodeId = createProductDto.FamilyNodeId,
                        GroupNodeId = createProductDto.GroupNodeId,
                        StationId = createProductDto.StationId,
                        IsBundle = createProductDto.IsBundle,
                        BrandId = createProductDto.BrandId,
                        ModelId = createProductDto.ModelId,
                        PreferredSupplierId = createProductDto.PreferredSupplierId,
                        ReorderPoint = createProductDto.ReorderPoint,
                        SafetyStock = createProductDto.SafetyStock,
                        TargetStockLevel = createProductDto.TargetStockLevel,
                        AverageDailyDemand = createProductDto.AverageDailyDemand,
                        CreatedBy = currentUser,
                        CreatedAt = DateTime.UtcNow
                    };

                    _ = _context.Products.Add(product);
                    _ = await _context.SaveChangesAsync(cancellationToken);

                    // Audit log for the created product
                    _ = await _auditLogService.TrackEntityChangesAsync(product, "Create", currentUser, null, cancellationToken);

                    _logger.LogInformation("Product created with ID {ProductId} and Code {Code} by user {User}. IsVatIncluded: {IsVatIncluded}",
                        product.Id, product.Code, currentUser, product.IsVatIncluded);

                    return MapToProductDto(product);
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
                {
                    // Unique constraint violation - regenerate code and retry
                    if (attempt < MaxRetryAttempts)
                    {
                        _logger.LogWarning("Unique constraint violation on attempt {Attempt} for code {Code}. Retrying...", attempt, createProductDto.Code);
                        createProductDto.Code = await _codeGenerator.GenerateDailyCodeAsync(cancellationToken);

                        // Reset the context to clear tracked entities
                        _context.ChangeTracker.Clear();
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to create product after {MaxRetryAttempts} attempts due to unique constraint violations.", MaxRetryAttempts);
                        throw new InvalidOperationException($"Unable to generate a unique product code after {MaxRetryAttempts} attempts. Please try again.", ex);
                    }
                }
            }

            // This should never be reached
            throw new InvalidOperationException("Unexpected error in product creation retry logic.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for user {User}.", currentUser);
            throw;
        }
    }

    public async Task<ProductDetailDto> CreateProductWithCodesAndUnitsAsync(CreateProductWithCodesAndUnitsDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Generate code if not provided
            if (string.IsNullOrWhiteSpace(createDto.Code))
            {
                createDto.Code = await _codeGenerator.GenerateDailyCodeAsync(cancellationToken);
                _logger.LogInformation("Auto-generated product code: {Code}", createDto.Code);
            }

            // Use transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Create the product
                var product = new Product
                {
                    TenantId = currentTenantId.Value,
                    Name = createDto.Name,
                    ShortDescription = createDto.ShortDescription,
                    Description = createDto.Description,
                    Code = createDto.Code,
                    Status = (EntityProductStatus)createDto.Status,
                    IsVatIncluded = createDto.IsVatIncluded,
                    DefaultPrice = createDto.DefaultPrice,
                    VatRateId = createDto.VatRateId,
                    UnitOfMeasureId = createDto.UnitOfMeasureId,
                    CategoryNodeId = createDto.CategoryNodeId,
                    FamilyNodeId = createDto.FamilyNodeId,
                    GroupNodeId = createDto.GroupNodeId,
                    StationId = createDto.StationId,
                    BrandId = createDto.BrandId,
                    ModelId = createDto.ModelId,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                };

                _ = _context.Products.Add(product);
                _ = await _context.SaveChangesAsync(cancellationToken);

                // Audit log for the created product
                _ = await _auditLogService.TrackEntityChangesAsync(product, "Create", currentUser, null, cancellationToken);

                _logger.LogInformation("Product created with ID {ProductId} and Code {Code} by user {User}.", product.Id, product.Code, currentUser);

                // Track created units and codes for the response
                var createdUnits = new List<ProductUnit>();
                var createdCodes = new List<ProductCode>();

                // Process codes and units
                foreach (var codeWithUnit in createDto.CodesWithUnits)
                {
                    ProductUnit? productUnit = null;

                    // Create or find ProductUnit if UnitOfMeasureId is specified
                    if (codeWithUnit.UnitOfMeasureId.HasValue)
                    {
                        // Check if a ProductUnit already exists for this product and UoM
                        productUnit = await _context.ProductUnits
                            .Where(pu => pu.ProductId == product.Id &&
                                   pu.UnitOfMeasureId == codeWithUnit.UnitOfMeasureId.Value &&
                                   !pu.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        // Create new ProductUnit if it doesn't exist
                        if (productUnit == null)
                        {
                            productUnit = new ProductUnit
                            {
                                TenantId = currentTenantId.Value,
                                ProductId = product.Id,
                                UnitOfMeasureId = codeWithUnit.UnitOfMeasureId.Value,
                                ConversionFactor = codeWithUnit.ConversionFactor,
                                UnitType = codeWithUnit.UnitType,
                                Description = codeWithUnit.UnitDescription,
                                Status = EntityProductUnitStatus.Active,
                                CreatedBy = currentUser,
                                CreatedAt = DateTime.UtcNow
                            };

                            _ = _context.ProductUnits.Add(productUnit);
                            _ = await _context.SaveChangesAsync(cancellationToken);

                            // Audit log for the created product unit
                            _ = await _auditLogService.TrackEntityChangesAsync(productUnit, "Create", currentUser, null, cancellationToken);

                            createdUnits.Add(productUnit);
                            _logger.LogInformation("Product unit created with ID {ProductUnitId} for product {ProductId} with conversion factor {ConversionFactor}.",
                                productUnit.Id, product.Id, productUnit.ConversionFactor);
                        }
                    }

                    // Create ProductCode
                    var productCode = new ProductCode
                    {
                        TenantId = currentTenantId.Value,
                        ProductId = product.Id,
                        ProductUnitId = productUnit?.Id,
                        CodeType = codeWithUnit.CodeType,
                        Code = codeWithUnit.Code,
                        AlternativeDescription = codeWithUnit.AlternativeDescription,
                        Status = EntityProductCodeStatus.Active,
                        CreatedBy = currentUser,
                        CreatedAt = DateTime.UtcNow
                    };

                    _ = _context.ProductCodes.Add(productCode);
                    _ = await _context.SaveChangesAsync(cancellationToken);

                    // Audit log for the created product code
                    _ = await _auditLogService.TrackEntityChangesAsync(productCode, "Create", currentUser, null, cancellationToken);

                    createdCodes.Add(productCode);
                    _logger.LogInformation("Product code created with ID {ProductCodeId} for product {ProductId} with code {Code}.",
                        productCode.Id, product.Id, productCode.Code);
                }

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);

                // Reload product with all related entities for the response
                var createdProduct = await _context.Products
                    .Where(p => p.Id == product.Id && !p.IsDeleted)
                    .Include(p => p.Codes.Where(c => !c.IsDeleted))
                    .Include(p => p.Units.Where(u => !u.IsDeleted))
                    .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                    .Include(p => p.Brand)
                    .Include(p => p.Model)
                    .FirstOrDefaultAsync(cancellationToken);

                _logger.LogInformation("Product created successfully with {CodeCount} codes and {UnitCount} units.",
                    createdCodes.Count, createdUnits.Count);

                return MapToProductDetailDto(createdProduct!);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with codes and units for user {User}.", currentUser);
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
                BrandId = product.BrandId,
                ModelId = product.ModelId,
                PreferredSupplierId = product.PreferredSupplierId,
                ReorderPoint = product.ReorderPoint,
                SafetyStock = product.SafetyStock,
                TargetStockLevel = product.TargetStockLevel,
                AverageDailyDemand = product.AverageDailyDemand,
                CreatedBy = product.CreatedBy,
                CreatedAt = product.CreatedAt,
                ModifiedBy = product.ModifiedBy,
                ModifiedAt = product.ModifiedAt
            };

            // Log incoming DTO values for diagnostic purposes
            _logger.LogInformation("UpdateProductAsync - ProductId: {ProductId}, Incoming IsVatIncluded: {IncomingIsVatIncluded}, Current IsVatIncluded: {CurrentIsVatIncluded}",
                id, updateProductDto.IsVatIncluded, originalProduct.IsVatIncluded);

            // Update properties
            product.Name = updateProductDto.Name;
            product.ShortDescription = updateProductDto.ShortDescription;
            product.Description = updateProductDto.Description;
            // Note: Code and IsBundle are intentionally not updatable after creation
#pragma warning disable CS0618 // Type or member is obsolete
            product.ImageUrl = updateProductDto.ImageUrl;
#pragma warning restore CS0618 // Type or member is obsolete
            product.ImageDocumentId = updateProductDto.ImageDocumentId;
            product.Status = (EntityProductStatus)updateProductDto.Status;
            product.IsVatIncluded = updateProductDto.IsVatIncluded;
            product.DefaultPrice = updateProductDto.DefaultPrice;
            product.VatRateId = updateProductDto.VatRateId;
            product.UnitOfMeasureId = updateProductDto.UnitOfMeasureId;
            product.CategoryNodeId = updateProductDto.CategoryNodeId;
            product.FamilyNodeId = updateProductDto.FamilyNodeId;
            product.GroupNodeId = updateProductDto.GroupNodeId;
            product.StationId = updateProductDto.StationId;
            product.BrandId = updateProductDto.BrandId;
            product.ModelId = updateProductDto.ModelId;
            product.PreferredSupplierId = updateProductDto.PreferredSupplierId;
            product.ReorderPoint = updateProductDto.ReorderPoint;
            product.SafetyStock = updateProductDto.SafetyStock;
            product.TargetStockLevel = updateProductDto.TargetStockLevel;
            product.AverageDailyDemand = updateProductDto.AverageDailyDemand;
            product.ModifiedBy = currentUser;
            product.ModifiedAt = DateTime.UtcNow;

            // Log product entity value before SaveChanges for diagnostic purposes
            _logger.LogInformation("UpdateProductAsync - ProductId: {ProductId}, IsVatIncluded value before SaveChanges: {IsVatIncludedBeforeSave}",
                id, product.IsVatIncluded);

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product
            _ = await _auditLogService.TrackEntityChangesAsync(product, "Update", currentUser, originalProduct, cancellationToken);

            _logger.LogInformation("Product {ProductId} updated by user {User}. IsVatIncluded changed from {OldValue} to {NewValue}",
                id, currentUser, originalProduct.IsVatIncluded, product.IsVatIncluded);

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
#pragma warning disable CS0618 // ImageUrl is obsolete but kept for backward compatibility in audit trail
            var originalProduct = new Product
            {
                Id = product.Id,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Code = product.Code,
                ImageUrl = product.ImageUrl,
                IsVatIncluded = product.IsVatIncluded,
#pragma warning restore CS0618
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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product
            _ = await _auditLogService.TrackEntityChangesAsync(product, "Delete", currentUser, originalProduct, cancellationToken);

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

    public async Task<ProductWithCodeDto?> GetProductWithCodeByCodeAsync(string codeValue, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codeValue);

            var productCode = await _context.ProductCodes
                .Where(pc => pc.Code == codeValue && !pc.IsDeleted)
                .Include(pc => pc.Product)
                    .ThenInclude(p => p.VatRate)       // ✅ Include VatRate for continuous scan
                .Include(pc => pc.Product)
                    .ThenInclude(p => p.UnitOfMeasure) // ✅ Include UnitOfMeasure for continuous scan
                .Include(pc => pc.Product)
                    .ThenInclude(p => p.Brand)         // Existing include
                .FirstOrDefaultAsync(cancellationToken);

            if (productCode?.Product == null || productCode.Product.IsDeleted)
            {
                return null;
            }

            return new ProductWithCodeDto
            {
                Product = MapToProductDto(productCode.Product),
                Code = MapToProductCodeDto(productCode)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with code by code {CodeValue}.", codeValue);
            throw;
        }
    }

    public async Task<ProductCodeDto> AddProductCodeAsync(CreateProductCodeDto createProductCodeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createProductCodeDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Validate tenant context
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Check if product exists
            if (!await ProductExistsAsync(createProductCodeDto.ProductId, cancellationToken))
            {
                throw new ArgumentException($"Product with ID {createProductCodeDto.ProductId} does not exist.");
            }

            var productCode = new ProductCode
            {
                TenantId = currentTenantId.Value,
                ProductId = createProductCodeDto.ProductId,
                ProductUnitId = createProductCodeDto.ProductUnitId,
                CodeType = createProductCodeDto.CodeType,
                Code = createProductCodeDto.Code,
                AlternativeDescription = createProductCodeDto.AlternativeDescription,
                Status = (EntityProductCodeStatus)createProductCodeDto.Status,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.ProductCodes.Add(productCode);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product code
            _ = await _auditLogService.TrackEntityChangesAsync(productCode, "Create", currentUser, null, cancellationToken);

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
            productCode.ProductUnitId = updateProductCodeDto.ProductUnitId;
            productCode.CodeType = updateProductCodeDto.CodeType;
            productCode.Code = updateProductCodeDto.Code;
            productCode.AlternativeDescription = updateProductCodeDto.AlternativeDescription;
            productCode.Status = (EntityProductCodeStatus)updateProductCodeDto.Status;
            productCode.ModifiedBy = currentUser;
            productCode.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product code
            _ = await _auditLogService.TrackEntityChangesAsync(productCode, "Update", currentUser, originalProductCode, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product code
            _ = await _auditLogService.TrackEntityChangesAsync(productCode, "Delete", currentUser, originalProductCode, cancellationToken);

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

            // Validate tenant context
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Check if product exists
            if (!await ProductExistsAsync(createProductUnitDto.ProductId, cancellationToken))
            {
                throw new ArgumentException($"Product with ID {createProductUnitDto.ProductId} does not exist.");
            }

            var productUnit = new ProductUnit
            {
                TenantId = currentTenantId.Value,
                ProductId = createProductUnitDto.ProductId,
                UnitOfMeasureId = createProductUnitDto.UnitOfMeasureId,
                ConversionFactor = createProductUnitDto.ConversionFactor,
                UnitType = createProductUnitDto.UnitType,
                Description = createProductUnitDto.Description,
                Status = EntityProductUnitStatus.Active,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.ProductUnits.Add(productUnit);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product unit
            _ = await _auditLogService.TrackEntityChangesAsync(productUnit, "Create", currentUser, null, cancellationToken);

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
            productUnit.Status = (Data.Entities.Products.ProductUnitStatus)updateProductUnitDto.Status;
            productUnit.ModifiedBy = currentUser;
            productUnit.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated product unit
            _ = await _auditLogService.TrackEntityChangesAsync(productUnit, "Update", currentUser, originalProductUnit, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted product unit
            _ = await _auditLogService.TrackEntityChangesAsync(productUnit, "Delete", currentUser, originalProductUnit, cancellationToken);

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

            _ = _context.ProductBundleItems.Add(bundleItem);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created bundle item
            _ = await _auditLogService.TrackEntityChangesAsync(bundleItem, "Create", currentUser, null, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated bundle item
            _ = await _auditLogService.TrackEntityChangesAsync(bundleItem, "Update", currentUser, originalBundleItem, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted bundle item
            _ = await _auditLogService.TrackEntityChangesAsync(bundleItem, "Delete", currentUser, originalBundleItem, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

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
            _ = Directory.CreateDirectory(uploadsFolder);

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
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                ThumbnailStorageKey = storageKey, // <- ensure thumbnail key is set
                FileSizeBytes = file.Length,
                Title = $"Product {product.Name} Image",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
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

                    _ = _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _ = _context.DocumentReferences.Add(documentReference);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Update product with new DocumentReference ID
            product.ImageDocumentId = documentReference.Id;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

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
            _ = _context.DocumentReferences.Remove(product.ImageDocument);

            // Update product
            product.ImageDocumentId = null;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(cancellationToken);

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
            ThumbnailUrl = product.ImageDocument?.Url ?? product.ImageDocument?.ThumbnailStorageKey ?? product.ImageDocument?.StorageKey,
            Status = (EventForge.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            VatRateName = product.VatRate?.Name,
            VatRatePercentage = product.VatRate?.Percentage,
            UnitOfMeasureId = product.UnitOfMeasureId,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PreferredSupplierId = product.PreferredSupplierId,
            ReorderPoint = product.ReorderPoint,
            SafetyStock = product.SafetyStock,
            TargetStockLevel = product.TargetStockLevel,
            AverageDailyDemand = product.AverageDailyDemand,
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
            ThumbnailUrl = product.ImageDocument?.Url ?? product.ImageDocument?.ThumbnailStorageKey ?? product.ImageDocument?.StorageKey,
            Status = (EventForge.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            UnitOfMeasureId = product.UnitOfMeasureId,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PreferredSupplierId = product.PreferredSupplierId,
            PreferredSupplierName = null, // resolved on client via suppliers list
            ReorderPoint = product.ReorderPoint,
            SafetyStock = product.SafetyStock,
            TargetStockLevel = product.TargetStockLevel,
            AverageDailyDemand = product.AverageDailyDemand,
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
            ProductUnitId = productCode.ProductUnitId,
            CodeType = productCode.CodeType,
            Code = productCode.Code,
            AlternativeDescription = productCode.AlternativeDescription,
            Status = (EventForge.DTOs.Common.ProductCodeStatus)productCode.Status,
            UnitOfMeasureId = productCode.ProductUnit?.UnitOfMeasureId,
            UnitOfMeasureName = productCode.ProductUnit?.UnitOfMeasure?.Name,
            ConversionFactor = productCode.ProductUnit?.ConversionFactor,
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
            Status = (DTOs.Common.ProductUnitStatus)productUnit.Status,
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

    // Product Supplier management operations

    public async Task<IEnumerable<ProductSupplierDto>> GetProductSuppliersAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            var suppliers = await _context.ProductSuppliers
                .Where(ps => ps.ProductId == productId && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
                .Include(ps => ps.Supplier)
                .Include(ps => ps.Product)
                .OrderByDescending(ps => ps.Preferred)
                .ThenBy(ps => ps.Supplier!.Name)
                .ToListAsync(cancellationToken);

            return suppliers.Select(MapToProductSupplierDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suppliers for product {ProductId}.", productId);
            throw;
        }
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            var supplier = await _context.ProductSuppliers
                .Where(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
                .Include(ps => ps.Supplier)
                .Include(ps => ps.Product)
                .FirstOrDefaultAsync(cancellationToken);

            return supplier != null ? MapToProductSupplierDto(supplier) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product supplier {Id}.", id);
            throw;
        }
    }

    public async Task<ProductSupplierDto> AddProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            // Validate product exists
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == createProductSupplierDto.ProductId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID {createProductSupplierDto.ProductId} not found.");
            }

            // Validate bundle products cannot have suppliers
            if (product.IsBundle)
            {
                throw new InvalidOperationException("Bundle products cannot have suppliers.");
            }

            // Validate supplier exists and is a supplier type
            var supplier = await _context.BusinessParties
                .FirstOrDefaultAsync(bp => bp.Id == createProductSupplierDto.SupplierId && !bp.IsDeleted && bp.TenantId == currentTenantId.Value, cancellationToken);

            if (supplier == null)
            {
                throw new InvalidOperationException($"Supplier with ID {createProductSupplierDto.SupplierId} not found.");
            }

            if (supplier.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.Fornitore && supplier.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.ClienteFornitore)
            {
                throw new InvalidOperationException("The selected business party is not a supplier.");
            }

            // If this is preferred, unset any other preferred suppliers for this product
            if (createProductSupplierDto.Preferred)
            {
                var existingPreferred = await _context.ProductSuppliers
                    .Where(ps => ps.ProductId == createProductSupplierDto.ProductId && ps.Preferred && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
                    .ToListAsync(cancellationToken);

                foreach (var ps in existingPreferred)
                {
                    ps.Preferred = false;
                    ps.ModifiedBy = currentUser;
                    ps.ModifiedAt = DateTime.UtcNow;
                }
            }

            var productSupplier = new ProductSupplier
            {
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
                TenantId = currentTenantId.Value,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.ProductSuppliers.Add(productSupplier);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync(
                "ProductSupplier",
                productSupplier.Id,
                "SupplierId",
                "Create",
                null,
                supplier.Name,
                currentUser,
                $"Added supplier {supplier.Name} to product {product.Name}",
                cancellationToken
            );

            // Reload with navigation properties
            await _context.Entry(productSupplier).Reference(ps => ps.Supplier).LoadAsync(cancellationToken);
            await _context.Entry(productSupplier).Reference(ps => ps.Product).LoadAsync(cancellationToken);

            return MapToProductSupplierDto(productSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding supplier to product.");
            throw;
        }
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            var productSupplier = await _context.ProductSuppliers
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value, cancellationToken);

            if (productSupplier == null)
            {
                return null;
            }

            // Validate product exists if changed
            if (productSupplier.ProductId != updateProductSupplierDto.ProductId)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == updateProductSupplierDto.ProductId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

                if (product == null)
                {
                    throw new InvalidOperationException($"Product with ID {updateProductSupplierDto.ProductId} not found.");
                }

                if (product.IsBundle)
                {
                    throw new InvalidOperationException("Bundle products cannot have suppliers.");
                }
            }

            // Validate supplier exists and is a supplier type if changed
            if (productSupplier.SupplierId != updateProductSupplierDto.SupplierId)
            {
                var supplier = await _context.BusinessParties
                    .FirstOrDefaultAsync(bp => bp.Id == updateProductSupplierDto.SupplierId && !bp.IsDeleted && bp.TenantId == currentTenantId.Value, cancellationToken);

                if (supplier == null)
                {
                    throw new InvalidOperationException($"Supplier with ID {updateProductSupplierDto.SupplierId} not found.");
                }

                if (supplier.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.Fornitore && supplier.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.ClienteFornitore)
                {
                    throw new InvalidOperationException("The selected business party is not a supplier.");
                }
            }

            // If this is being set as preferred, unset any other preferred suppliers for this product
            if (updateProductSupplierDto.Preferred && !productSupplier.Preferred)
            {
                var existingPreferred = await _context.ProductSuppliers
                    .Where(ps => ps.ProductId == updateProductSupplierDto.ProductId && ps.Preferred && ps.Id != id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
                    .ToListAsync(cancellationToken);

                foreach (var ps in existingPreferred)
                {
                    ps.Preferred = false;
                    ps.ModifiedBy = currentUser;
                    ps.ModifiedAt = DateTime.UtcNow;
                }
            }

            // Capture old values for price history logging
            var oldUnitCost = productSupplier.UnitCost ?? 0;
            var newUnitCost = updateProductSupplierDto.UnitCost ?? 0;
            var oldLeadTimeDays = productSupplier.LeadTimeDays;
            var newLeadTimeDays = updateProductSupplierDto.LeadTimeDays;
            var oldCurrency = productSupplier.Currency ?? DefaultCurrency;
            var newCurrency = updateProductSupplierDto.Currency ?? DefaultCurrency;

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
            productSupplier.ModifiedBy = currentUser;
            productSupplier.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log price change if price has changed
            if (oldUnitCost != newUnitCost && _tenantContext.CurrentUserId.HasValue)
            {
                try
                {
                    await _priceHistoryService.LogPriceChangeAsync(new PriceChangeLogRequest
                    {
                        ProductSupplierId = productSupplier.Id,
                        SupplierId = productSupplier.SupplierId,
                        ProductId = productSupplier.ProductId,
                        OldPrice = oldUnitCost,
                        NewPrice = newUnitCost,
                        Currency = newCurrency,
                        OldLeadTimeDays = oldLeadTimeDays,
                        NewLeadTimeDays = newLeadTimeDays,
                        ChangeSource = "Manual",
                        UserId = _tenantContext.CurrentUserId.Value
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the update if price history logging fails
                    _logger.LogWarning(ex, "Failed to log price history for ProductSupplier {Id}", id);
                }
            }

            _ = await _auditLogService.LogEntityChangeAsync(
                "ProductSupplier",
                productSupplier.Id,
                "ProductSupplier",
                "Update",
                null,
                "Updated",
                currentUser,
                $"Updated supplier relationship for product",
                cancellationToken
            );

            return MapToProductSupplierDto(productSupplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product supplier {Id}.", id);
            throw;
        }
    }

    public async Task<bool> RemoveProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product supplier operations.");
            }

            var productSupplier = await _context.ProductSuppliers
                .FirstOrDefaultAsync(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value, cancellationToken);

            if (productSupplier == null)
            {
                return false;
            }

            productSupplier.IsDeleted = true;
            productSupplier.ModifiedBy = currentUser;
            productSupplier.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync(
                "ProductSupplier",
                productSupplier.Id,
                "IsDeleted",
                "Delete",
                "false",
                "true",
                currentUser,
                $"Removed supplier from product",
                cancellationToken
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product supplier {Id}.", id);
            throw;
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

    public async Task<IEnumerable<ProductWithAssociationDto>> GetProductsWithSupplierAssociationAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        // Ensure tenant context available for association filtering
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        // Get all products (preserve previous behaviour: products may be global)
        var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        // Get all existing associations for this supplier within the current tenant
        var associations = await _context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
            .ToListAsync(cancellationToken);

        var associationDict = associations.ToDictionary(a => a.ProductId);

        return products.Select(p =>
        {
            // Try to get association; use null-safe operators to avoid possible null dereference warnings
            associationDict.TryGetValue(p.Id, out var association);
            return new ProductWithAssociationDto
            {
                ProductId = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.ShortDescription,
                IsAssociated = association != null,
                ProductSupplierId = association?.Id,
                UnitCost = association?.UnitCost,
                SupplierProductCode = association?.SupplierProductCode,
                Preferred = association?.Preferred ?? false
            };
        }).ToList();
    }

    public async Task<int> BulkUpdateProductSupplierAssociationsAsync(Guid supplierId, IEnumerable<Guid> productIds, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var productIdList = productIds.ToList();
        var now = DateTime.UtcNow;

        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        // Get existing associations for this supplier within the tenant
        var existingAssociations = await _context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
            .ToListAsync(cancellationToken);

        var existingProductIds = existingAssociations.Select(a => a.ProductId).ToHashSet();

        // Determine which associations to add
        var productIdsToAdd = productIdList.Except(existingProductIds).ToList();

        // Determine which associations to remove (soft delete)
        var associationsToRemove = existingAssociations
            .Where(a => !productIdList.Contains(a.ProductId))
            .ToList();

        // Add new associations and set TenantId
        foreach (var productId in productIdsToAdd)
        {
            var newAssociation = new ProductSupplier
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                SupplierId = supplierId,
                Preferred = false,
                CreatedAt = now,
                CreatedBy = currentUser,
                ModifiedAt = now,
                ModifiedBy = currentUser,
                IsDeleted = false,
                TenantId = currentTenantId.Value
            };
            _context.ProductSuppliers.Add(newAssociation);
        }

        // Soft delete removed associations (already scoped to tenant)
        foreach (var association in associationsToRemove)
        {
            association.IsDeleted = true;
            association.ModifiedAt = now;
            association.ModifiedBy = currentUser;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return productIdsToAdd.Count;
    }

    public async Task<PagedResult<ProductSupplierDto>> GetProductsBySupplierAsync(
        Guid supplierId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        try
        {
            // Query product suppliers for this supplier
            var query = _context.ProductSuppliers
                .Where(ps => ps.SupplierId == supplierId &&
                            !ps.IsDeleted &&
                            ps.TenantId == currentTenantId.Value)
                .Include(ps => ps.Product)
                .Include(ps => ps.Supplier)
                .OrderByDescending(ps => ps.Preferred)
                .ThenBy(ps => ps.Product!.Name);

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (page - 1) * pageSize;

            var productSuppliers = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Get product IDs to fetch latest purchase data
            var productIds = productSuppliers.Select(ps => ps.ProductId).ToList();

            // Get latest purchase prices from approved document rows
            var latestPurchases = await _context.DocumentRows
                .Where(dr => dr.ProductId.HasValue &&
                            productIds.Contains(dr.ProductId.Value) &&
                            !dr.IsDeleted &&
                            dr.TenantId == currentTenantId.Value)
                .Include(dr => dr.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .Where(dr => dr.DocumentHeader != null &&
                            !dr.DocumentHeader.IsDeleted &&
                            dr.DocumentHeader.BusinessPartyId == supplierId &&
                            dr.DocumentHeader.ApprovalStatus == EventForge.Server.Data.Entities.Documents.ApprovalStatus.Approved &&
                            dr.DocumentHeader.DocumentType != null &&
                            dr.DocumentHeader.DocumentType.IsStockIncrease == true &&
                            dr.DocumentHeader.TenantId == currentTenantId.Value)
                .GroupBy(dr => dr.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    LastPurchasePrice = g.OrderByDescending(dr => dr.DocumentHeader!.Date)
                                         .Select(dr => dr.UnitPrice)
                                         .FirstOrDefault(),
                    LastPurchaseDate = g.OrderByDescending(dr => dr.DocumentHeader!.Date)
                                        .Select(dr => dr.DocumentHeader!.Date)
                                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var latestPurchaseDict = latestPurchases.ToDictionary(lp => lp.ProductId);

            // Map to DTOs
            var items = productSuppliers.Select(ps =>
            {
                var dto = new ProductSupplierDto
                {
                    Id = ps.Id,
                    ProductId = ps.ProductId,
                    ProductName = ps.Product?.Name,
                    SupplierId = ps.SupplierId,
                    SupplierName = ps.Supplier?.Name,
                    SupplierProductCode = ps.SupplierProductCode,
                    PurchaseDescription = ps.PurchaseDescription,
                    UnitCost = ps.UnitCost,
                    Currency = ps.Currency,
                    MinOrderQty = ps.MinOrderQty,
                    IncrementQty = ps.IncrementQty,
                    LeadTimeDays = ps.LeadTimeDays,
                    Preferred = ps.Preferred,
                    Notes = ps.Notes,
                    CreatedAt = ps.CreatedAt,
                    CreatedBy = ps.CreatedBy
                };

                // Enrich with latest purchase data
                if (latestPurchaseDict.TryGetValue(ps.ProductId, out var latestPurchase))
                {
                    dto.LastPurchasePrice = latestPurchase.LastPurchasePrice;
                    dto.LastPurchaseDate = latestPurchase.LastPurchaseDate;
                }

                return dto;
            }).ToList();

            return new PagedResult<ProductSupplierDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by supplier {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<IEnumerable<RecentProductTransactionDto>> GetRecentProductTransactionsAsync(
        Guid productId,
        string type = "purchase",
        Guid? partyId = null,
        int top = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product operations.");
            }

            // Determine if we're looking for purchases (stock increase) or sales (stock decrease)
            bool isStockIncrease = type.Equals("purchase", StringComparison.OrdinalIgnoreCase);

            // Query document rows with all necessary joins
            var query = _context.DocumentRows
                .Where(r => r.ProductId == productId &&
                            !r.IsDeleted &&
                            r.TenantId == currentTenantId.Value)
                .Include(r => r.DocumentHeader)
                    .ThenInclude(h => h!.DocumentType)
                .Include(r => r.DocumentHeader)
                    .ThenInclude(h => h!.BusinessParty)
                .Where(r => r.DocumentHeader != null &&
                            !r.DocumentHeader.IsDeleted &&
                            r.DocumentHeader.ApprovalStatus == EventForge.Server.Data.Entities.Documents.ApprovalStatus.Approved &&
                            r.DocumentHeader.DocumentType != null &&
                            r.DocumentHeader.DocumentType.IsStockIncrease == isStockIncrease &&
                            r.DocumentHeader.TenantId == currentTenantId.Value);

            // Filter by party if provided
            if (partyId.HasValue)
            {
                query = query.Where(r => r.DocumentHeader!.BusinessPartyId == partyId.Value);
            }

            // Order by document date (most recent first) and created date
            var rows = await query
                .OrderByDescending(r => r.DocumentHeader!.Date)
                .ThenByDescending(r => r.CreatedAt)
                .Take(top)
                .ToListAsync(cancellationToken);

            // Map to DTOs and calculate effective prices
            var transactions = rows.Select(row =>
            {
                var header = row.DocumentHeader!;

                // Calculate normalized unit price (use BaseUnitPrice if available, otherwise UnitPrice)
                decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;

                // Calculate unit discount
                decimal unitDiscount = 0;
                if (row.DiscountType == EventForge.DTOs.Common.DiscountType.Percentage)
                {
                    unitDiscount = unitPriceNormalized * (row.LineDiscount / 100);
                }
                else if (row.LineDiscountValue > 0 && row.Quantity > 0)
                {
                    unitDiscount = row.LineDiscountValue / row.Quantity;
                }

                // Clamp discount to not exceed unit price
                unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);

                // Calculate effective unit price (after discount)
                decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

                // Use normalized quantity (BaseQuantity if available, otherwise Quantity)
                decimal quantityNormalized = row.BaseQuantity ?? row.Quantity;

                return new RecentProductTransactionDto
                {
                    DocumentHeaderId = header.Id,
                    DocumentNumber = header.Number,
                    DocumentDate = header.Date,
                    DocumentRowId = row.Id,
                    PartyId = header.BusinessPartyId,
                    PartyName = header.BusinessParty?.Name ?? string.Empty,
                    ProductId = row.ProductId!.Value,
                    Quantity = quantityNormalized,
                    EffectiveUnitPrice = Math.Round(effectiveUnitPrice, 2),
                    UnitPriceRaw = row.UnitPrice,
                    BaseUnitPrice = row.BaseUnitPrice,
                    Currency = DefaultCurrency,
                    UnitOfMeasure = row.UnitOfMeasure,
                    DiscountType = row.DiscountType.ToString(),
                    Discount = row.DiscountType == EventForge.DTOs.Common.DiscountType.Percentage
                        ? row.LineDiscount
                        : row.LineDiscountValue
                };
            }).ToList();

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent product transactions for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<ProductSearchResultDto> SearchProductsAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for product search operations.");
            }

            var result = new ProductSearchResultDto();

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            var queryTrimmed = query.Trim();

            // Step 1: Try exact match on ProductCodes.Code (case-insensitive)
            var productCode = await _context.ProductCodes
                .WhereActiveTenant(currentTenantId.Value)
                .Include(pc => pc.Product)
                    .ThenInclude(p => p.Brand)
                .Include(pc => pc.Product)
                    .ThenInclude(p => p.VatRate)
                .Include(pc => pc.ProductUnit)
                    .ThenInclude(pu => pu!.UnitOfMeasure)
                .FirstOrDefaultAsync(pc => pc.Code.ToLower() == queryTrimmed.ToLower(), cancellationToken);

            if (productCode?.Product != null && productCode.Product.Status == EntityProductStatus.Active)
            {
                result.IsExactCodeMatch = true;
                result.ExactMatch = new ProductWithCodeDto
                {
                    Product = MapToProductDto(productCode.Product),
                    Code = MapToProductCodeDto(productCode)
                };
                result.TotalCount = 1;
                return result;
            }

            // Step 2: Try exact match on Product.Code (case-insensitive)
            var productByCode = await _context.Products
                .WhereActiveTenant(currentTenantId.Value)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .FirstOrDefaultAsync(p => p.Code != null && p.Code.ToLower() == queryTrimmed.ToLower(), cancellationToken);

            if (productByCode != null && productByCode.Status == EntityProductStatus.Active)
            {
                result.IsExactCodeMatch = true;
                result.ExactMatch = new ProductWithCodeDto
                {
                    Product = MapToProductDto(productByCode),
                    Code = null
                };
                result.TotalCount = 1;
                return result;
            }

            // Step 3: Text search in Name, ShortDescription, Description, Brand.Name (case-insensitive)
            var searchResults = await _context.Products
                .WhereActiveTenant(currentTenantId.Value)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .Where(p => p.Status == EntityProductStatus.Active &&
                    (EF.Functions.Like(p.Name, $"%{queryTrimmed}%") ||
                     (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, $"%{queryTrimmed}%")) ||
                     (p.Description != null && EF.Functions.Like(p.Description, $"%{queryTrimmed}%")) ||
                     (p.Brand != null && EF.Functions.Like(p.Brand.Name, $"%{queryTrimmed}%"))))
                .Take(maxResults)
                .ToListAsync(cancellationToken);

            result.SearchResults = searchResults.Select(MapToProductDto).ToList();
            result.TotalCount = result.SearchResults.Count;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing unified product search for query {Query}", query);
            throw;
        }
    }
}