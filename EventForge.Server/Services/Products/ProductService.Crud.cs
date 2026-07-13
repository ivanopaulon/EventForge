using EventForge.Server.Services.CodeGeneration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Products;
using EntityProductCodeStatus = EventForge.Server.Data.Entities.Products.ProductCodeStatus;
using EntityProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;
using EntityProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;


namespace EventForge.Server.Services.Products;

public partial class ProductService
{
    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createProductDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        // Generate code if not provided
        if (string.IsNullOrWhiteSpace(createProductDto.Code))
        {
            createProductDto.Code = await codeGenerator.GenerateDailyCodeAsync(cancellationToken);
            logger.LogInformation("Auto-generated product code: {Code}", createProductDto.Code);
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

                _ = context.Products.Add(product);
                _ = await context.SaveChangesAsync(cancellationToken);

                // Audit log for the created product
                _ = await auditLogService.TrackEntityChangesAsync(product, "Create", currentUser, null, cancellationToken);

                logger.LogInformation("Product created with ID {ProductId} and Code {Code} by user {User}. IsVatIncluded: {IsVatIncluded}",
                    product.Id, product.Code, currentUser, product.IsVatIncluded);

                return MapToProductDto(product);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                // Unique constraint violation - regenerate code and retry
                if (attempt < MaxRetryAttempts)
                {
                    logger.LogWarning("Unique constraint violation on attempt {Attempt} for code {Code}. Retrying...", attempt, createProductDto.Code);
                    createProductDto.Code = await codeGenerator.GenerateDailyCodeAsync(cancellationToken);

                    // Reset the context to clear tracked entities
                    context.ChangeTracker.Clear();
                }
                else
                {
                    logger.LogError(ex, "Failed to create product after {MaxRetryAttempts} attempts due to unique constraint violations.", MaxRetryAttempts);
                    throw new InvalidOperationException($"Unable to generate a unique product code after {MaxRetryAttempts} attempts. Please try again.", ex);
                }
            }
        }

        // This should never be reached
        throw new InvalidOperationException("Unexpected error in product creation retry logic.");
    }

    public async Task<ProductDetailDto> CreateProductWithCodesAndUnitsAsync(CreateProductWithCodesAndUnitsDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        // Generate code if not provided
        if (string.IsNullOrWhiteSpace(createDto.Code))
        {
            createDto.Code = await codeGenerator.GenerateDailyCodeAsync(cancellationToken);
            logger.LogInformation("Auto-generated product code: {Code}", createDto.Code);
        }

        // Use transaction to ensure atomicity
        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

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

            _ = context.Products.Add(product);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Audit log for the created product
            _ = await auditLogService.TrackEntityChangesAsync(product, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Product created with ID {ProductId} and Code {Code} by user {User}.", product.Id, product.Code, currentUser);

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
                    productUnit = await context.ProductUnits
                        .Where(pu => pu.ProductId == product.Id &&
                               pu.UnitOfMeasureId == codeWithUnit.UnitOfMeasureId.Value &&
                               !pu.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    // Create new ProductUnit if it doesn't exist
                    if (productUnit is null)
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

                        _ = context.ProductUnits.Add(productUnit);
                        _ = await context.SaveChangesAsync(cancellationToken);

                        // Audit log for the created product unit
                        _ = await auditLogService.TrackEntityChangesAsync(productUnit, "Create", currentUser, null, cancellationToken);

                        createdUnits.Add(productUnit);
                        logger.LogInformation("Product unit created with ID {ProductUnitId} for product {ProductId} with conversion factor {ConversionFactor}.",
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

                _ = context.ProductCodes.Add(productCode);
                _ = await context.SaveChangesAsync(cancellationToken);

                // Audit log for the created product code
                _ = await auditLogService.TrackEntityChangesAsync(productCode, "Create", currentUser, null, cancellationToken);

                createdCodes.Add(productCode);
                logger.LogInformation("Product code created with ID {ProductCodeId} for product {ProductId} with code {Code}.",
                    productCode.Id, product.Id, productCode.Code);
            }

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // Reload product with all related entities for the response
            var createdProduct = await context.Products
                .AsNoTracking()
                .Where(p => p.Id == product.Id && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .Include(p => p.Brand)
                .Include(p => p.Model)
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogInformation("Product created successfully with {CodeCount} codes and {UnitCount} units.",
                createdCodes.Count, createdUnits.Count);

            return MapToProductDetailDto(createdProduct!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateProductDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for product operations.");

            var product = await context.Products
                .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                logger.LogWarning("Product with ID {ProductId} not found for update by user {User}.", id, currentUser);
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
            logger.LogInformation("UpdateProductAsync - ProductId: {ProductId}, Incoming IsVatIncluded: {IncomingIsVatIncluded}, Current IsVatIncluded: {CurrentIsVatIncluded}",
                id, updateProductDto.IsVatIncluded, originalProduct.IsVatIncluded);

            // Update properties
            product.Name = updateProductDto.Name;
            product.ShortDescription = updateProductDto.ShortDescription;
            product.Description = updateProductDto.Description;
            // Note: Code and IsBundle are intentionally not updatable after creation
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
            logger.LogInformation("UpdateProductAsync - ProductId: {ProductId}, IsVatIncluded value before SaveChanges: {IsVatIncludedBeforeSave}",
                id, product.IsVatIncluded);

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating product {ProductId}.", id);
                throw new InvalidOperationException("Il prodotto è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log for the updated product
            _ = await auditLogService.TrackEntityChangesAsync(product, "Update", currentUser, originalProduct, cancellationToken);

            logger.LogInformation("Product {ProductId} updated by user {User}. IsVatIncluded changed from {OldValue} to {NewValue}",
                id, currentUser, originalProduct.IsVatIncluded, product.IsVatIncluded);

            return MapToProductDto(product);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for product operations.");

            var product = await context.Products
                .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
                .Include(p => p.Codes.Where(c => !c.IsDeleted))
                .Include(p => p.Units.Where(u => !u.IsDeleted))
                .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                logger.LogWarning("Product with ID {ProductId} not found for deletion by user {User}.", id, currentUser);
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

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting product {ProductId}.", id);
                throw new InvalidOperationException("Il prodotto è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log for the deleted product
            _ = await auditLogService.TrackEntityChangesAsync(product, "Delete", currentUser, originalProduct, cancellationToken);

            logger.LogInformation("Product {ProductId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    // Product Code management operations

}
