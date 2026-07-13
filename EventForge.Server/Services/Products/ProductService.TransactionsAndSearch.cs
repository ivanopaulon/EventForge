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
    public async Task<IEnumerable<RecentProductTransactionDto>> GetRecentProductTransactionsAsync(
        Guid productId,
        string type = "purchase",
        Guid? partyId = null,
        int top = 3,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        // Determine if we're looking for purchases (stock increase) or sales (stock decrease)
        bool isStockIncrease = type.Equals("purchase", StringComparison.OrdinalIgnoreCase);

        // Query document rows with all necessary joins
        var query = context.DocumentRows
            .AsNoTracking()
            .Where(r => r.ProductId == productId &&
                        !r.IsDeleted &&
                        r.TenantId == currentTenantId.Value)
            .Include(r => r.DocumentHeader)
                .ThenInclude(h => h!.DocumentType)
            .Include(r => r.DocumentHeader)
                .ThenInclude(h => h!.BusinessParty)
            // Include both Archived and Active documents: an Active document that has not been
            // closed yet still represents a real purchase/sale and must appear in price history.
            .Where(r => r.DocumentHeader != null &&
                        !r.DocumentHeader.IsDeleted &&
                        (r.DocumentHeader.Status == DocumentStatus.Archived ||
                         r.DocumentHeader.Status == DocumentStatus.Active) &&
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
            if (row.DiscountType == Prym.DTOs.Common.DiscountType.Percentage)
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
                Discount = row.DiscountType == Prym.DTOs.Common.DiscountType.Percentage
                    ? row.LineDiscount
                    : row.LineDiscountValue
            };
        }).ToList();

        return transactions;
    }

    public async Task<ProductSearchResultDto> SearchProductsAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
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

        // Step 1: Try exact match on ProductCodes.Code (case-insensitive).
        // Use only !IsDeleted + TenantId filter (not WhereActiveTenant) so that
        // ProductCodes with IsActive=false are still reachable via exact scan —
        // this mirrors the behaviour of GetProductWithCodeByCodeAsync used for barcode ENTER.
        var productCode = await context.ProductCodes
            .AsNoTracking()
            .Where(pc => !pc.IsDeleted && pc.TenantId == currentTenantId.Value)
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.Brand)
            .Include(pc => pc.Product)
                .ThenInclude(p => p!.VatRate)
            .Include(pc => pc.ProductUnit)
                .ThenInclude(pu => pu!.UnitOfMeasure)
            .FirstOrDefaultAsync(pc => pc.Code.ToLower() == queryTrimmed.ToLower(), cancellationToken);

        if (productCode?.Product is not null && !productCode.Product.IsDeleted)
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

        // Step 2: Try exact match on Product.Code (case-insensitive).
        // Also relaxed to include inactive products (IsActive=false / Status != Active)
        // so the UI can show a warning instead of silently returning no results.
        var productByCode = await context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.TenantId == currentTenantId.Value)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .FirstOrDefaultAsync(p => p.Code != null && p.Code.ToLower() == queryTrimmed.ToLower(), cancellationToken);

        if (productByCode is not null)
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

        // Step 3: Text search in Name, ShortDescription, Description, Brand.Name, and alias codes (case-insensitive, multi-word AND logic)
        var searchWords = queryTrimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var textQuery = context.Products
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Where(p => p.Status == EntityProductStatus.Active);

        foreach (var word in searchWords)
        {
            var w = word;
            textQuery = textQuery.Where(p =>
                EF.Functions.Like(p.Name, $"%{w}%") ||
                (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, $"%{w}%")) ||
                (p.Description != null && EF.Functions.Like(p.Description, $"%{w}%")) ||
                (p.Brand != null && EF.Functions.Like(p.Brand.Name, $"%{w}%")) ||
                p.Codes.Any(c => !c.IsDeleted && EF.Functions.Like(c.Code, $"%{w}%")));
        }

        var searchResults = await textQuery
            .OrderBy(p => p.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        result.SearchResults = searchResults.Select(MapToProductDto).ToList();
        result.TotalCount = result.SearchResults.Count;

        return result;
    }

}
