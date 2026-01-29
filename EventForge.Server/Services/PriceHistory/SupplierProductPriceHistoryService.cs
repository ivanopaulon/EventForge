using EventForge.DTOs.Common;
using EventForge.DTOs.PriceHistory;
using EventForge.Server.Services.Alerts;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.PriceHistory;

/// <summary>
/// Service implementation for managing supplier product price history.
/// </summary>
public class SupplierProductPriceHistoryService : ISupplierProductPriceHistoryService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SupplierProductPriceHistoryService> _logger;
    private readonly Lazy<ISupplierPriceAlertService>? _alertService;

    public SupplierProductPriceHistoryService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        ILogger<SupplierProductPriceHistoryService> logger,
        Lazy<ISupplierPriceAlertService>? alertService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService;
    }

    /// <inheritdoc/>
    public async Task<Guid> LogPriceChangeAsync(PriceChangeLogRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            // Calculate price changes
            var priceChange = request.NewPrice - request.OldPrice;
            var priceChangePercentage = request.OldPrice != 0
                ? (priceChange / request.OldPrice) * 100
                : 0;

            var priceHistory = new SupplierProductPriceHistory
            {
                Id = Guid.NewGuid(),
                ProductSupplierId = request.ProductSupplierId,
                SupplierId = request.SupplierId,
                ProductId = request.ProductId,
                OldUnitCost = request.OldPrice,
                NewUnitCost = request.NewPrice,
                PriceChange = priceChange,
                PriceChangePercentage = priceChangePercentage,
                Currency = request.Currency,
                OldLeadTimeDays = request.OldLeadTimeDays,
                NewLeadTimeDays = request.NewLeadTimeDays,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = request.UserId,
                ChangeSource = request.ChangeSource,
                ChangeReason = request.ChangeReason,
                Notes = request.Notes,
                TenantId = currentTenantId.Value,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupplierProductPriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price history logged for ProductSupplier {ProductSupplierId}: {OldPrice} -> {NewPrice} ({ChangePercentage}%)",
                request.ProductSupplierId,
                request.OldPrice,
                request.NewPrice,
                priceChangePercentage);

            // Generate alerts for price change (FASE 5 integration)
            if (_alertService != null)
            {
                try
                {
                    await _alertService.Value.GenerateAlertsForPriceChangeAsync(
                        request.ProductId,
                        request.SupplierId,
                        request.OldPrice,
                        request.NewPrice,
                        cancellationToken);
                }
                catch (Exception alertEx)
                {
                    _logger.LogWarning(alertEx, "Failed to generate price change alerts for product {ProductId}", request.ProductId);
                    // Don't throw - alerts are not critical to price history logging
                }
            }

            return priceHistory.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging price change for ProductSupplier {ProductSupplierId}", request.ProductSupplierId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> LogBulkPriceChangesAsync(List<PriceChangeLogRequest> requests, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var priceHistories = new List<SupplierProductPriceHistory>();
            var now = DateTime.UtcNow;

            foreach (var request in requests)
            {
                var priceChange = request.NewPrice - request.OldPrice;
                var priceChangePercentage = request.OldPrice != 0
                    ? (priceChange / request.OldPrice) * 100
                    : 0;

                var priceHistory = new SupplierProductPriceHistory
                {
                    Id = Guid.NewGuid(),
                    ProductSupplierId = request.ProductSupplierId,
                    SupplierId = request.SupplierId,
                    ProductId = request.ProductId,
                    OldUnitCost = request.OldPrice,
                    NewUnitCost = request.NewPrice,
                    PriceChange = priceChange,
                    PriceChangePercentage = priceChangePercentage,
                    Currency = request.Currency,
                    OldLeadTimeDays = request.OldLeadTimeDays,
                    NewLeadTimeDays = request.NewLeadTimeDays,
                    ChangedAt = now,
                    ChangedByUserId = request.UserId,
                    ChangeSource = request.ChangeSource,
                    ChangeReason = request.ChangeReason,
                    Notes = request.Notes,
                    TenantId = currentTenantId.Value,
                    IsDeleted = false,
                    CreatedAt = now
                };

                priceHistories.Add(priceHistory);
            }

            _context.SupplierProductPriceHistories.AddRange(priceHistories);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk logged {Count} price changes", priceHistories.Count);

            return priceHistories.Select(ph => ph.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk logging price changes");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetProductPriceHistoryAsync(
        Guid supplierId,
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var query = _context.SupplierProductPriceHistories
                .WhereActiveTenant(currentTenantId.Value)
                .Where(h => h.SupplierId == supplierId && h.ProductId == productId);

            query = ApplyFilters(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, request);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(h => h.Product)
                .Include(h => h.Supplier)
                .Include(h => h.ChangedByUser)
                .ToListAsync(cancellationToken);

            var historyItems = items.Select(MapToPriceHistoryItem).ToList();

            var statistics = await CalculateStatisticsAsync(
                _context.SupplierProductPriceHistories
                    .WhereActiveTenant(currentTenantId.Value)
                    .Where(h => h.SupplierId == supplierId && h.ProductId == productId),
                cancellationToken);

            return new PriceHistoryResponse
            {
                Items = historyItems,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetSupplierPriceHistoryAsync(
        Guid supplierId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var query = _context.SupplierProductPriceHistories
                .WhereActiveTenant(currentTenantId.Value)
                .Where(h => h.SupplierId == supplierId);

            query = ApplyFilters(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, request);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(h => h.Product)
                .Include(h => h.Supplier)
                .Include(h => h.ChangedByUser)
                .ToListAsync(cancellationToken);

            var historyItems = items.Select(MapToPriceHistoryItem).ToList();

            var statistics = await CalculateStatisticsAsync(
                _context.SupplierProductPriceHistories
                    .WhereActiveTenant(currentTenantId.Value)
                    .Where(h => h.SupplierId == supplierId),
                cancellationToken);

            return new PriceHistoryResponse
            {
                Items = historyItems,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Supplier {SupplierId}", supplierId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryResponse> GetProductAllSuppliersPriceHistoryAsync(
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var query = _context.SupplierProductPriceHistories
                .WhereActiveTenant(currentTenantId.Value)
                .Where(h => h.ProductId == productId);

            query = ApplyFilters(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, request);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(h => h.Product)
                .Include(h => h.Supplier)
                .Include(h => h.ChangedByUser)
                .ToListAsync(cancellationToken);

            var historyItems = items.Select(MapToPriceHistoryItem).ToList();

            var statistics = await CalculateStatisticsAsync(
                _context.SupplierProductPriceHistories
                    .WhereActiveTenant(currentTenantId.Value)
                    .Where(h => h.ProductId == productId),
                cancellationToken);

            return new PriceHistoryResponse
            {
                Items = historyItems,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Product {ProductId}", productId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PriceHistoryStatistics> GetPriceHistoryStatisticsAsync(
        Guid supplierId,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var query = _context.SupplierProductPriceHistories
                .WhereActiveTenant(currentTenantId.Value)
                .Where(h => h.SupplierId == supplierId);

            if (productId.HasValue)
            {
                query = query.Where(h => h.ProductId == productId.Value);
            }

            return await CalculateStatisticsAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price history statistics for Supplier {SupplierId}", supplierId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PriceTrendDataPoint>> GetPriceTrendDataAsync(
        Guid supplierId,
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for price history operations.");
            }

            var trendData = await _context.SupplierProductPriceHistories
                .WhereActiveTenant(currentTenantId.Value)
                .Where(h => h.SupplierId == supplierId && h.ProductId == productId)
                .Where(h => h.ChangedAt >= fromDate && h.ChangedAt <= toDate)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new PriceTrendDataPoint
                {
                    Date = h.ChangedAt,
                    Price = h.NewUnitCost,
                    ChangeSource = h.ChangeSource,
                    Currency = h.Currency
                })
                .ToListAsync(cancellationToken);

            return trendData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price trend data for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            throw;
        }
    }

    #region Private Helper Methods

    private static IQueryable<SupplierProductPriceHistory> ApplyFilters(
        IQueryable<SupplierProductPriceHistory> query,
        PriceHistoryRequest request)
    {
        if (request.FromDate.HasValue)
        {
            query = query.Where(h => h.ChangedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(h => h.ChangedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ChangeSource))
        {
            query = query.Where(h => h.ChangeSource == request.ChangeSource);
        }

        if (request.MinChangePercentage.HasValue)
        {
            var minChange = request.MinChangePercentage.Value;
            query = query.Where(h => Math.Abs(h.PriceChangePercentage) >= minChange);
        }

        return query;
    }

    private static IQueryable<SupplierProductPriceHistory> ApplySorting(
        IQueryable<SupplierProductPriceHistory> query,
        PriceHistoryRequest request)
    {
        var isDescending = request.SortDirection.Equals("Desc", StringComparison.OrdinalIgnoreCase);

        query = request.SortBy.ToLowerInvariant() switch
        {
            "pricechange" => isDescending
                ? query.OrderByDescending(h => h.PriceChange)
                : query.OrderBy(h => h.PriceChange),
            "pricechangepercentage" => isDescending
                ? query.OrderByDescending(h => h.PriceChangePercentage)
                : query.OrderBy(h => h.PriceChangePercentage),
            "changedat" or _ => isDescending
                ? query.OrderByDescending(h => h.ChangedAt)
                : query.OrderBy(h => h.ChangedAt)
        };

        return query;
    }

    private static PriceHistoryItem MapToPriceHistoryItem(SupplierProductPriceHistory history)
    {
        return new PriceHistoryItem
        {
            Id = history.Id,
            ProductName = history.Product?.Name ?? "Unknown",
            ProductCode = history.Product?.Code ?? null,
            SupplierName = history.Supplier?.Name ?? "Unknown",
            OldPrice = history.OldUnitCost,
            NewPrice = history.NewUnitCost,
            PriceChange = history.PriceChange,
            PriceChangePercentage = history.PriceChangePercentage,
            Currency = history.Currency,
            ChangedAt = history.ChangedAt,
            ChangedByUserName = history.ChangedByUser != null
                ? $"{history.ChangedByUser.FirstName} {history.ChangedByUser.LastName}".Trim()
                : "Unknown",
            ChangeSource = history.ChangeSource,
            ChangeReason = history.ChangeReason,
            OldLeadTimeDays = history.OldLeadTimeDays,
            NewLeadTimeDays = history.NewLeadTimeDays,
            Notes = history.Notes
        };
    }

    private static async Task<PriceHistoryStatistics> CalculateStatisticsAsync(
        IQueryable<SupplierProductPriceHistory> query,
        CancellationToken cancellationToken)
    {
        if (!await query.AnyAsync(cancellationToken))
        {
            return new PriceHistoryStatistics
            {
                AveragePriceChange = 0,
                MaxPriceIncrease = 0,
                MaxPriceDecrease = 0,
                TotalChanges = 0,
                LastChangeDate = null,
                AverageAbsolutePriceChange = 0,
                TotalIncreases = 0,
                TotalDecreases = 0
            };
        }

        var statistics = await query
            .GroupBy(h => 1)
            .Select(g => new
            {
                AveragePriceChange = g.Average(h => h.PriceChangePercentage),
                MaxPriceIncrease = g.Max(h => h.PriceChangePercentage),
                MaxPriceDecrease = g.Min(h => h.PriceChangePercentage),
                TotalChanges = g.Count(),
                LastChangeDate = g.Max(h => h.ChangedAt),
                AverageAbsolutePriceChange = g.Average(h => h.PriceChange),
                TotalIncreases = g.Count(h => h.PriceChange > 0),
                TotalDecreases = g.Count(h => h.PriceChange < 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new PriceHistoryStatistics
        {
            AveragePriceChange = statistics?.AveragePriceChange ?? 0,
            MaxPriceIncrease = statistics?.MaxPriceIncrease ?? 0,
            MaxPriceDecrease = statistics?.MaxPriceDecrease ?? 0,
            TotalChanges = statistics?.TotalChanges ?? 0,
            LastChangeDate = statistics?.LastChangeDate,
            AverageAbsolutePriceChange = statistics?.AverageAbsolutePriceChange ?? 0,
            TotalIncreases = statistics?.TotalIncreases ?? 0,
            TotalDecreases = statistics?.TotalDecreases ?? 0
        };
    }

    #endregion
}
