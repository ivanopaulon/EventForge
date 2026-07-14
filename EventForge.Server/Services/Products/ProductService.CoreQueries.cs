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
    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationParameters pagination, string? searchTerm = null, Guid? classificationNodeId = null, bool includeInactive = false, string? quickFilter = null, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        // When includeInactive is true, show all non-deleted products (including IsActive=false).
        // Management pages use this to allow admins to see and reactivate inactive products.
        var query = includeInactive
            ? context.Products.AsNoTracking().Where(p => !p.IsDeleted && p.TenantId == currentTenantId.Value)
            : context.Products.AsNoTracking().WhereActiveTenant(currentTenantId.Value);

        // Apply search filter if provided (before includes to avoid type conversion issues)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.Code.ToLower().Contains(lowerSearchTerm) ||
                p.Name.ToLower().Contains(lowerSearchTerm) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(lowerSearchTerm)) ||
                (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                p.Codes.Any(c => !c.IsDeleted && c.Code.ToLower().Contains(lowerSearchTerm)));
        }

        // Apply classification node filter (including descendants)
        if (classificationNodeId.HasValue)
        {
            // Collect all descendant IDs in a single query set (works for moderate-sized trees)
            var descendantIds = await GetDescendantNodeIdsAsync(classificationNodeId.Value, cancellationToken);
            descendantIds.Add(classificationNodeId.Value);

            query = query.Where(p =>
                (p.CategoryNodeId.HasValue && descendantIds.Contains(p.CategoryNodeId.Value)) ||
                (p.FamilyNodeId.HasValue && descendantIds.Contains(p.FamilyNodeId.Value)) ||
                (p.GroupNodeId.HasValue && descendantIds.Contains(p.GroupNodeId.Value)));
        }

        // Apply quick filter if provided (management page chip filters mapped to server-side predicates).
        if (!string.IsNullOrWhiteSpace(quickFilter))
        {
            query = quickFilter switch
            {
                "active" => query.Where(p => p.IsActive),
                "inactive" => query.Where(p => !p.IsActive),
                "bundle" => query.Where(p => p.IsBundle),
                "simple" => query.Where(p => !p.IsBundle),
                "with_images" => query.Where(p => p.ImageDocumentId != null),
                "recent" => query.Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                _ => query
            };
        }

        // For the paginated list view, navigation collections (Codes, Units, BundleItems, ImageDocument)
        // are not shown in the grid and are skipped here to avoid 4 extra JOINs per request.
        // CountX fields in the DTO will be 0; full collections are loaded in GetProductDetailAsync.
        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .OrderBy(p => p.Name)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = products.Select(MapToProductDto);

        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Returns all descendant node IDs for a given parent node (recursive, in-memory).
    /// </summary>
    private async Task<HashSet<Guid>> GetDescendantNodeIdsAsync(Guid parentId, CancellationToken cancellationToken)
    {
        // Use a request-scoped cache to avoid re-fetching the full tree on subsequent calls.
        if (_classificationNodesCache == null)
        {
            var raw = await context.ClassificationNodes
                .AsNoTracking()
                .Where(cn => !cn.IsDeleted)
                .Select(cn => new { cn.Id, cn.ParentId })
                .ToListAsync(cancellationToken);

            _classificationNodesCache = raw.Select(x => (x.Id, x.ParentId)).ToList();
        }

        var result = new HashSet<Guid>();
        var childLookup = _classificationNodesCache.ToLookup(n => n.ParentId);
        var queue = new Queue<Guid>();
        foreach (var child in childLookup[parentId])
            queue.Enqueue(child.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (result.Add(current))
            {
                foreach (var child in childLookup[current])
                    queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    public async Task<PagedResult<ProductDto>> GetProductsForPosCatalogAsync(PaginationParameters pagination, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var query = context.Products.AsNoTracking().WhereActiveTenant(currentTenantId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.Code.ToLower().Contains(lowerSearchTerm) ||
                p.Name.ToLower().Contains(lowerSearchTerm) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(lowerSearchTerm)) ||
                (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                p.Codes.Any(c => !c.IsDeleted && c.Code.ToLower().Contains(lowerSearchTerm)));
        }

        // Only include navigations needed by the POS grid — skip Codes, Units, BundleItems.
        query = query
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument);

        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .OrderBy(p => p.Name)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = products.Select(MapToPosCatalogDto);

        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id && p.TenantId == currentTenantId.Value && !p.IsDeleted)
            .Include(p => p.Codes.Where(c => !c.IsDeleted))
            .Include(p => p.Units.Where(u => !u.IsDeleted))
            .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
            .Include(p => p.ImageDocument)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null) return null;

        string? preferredSupplierName = null;
        if (product.PreferredSupplierId.HasValue)
        {
            preferredSupplierName = await context.BusinessParties
                .AsNoTracking()
                .Where(bp => bp.Id == product.PreferredSupplierId.Value && !bp.IsDeleted && bp.TenantId == currentTenantId.Value)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var dto = MapToProductDto(product);
        dto.PreferredSupplierName = preferredSupplierName;
        return dto;
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id && p.TenantId == currentTenantId.Value && !p.IsDeleted)
            .Include(p => p.Codes.Where(c => !c.IsDeleted))
            .Include(p => p.Units.Where(u => !u.IsDeleted))
            .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
            .Include(p => p.ImageDocument)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null) return null;

        string? preferredSupplierName = null;
        if (product.PreferredSupplierId.HasValue)
        {
            preferredSupplierName = await context.BusinessParties
                .AsNoTracking()
                .Where(bp => bp.Id == product.PreferredSupplierId.Value && !bp.IsDeleted && bp.TenantId == currentTenantId.Value)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var dto = MapToProductDetailDto(product);
        dto.PreferredSupplierName = preferredSupplierName;
        return dto;
    }

}
