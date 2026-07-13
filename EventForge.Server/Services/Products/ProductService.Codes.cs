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
    public async Task<IEnumerable<ProductCodeDto>> GetProductCodesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var codes = await context.ProductCodes
            .AsNoTracking()
            .Where(pc => pc.ProductId == productId && !pc.IsDeleted)
            .OrderBy(pc => pc.CodeType)
            .ThenBy(pc => pc.Code)
            .ToListAsync(cancellationToken);

        return codes.Select(MapToProductCodeDto);
    }

    public async Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var code = await context.ProductCodes
            .AsNoTracking()
            .Where(pc => pc.Id == id && !pc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return code is not null ? MapToProductCodeDto(code) : null;
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string codeValue, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeValue);

        var productCode = await context.ProductCodes
            .AsNoTracking()
            .Where(pc => pc.Code == codeValue && !pc.IsDeleted)
            .Include(pc => pc.Product)
            .FirstOrDefaultAsync(cancellationToken);

        if (productCode?.Product is null || productCode.Product.IsDeleted)
        {
            return null;
        }

        return MapToProductDto(productCode.Product);
    }

    public async Task<ProductWithCodeDto?> GetProductWithCodeByCodeAsync(string codeValue, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeValue);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var productCode = await context.ProductCodes
            .AsNoTracking()
            .Where(pc => pc.Code == codeValue && !pc.IsDeleted && pc.TenantId == currentTenantId.Value)
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.VatRate)       // ✅ Include VatRate for continuous scan
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.UnitOfMeasure) // ✅ Include UnitOfMeasure for continuous scan
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.Brand)         // Existing include
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.ImageDocument) // Include image document for thumbnails
            .FirstOrDefaultAsync(cancellationToken);

        if (productCode?.Product is null || productCode.Product.IsDeleted)
        {
            return null;
        }

        return new ProductWithCodeDto
        {
            Product = MapToProductDto(productCode.Product),
            Code = MapToProductCodeDto(productCode)
        };
    }

    public async Task<ProductCodeDto> AddProductCodeAsync(CreateProductCodeDto createProductCodeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createProductCodeDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        // Validate tenant context
        var currentTenantId = tenantContext.CurrentTenantId;
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

        _ = context.ProductCodes.Add(productCode);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the created product code
        _ = await auditLogService.TrackEntityChangesAsync(productCode, "Create", currentUser, null, cancellationToken);

        logger.LogInformation("Product code created with ID {ProductCodeId} for product {ProductId} by user {User}.",
            productCode.Id, createProductCodeDto.ProductId, currentUser);

        return MapToProductCodeDto(productCode);
    }

    public async Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateProductCodeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateProductCodeDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for product operations.");

        var productCode = await context.ProductCodes
            .Where(pc => pc.Id == id && pc.TenantId == currentTenantId && !pc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (productCode is null)
        {
            logger.LogWarning("Product code with ID {ProductCodeId} not found for update by user {User}.", id, currentUser);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the updated product code
        _ = await auditLogService.TrackEntityChangesAsync(productCode, "Update", currentUser, originalProductCode, cancellationToken);

        logger.LogInformation("Product code {ProductCodeId} updated by user {User}.", id, currentUser);

        return MapToProductCodeDto(productCode);
    }

    public async Task<bool> RemoveProductCodeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for product operations.");

        var productCode = await context.ProductCodes
            .Where(pc => pc.Id == id && pc.TenantId == currentTenantId && !pc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (productCode is null)
        {
            logger.LogWarning("Product code with ID {ProductCodeId} not found for deletion by user {User}.", id, currentUser);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the deleted product code
        _ = await auditLogService.TrackEntityChangesAsync(productCode, "Delete", currentUser, originalProductCode, cancellationToken);

        logger.LogInformation("Product code {ProductCodeId} deleted by user {User}.", id, currentUser);

        return true;
    }

    // Product Unit management operations

}
