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
    public async Task<IEnumerable<ProductBundleItemDto>> GetProductBundleItemsAsync(Guid bundleProductId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        var bundleItems = await context.ProductBundleItems
            .AsNoTracking()
            .Include(pbi => pbi.ComponentProduct)
            .Where(pbi => pbi.BundleProductId == bundleProductId && pbi.TenantId == currentTenantId.Value && !pbi.IsDeleted)
            .OrderBy(pbi => pbi.ComponentProduct!.Name)
            .ToListAsync(cancellationToken);

        return bundleItems.Select(MapToProductBundleItemDto);
    }

    public async Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        var bundleItem = await context.ProductBundleItems
            .AsNoTracking()
            .Include(pbi => pbi.ComponentProduct)
            .Where(pbi => pbi.Id == id && pbi.TenantId == currentTenantId.Value && !pbi.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return bundleItem is not null ? MapToProductBundleItemDto(bundleItem) : null;
    }

    public async Task<ProductBundleItemDto> AddProductBundleItemAsync(CreateProductBundleItemDto createProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createProductBundleItemDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        // Enforce self-reference guard
        if (createProductBundleItemDto.BundleProductId == createProductBundleItemDto.ComponentProductId)
            throw new ArgumentException("A product cannot be a component of itself.");

        // Check if bundle product exists and is actually a bundle.
        // Note: a bundle component may itself be a bundle — nested bundles (kits containing sub-kits)
        // are allowed by design. Only direct self-reference (A contains A) is blocked above.
        // Indirect circular references (A→B→A) are not detected here; callers must enforce
        // cycle-free hierarchies through UI constraints or a separate graph check if required.
        var bundleProduct = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == createProductBundleItemDto.BundleProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted)
            .Select(p => new { p.Id, p.IsBundle })
            .FirstOrDefaultAsync(cancellationToken);

        if (bundleProduct is null)
            throw new ArgumentException($"Bundle product with ID {createProductBundleItemDto.BundleProductId} does not exist.");

        if (!bundleProduct.IsBundle)
            throw new InvalidOperationException($"Product {createProductBundleItemDto.BundleProductId} is not a bundle product.");

        // Check if component product exists in the same tenant
        if (!await context.Products.AsNoTracking().AnyAsync(
            p => p.Id == createProductBundleItemDto.ComponentProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken))
        {
            throw new ArgumentException($"Component product with ID {createProductBundleItemDto.ComponentProductId} does not exist.");
        }

        // Check for duplicate component in the same bundle
        if (await context.ProductBundleItems.AsNoTracking().AnyAsync(pbi =>
                pbi.BundleProductId == createProductBundleItemDto.BundleProductId &&
                pbi.ComponentProductId == createProductBundleItemDto.ComponentProductId &&
                pbi.TenantId == currentTenantId.Value &&
                !pbi.IsDeleted,
                cancellationToken))
        {
            throw new InvalidOperationException("This component product is already part of the bundle.");
        }

        var bundleItem = new ProductBundleItem
        {
            TenantId = currentTenantId.Value,
            BundleProductId = createProductBundleItemDto.BundleProductId,
            ComponentProductId = createProductBundleItemDto.ComponentProductId,
            Quantity = createProductBundleItemDto.Quantity,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        _ = context.ProductBundleItems.Add(bundleItem);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the created bundle item
        _ = await auditLogService.TrackEntityChangesAsync(bundleItem, "Create", currentUser, null, cancellationToken);

        logger.LogInformation("Bundle item created with ID {BundleItemId} for bundle {BundleProductId} by user {User}.",
            bundleItem.Id, createProductBundleItemDto.BundleProductId, currentUser);

        // Reload with navigation property to populate ComponentProductName/Code in the DTO
        var savedItem = await context.ProductBundleItems
            .Include(bi => bi.ComponentProduct)
            .Where(bi => bi.Id == bundleItem.Id)
            .FirstAsync(cancellationToken);

        return MapToProductBundleItemDto(savedItem);
    }

    public async Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateProductBundleItemDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        var bundleItem = await context.ProductBundleItems
            .Where(pbi => pbi.Id == id && pbi.TenantId == currentTenantId.Value && !pbi.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (bundleItem is null)
        {
            logger.LogWarning("Bundle item with ID {BundleItemId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        // Check if component product exists in the same tenant
        if (!await context.Products.AsNoTracking().AnyAsync(
            p => p.Id == updateProductBundleItemDto.ComponentProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken))
        {
            throw new ArgumentException($"Component product with ID {updateProductBundleItemDto.ComponentProductId} does not exist.");
        }

        // Check for duplicate component in the same bundle (excluding the current item)
        if (updateProductBundleItemDto.ComponentProductId != bundleItem.ComponentProductId &&
            await context.ProductBundleItems.AsNoTracking().AnyAsync(pbi =>
                pbi.Id != id &&
                pbi.BundleProductId == bundleItem.BundleProductId &&
                pbi.ComponentProductId == updateProductBundleItemDto.ComponentProductId &&
                pbi.TenantId == currentTenantId.Value &&
                !pbi.IsDeleted,
                cancellationToken))
        {
            throw new InvalidOperationException("This component product is already part of the bundle.");
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the updated bundle item
        _ = await auditLogService.TrackEntityChangesAsync(bundleItem, "Update", currentUser, originalBundleItem, cancellationToken);

        logger.LogInformation("Bundle item {BundleItemId} updated by user {User}.", id, currentUser);

        // Reload with navigation property so ComponentProductName/Code are populated in the returned DTO
        var savedItem = await context.ProductBundleItems
            .AsNoTracking()
            .Include(bi => bi.ComponentProduct)
            .Where(bi => bi.Id == id)
            .FirstAsync(cancellationToken);

        return MapToProductBundleItemDto(savedItem);
    }

    public async Task<bool> RemoveProductBundleItemAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        var bundleItem = await context.ProductBundleItems
            .Where(pbi => pbi.Id == id && pbi.TenantId == currentTenantId.Value && !pbi.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (bundleItem is null)
        {
            logger.LogWarning("Bundle item with ID {BundleItemId} not found for deletion by user {User}.", id, currentUser);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log for the deleted bundle item
        _ = await auditLogService.TrackEntityChangesAsync(bundleItem, "Delete", currentUser, originalBundleItem, cancellationToken);

        logger.LogInformation("Bundle item {BundleItemId} deleted by user {User}.", id, currentUser);

        return true;
    }

}
