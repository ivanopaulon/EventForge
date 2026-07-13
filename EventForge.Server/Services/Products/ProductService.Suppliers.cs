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
    public async Task<IEnumerable<ProductSupplierDto>> GetProductSuppliersAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        var suppliers = await context.ProductSuppliers
            .AsNoTracking()
            .Where(ps => ps.ProductId == productId && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
            .Include(ps => ps.Supplier)
            .Include(ps => ps.Product)
            .OrderByDescending(ps => ps.Preferred)
            .ThenBy(ps => ps.Supplier!.Name)
            .ToListAsync(cancellationToken);

        return suppliers.Select(MapToProductSupplierDto);
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        var supplier = await context.ProductSuppliers
            .AsNoTracking()
            .Where(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value)
            .Include(ps => ps.Supplier)
            .Include(ps => ps.Product)
            .FirstOrDefaultAsync(cancellationToken);

        return supplier is not null ? MapToProductSupplierDto(supplier) : null;
    }

    public async Task<ProductSupplierDto> AddProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        // Validate product exists
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == createProductSupplierDto.ProductId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {createProductSupplierDto.ProductId} not found.");
        }

        // Validate bundle products cannot have suppliers
        if (product.IsBundle)
        {
            throw new InvalidOperationException("Bundle products cannot have suppliers.");
        }

        // Validate supplier exists and is a supplier type
        var supplier = await context.BusinessParties
            .AsNoTracking()
            .FirstOrDefaultAsync(bp => bp.Id == createProductSupplierDto.SupplierId && !bp.IsDeleted && bp.TenantId == currentTenantId.Value, cancellationToken);

        if (supplier is null)
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
            var existingPreferred = await context.ProductSuppliers
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

        _ = context.ProductSuppliers.Add(productSupplier);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
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
        await context.Entry(productSupplier).Reference(ps => ps.Supplier).LoadAsync(cancellationToken);
        await context.Entry(productSupplier).Reference(ps => ps.Product).LoadAsync(cancellationToken);

        return MapToProductSupplierDto(productSupplier);
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        var productSupplier = await context.ProductSuppliers
            .Include(ps => ps.Product)
            .Include(ps => ps.Supplier)
            .FirstOrDefaultAsync(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value, cancellationToken);

        if (productSupplier is null)
        {
            return null;
        }

        // Validate product exists if changed
        if (productSupplier.ProductId != updateProductSupplierDto.ProductId)
        {
            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == updateProductSupplierDto.ProductId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

            if (product is null)
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
            var supplier = await context.BusinessParties
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == updateProductSupplierDto.SupplierId && !bp.IsDeleted && bp.TenantId == currentTenantId.Value, cancellationToken);

            if (supplier is null)
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
            var existingPreferred = await context.ProductSuppliers
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

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
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

    public async Task<bool> RemoveProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        var productSupplier = await context.ProductSuppliers
            .FirstOrDefaultAsync(ps => ps.Id == id && !ps.IsDeleted && ps.TenantId == currentTenantId.Value, cancellationToken);

        if (productSupplier is null)
        {
            return false;
        }

        productSupplier.IsDeleted = true;
        productSupplier.ModifiedBy = currentUser;
        productSupplier.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
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
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        // Get all products (preserve previous behaviour: products may be global)
        var products = await context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        // Get all existing associations for this supplier within the current tenant
        var associations = await context.ProductSuppliers
            .AsNoTracking()
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
                ShortDescription = p.ShortDescription,
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

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }

        // Get existing associations for this supplier within the tenant
        var existingAssociations = await context.ProductSuppliers
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
            context.ProductSuppliers.Add(newAssociation);
        }

        // Soft delete removed associations (already scoped to tenant)
        foreach (var association in associationsToRemove)
        {
            association.IsDeleted = true;
            association.ModifiedAt = now;
            association.ModifiedBy = currentUser;
        }

        await context.SaveChangesAsync(cancellationToken);

        return productIdsToAdd.Count;
    }

    public async Task<PagedResult<ProductSupplierDto>> GetProductsBySupplierAsync(
        Guid supplierId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product supplier operations.");
        }
        // Query product suppliers for this supplier
        var query = context.ProductSuppliers
            .AsNoTracking()
            .Where(ps => ps.SupplierId == supplierId &&
                        !ps.IsDeleted &&
                        ps.TenantId == currentTenantId.Value)
            .Include(ps => ps.Product)
            .Include(ps => ps.Supplier)
            .OrderByDescending(ps => ps.Preferred)
            .ThenBy(ps => ps.Product!.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var productSuppliers = await query
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Get product IDs to fetch latest purchase data
        var productIds = productSuppliers.Select(ps => ps.ProductId).ToList();

        // Get latest purchase prices from archived document rows
        var latestPurchases = await context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.ProductId.HasValue &&
                        productIds.Contains(dr.ProductId.Value) &&
                        !dr.IsDeleted &&
                        dr.TenantId == currentTenantId.Value)
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Where(dr => dr.DocumentHeader != null &&
                        !dr.DocumentHeader.IsDeleted &&
                        dr.DocumentHeader.BusinessPartyId == supplierId &&
                        dr.DocumentHeader.Status == DocumentStatus.Archived &&
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
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

}
