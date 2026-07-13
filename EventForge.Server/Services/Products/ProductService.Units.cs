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
    public async Task<IEnumerable<ProductUnitDto>> GetProductUnitsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var units = await context.ProductUnits
            .AsNoTracking()
            .Where(pu => pu.ProductId == productId && !pu.IsDeleted)
            .OrderBy(pu => pu.UnitType)
            .ThenBy(pu => pu.ConversionFactor)
            .ToListAsync(cancellationToken);

        return units.Select(MapToProductUnitDto);
    }

    public async Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await context.ProductUnits
            .AsNoTracking()
            .Where(pu => pu.Id == id && !pu.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return unit is not null ? MapToProductUnitDto(unit) : null;
    }

    public async Task<ProductUnitDto> AddProductUnitAsync(CreateProductUnitDto createProductUnitDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createProductUnitDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        // Validate tenant context
        var currentTenantId = tenantContext.CurrentTenantId;
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

        _ = context.ProductUnits.Add(productUnit);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the created product unit
        _ = await auditLogService.TrackEntityChangesAsync(productUnit, "Create", currentUser, null, cancellationToken);

        logger.LogInformation("Product unit created with ID {ProductUnitId} for product {ProductId} by user {User}.",
            productUnit.Id, createProductUnitDto.ProductId, currentUser);

        return MapToProductUnitDto(productUnit);
    }

    public async Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateProductUnitDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateProductUnitDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for product operations.");

        var productUnit = await context.ProductUnits
            .Where(pu => pu.Id == id && pu.TenantId == currentTenantId && !pu.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (productUnit is null)
        {
            logger.LogWarning("Product unit with ID {ProductUnitId} not found for update by user {User}.", id, currentUser);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the updated product unit
        _ = await auditLogService.TrackEntityChangesAsync(productUnit, "Update", currentUser, originalProductUnit, cancellationToken);

        logger.LogInformation("Product unit {ProductUnitId} updated by user {User}.", id, currentUser);

        return MapToProductUnitDto(productUnit);
    }

    public async Task<bool> RemoveProductUnitAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for product operations.");

        var productUnit = await context.ProductUnits
            .Where(pu => pu.Id == id && pu.TenantId == currentTenantId && !pu.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (productUnit is null)
        {
            logger.LogWarning("Product unit with ID {ProductUnitId} not found for deletion by user {User}.", id, currentUser);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the deleted product unit
        _ = await auditLogService.TrackEntityChangesAsync(productUnit, "Delete", currentUser, originalProductUnit, cancellationToken);

        logger.LogInformation("Product unit {ProductUnitId} deleted by user {User}.", id, currentUser);

        return true;
    }

    // Product Bundle Item management operations

}
