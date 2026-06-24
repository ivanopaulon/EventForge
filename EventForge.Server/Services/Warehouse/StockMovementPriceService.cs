using EventForge.Server.Data.Entities.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Common;
using Prym.DTOs.PriceHistory;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service that derives supplier purchase price history directly from inbound stock movements.
/// Each <see cref="StockMovement"/> with <see cref="StockMovementReason.Purchase"/> and a non-null
/// <see cref="StockMovement.UnitCost"/> is a price data point. The "price change" between consecutive
/// movements for the same product+supplier is computed on the fly.
/// </summary>
public class StockMovementPriceService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    ILogger<StockMovementPriceService> logger) : IStockMovementPriceService
{
    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetProductPriceHistoryAsync(
        Guid supplierId,
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        logger.LogDebug("Retrieving price history for supplier {SupplierId}, product {ProductId}.", supplierId, productId);

        var query = BuildBaseQuery(tenantId)
            .Where(m => m.BusinessPartyId == supplierId && m.ProductId == productId);

        query = ApplyDateFilter(query, request.FromDate, request.ToDate);

        var movements = await query
            .Include(m => m.Product)
            .Include(m => m.BusinessParty)
            .Include(m => m.DocumentHeader)
            .OrderBy(m => m.MovementDate)
            .ToListAsync(cancellationToken);

        var items = BuildPriceHistoryItems(movements);
        items = ApplyMinChangeFilter(items, request.MinChangePercentage);

        return BuildPagedResponse(items, request);
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetSupplierPriceHistoryAsync(
        Guid supplierId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        logger.LogDebug("Retrieving price history for supplier {SupplierId}.", supplierId);

        var query = BuildBaseQuery(tenantId)
            .Where(m => m.BusinessPartyId == supplierId);

        query = ApplyDateFilter(query, request.FromDate, request.ToDate);

        var movements = await query
            .Include(m => m.Product)
            .Include(m => m.BusinessParty)
            .Include(m => m.DocumentHeader)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.MovementDate)
            .ToListAsync(cancellationToken);

        var items = BuildPriceHistoryItems(movements);
        items = ApplyMinChangeFilter(items, request.MinChangePercentage);
        items = items.OrderByDescending(x => x.ChangedAt).ToList();

        return BuildPagedResponse(items, request);
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetProductAllSuppliersPriceHistoryAsync(
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = BuildBaseQuery(tenantId)
            .Where(m => m.ProductId == productId && m.BusinessPartyId.HasValue);

        query = ApplyDateFilter(query, request.FromDate, request.ToDate);

        var movements = await query
            .Include(m => m.Product)
            .Include(m => m.BusinessParty)
            .Include(m => m.DocumentHeader)
            .OrderBy(m => m.BusinessPartyId)
            .ThenBy(m => m.MovementDate)
            .ToListAsync(cancellationToken);

        var items = BuildPriceHistoryItems(movements);
        items = ApplyMinChangeFilter(items, request.MinChangePercentage);
        items = items.OrderByDescending(x => x.ChangedAt).ToList();

        return BuildPagedResponse(items, request);
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryStatistics> GetPriceHistoryStatisticsAsync(
        Guid supplierId,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = BuildBaseQuery(tenantId)
            .Where(m => m.BusinessPartyId == supplierId);

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        var movements = await query
            .Include(m => m.Product)
            .Include(m => m.BusinessParty)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.MovementDate)
            .ToListAsync(cancellationToken);

        var items = BuildPriceHistoryItems(movements);
        return ComputeStatistics(items);
    }

    /// <inheritdoc/>
    public async Task<List<PriceTrendDataPoint>> GetPriceTrendDataAsync(
        Guid supplierId,
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var movements = await BuildBaseQuery(tenantId)
            .Where(m => m.BusinessPartyId == supplierId
                     && m.ProductId == productId
                     && m.MovementDate >= fromDate
                     && m.MovementDate <= toDate)
            .Include(m => m.BusinessParty)
            .OrderBy(m => m.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => new PriceTrendDataPoint
        {
            Date = m.MovementDate,
            Price = m.UnitCost ?? 0,
            Quantity = m.Quantity,
            BusinessPartyName = m.BusinessParty?.Name,
            ChangeSource = "Purchase"
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private Guid GetTenantId()
    {
        return tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for price history operations.");
    }

    /// <summary>
    /// Returns the base IQueryable filtering purchase inbound movements with a unit cost.
    /// </summary>
    private IQueryable<StockMovement> BuildBaseQuery(Guid tenantId)
    {
        return context.StockMovements
            .AsNoTracking()
            .Where(m => !m.IsDeleted
                     && m.TenantId == tenantId
                     && m.Reason == StockMovementReason.Purchase
                     && m.UnitCost.HasValue
                     && m.UnitCost > 0);
    }

    private static IQueryable<StockMovement> ApplyDateFilter(
        IQueryable<StockMovement> query,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue) query = query.Where(m => m.MovementDate >= from.Value);
        if (to.HasValue) query = query.Where(m => m.MovementDate <= to.Value);
        return query;
    }

    /// <summary>
    /// Converts a chronologically-ordered list of movements into PriceHistoryItems,
    /// computing OldPrice / PriceChange by comparing consecutive movements for the
    /// same product+supplier pair.
    /// </summary>
    private static List<PriceHistoryItem> BuildPriceHistoryItems(IReadOnlyList<StockMovement> movements)
    {
        if (movements.Count == 0) return [];

        // Group movements by (ProductId, BusinessPartyId) to compute diffs within each group
        var groups = movements
            .GroupBy(m => (m.ProductId, m.BusinessPartyId));

        var result = new List<PriceHistoryItem>(movements.Count);

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(m => m.MovementDate).ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var m = ordered[i];
                var newPrice = m.UnitCost ?? 0;
                // First movement in the group has no prior price — OldPrice = 0 signals "initial entry".
                var oldPrice = i > 0 ? ordered[i - 1].UnitCost ?? 0m : 0m;
                var priceChange = newPrice - oldPrice;
                var priceChangePercentage = oldPrice != 0
                    ? (priceChange / oldPrice) * 100m
                    : 0m;

                result.Add(new PriceHistoryItem
                {
                    Id = m.Id,
                    ProductName = m.Product?.Name ?? string.Empty,
                    ProductCode = m.Product?.Code,
                    SupplierName = m.BusinessParty?.Name ?? string.Empty,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PriceChange = priceChange,
                    PriceChangePercentage = priceChangePercentage,
                    Currency = "EUR",
                    ChangedAt = m.MovementDate,
                    ChangedByUserName = m.UserId ?? string.Empty,
                    ChangeSource = m.DocumentHeader is not null
                        ? $"Doc. {m.DocumentHeader.Number ?? m.DocumentHeaderId.ToString()}"
                        : "Movimento",
                    ChangeReason = m.Notes,
                    Notes = m.Reference
                });
            }
        }

        return result;
    }

    private static List<PriceHistoryItem> ApplyMinChangeFilter(
        List<PriceHistoryItem> items,
        decimal? minChangePercentage)
    {
        if (!minChangePercentage.HasValue || minChangePercentage <= 0) return items;
        return items.Where(x => Math.Abs(x.PriceChangePercentage) >= minChangePercentage.Value).ToList();
    }

    private static PriceHistoryResponse BuildPagedResponse(List<PriceHistoryItem> items, PriceHistoryRequest request)
    {
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var paged = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PriceHistoryResponse
        {
            Items = paged,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = request.Page,
            PageSize = request.PageSize,
            Statistics = ComputeStatistics(items)
        };
    }

    private static PriceHistoryStatistics ComputeStatistics(IReadOnlyCollection<PriceHistoryItem> items)
    {
        if (items.Count == 0) return new PriceHistoryStatistics();

        var increases = items.Where(x => x.PriceChangePercentage > 0).ToList();
        var decreases = items.Where(x => x.PriceChangePercentage < 0).ToList();

        return new PriceHistoryStatistics
        {
            AveragePriceChange = items.Average(x => x.PriceChangePercentage),
            AverageAbsolutePriceChange = items.Average(x => Math.Abs(x.PriceChange)),
            MaxPriceIncrease = increases.Count > 0 ? increases.Max(x => x.PriceChangePercentage) : 0,
            MaxPriceDecrease = decreases.Count > 0 ? decreases.Min(x => x.PriceChangePercentage) : 0,
            TotalChanges = items.Count,
            LastChangeDate = items.Max(x => (DateTime?)x.ChangedAt),
            TotalIncreases = increases.Count,
            TotalDecreases = decreases.Count
        };
    }
}
