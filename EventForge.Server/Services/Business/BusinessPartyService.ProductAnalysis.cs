using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<PagedResult<BusinessPartyProductAnalysisDto>> GetBusinessPartyProductAnalysisAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? type = null,
        int? topN = null,
        PaginationParameters pagination = default!,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        // Build base query with all document rows for this business party
        var rowsQuery = context.DocumentRows
            .AsNoTracking()
            .Include(r => r.DocumentHeader)
                .ThenInclude(h => h!.DocumentType)
            .Include(r => r.Product)
            .Where(r => !r.IsDeleted &&
                       r.TenantId == currentTenantId.Value &&
                       r.DocumentHeader!.BusinessPartyId == businessPartyId &&
                       !r.DocumentHeader.IsDeleted &&
                       r.ProductId != null);

        // Apply date filters
        if (fromDate.HasValue)
        {
            rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.Date <= toDate.Value);
        }

        // Apply type filter (purchase/sale)
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (type.Equals("purchase", StringComparison.OrdinalIgnoreCase))
            {
                rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease);
            }
            else if (type.Equals("sale", StringComparison.OrdinalIgnoreCase))
            {
                rowsQuery = rowsQuery.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease);
            }
        }

        // Materialize filtered rows first (to avoid EF translation issues with complex calculations)
        var rows = await rowsQuery.ToListAsync(cancellationToken);

        // Group and aggregate in memory
        var grouped = rows
            .GroupBy(r => new { r.ProductId, r.Product!.Code, r.Product.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId!.Value,
                ProductCode = g.Key.Code,
                ProductName = g.Key.Name,
                // Purchase aggregations
                QuantityPurchased = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Sum(r => r.BaseQuantity ?? r.Quantity),
                ValuePurchased = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Sum(r => CalculateEffectiveLineTotal(r)),
                LastPurchaseDate = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Max(r => (DateTime?)r.DocumentHeader!.Date),
                // Sale aggregations
                QuantitySold = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Sum(r => r.BaseQuantity ?? r.Quantity),
                ValueSold = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Sum(r => CalculateEffectiveLineTotal(r)),
                LastSaleDate = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                    .Max(r => (DateTime?)r.DocumentHeader!.Date)
            })
            .ToList();

        // Calculate averages and create DTOs
        var analysisResults = grouped.Select(g => new BusinessPartyProductAnalysisDto
        {
            ProductId = g.ProductId,
            ProductCode = g.ProductCode,
            ProductName = g.ProductName,
            QuantityPurchased = g.QuantityPurchased,
            ValuePurchased = g.ValuePurchased,
            QuantitySold = g.QuantitySold,
            ValueSold = g.ValueSold,
            LastPurchaseDate = g.LastPurchaseDate,
            LastSaleDate = g.LastSaleDate,
            AvgPurchasePrice = g.QuantityPurchased > 0 ? g.ValuePurchased / g.QuantityPurchased : 0m,
            AvgSalePrice = g.QuantitySold > 0 ? g.ValueSold / g.QuantitySold : 0m
        }).ToList();

        // Apply sorting
        var sortByField = sortBy?.ToLowerInvariant() ?? "valuepurchased";
        analysisResults = sortByField switch
        {
            "valuesold" => sortDescending
                ? analysisResults.OrderByDescending(a => a.ValueSold).ToList()
                : analysisResults.OrderBy(a => a.ValueSold).ToList(),
            "quantitypurchased" => sortDescending
                ? analysisResults.OrderByDescending(a => a.QuantityPurchased).ToList()
                : analysisResults.OrderBy(a => a.QuantityPurchased).ToList(),
            "quantitysold" => sortDescending
                ? analysisResults.OrderByDescending(a => a.QuantitySold).ToList()
                : analysisResults.OrderBy(a => a.QuantitySold).ToList(),
            "productname" => sortDescending
                ? analysisResults.OrderByDescending(a => a.ProductName).ToList()
                : analysisResults.OrderBy(a => a.ProductName).ToList(),
            _ => sortDescending
                ? analysisResults.OrderByDescending(a => a.ValuePurchased).ToList()
                : analysisResults.OrderBy(a => a.ValuePurchased).ToList()
        };

        // Apply topN filter if specified
        if (topN.HasValue && topN.Value > 0)
        {
            analysisResults = analysisResults.Take(topN.Value).ToList();
        }

        var totalCount = analysisResults.Count;

        // Apply pagination
        var pagedResults = analysisResults
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToList();

        return new PagedResult<BusinessPartyProductAnalysisDto>
        {
            Items = pagedResults,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Calculates the effective line total (quantity * effective unit price after discounts).
    /// Implements the same logic as PriceTrend for consistency.
    /// </summary>
    private static decimal CalculateEffectiveLineTotal(DocumentRow row)
    {
        // Use normalized values (base unit if available)
        var unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        var weightQuantity = row.BaseQuantity ?? row.Quantity;

        // Calculate per-unit discount
        decimal unitDiscount;
        if (row.DiscountType == Prym.DTOs.Common.DiscountType.Percentage)
        {
            unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        }
        else
        {
            // Absolute discount: divide by quantity
            unitDiscount = row.Quantity > 0 ? row.LineDiscountValue / row.Quantity : 0m;
        }

        // Clamp discount to not exceed unit price
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);

        // Calculate effective unit price
        var effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Return total
        return effectiveUnitPrice * weightQuantity;
    }

}
