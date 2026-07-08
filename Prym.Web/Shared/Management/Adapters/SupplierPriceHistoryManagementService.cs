using Prym.DTOs.Common;
using Prym.DTOs.PriceHistory;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class SupplierPriceHistoryManagementService(
    ISupplierPriceHistoryService priceHistoryService,
    IBusinessPartyService businessPartyService,
    IProductService productService)
    : IEntityManagementService<PriceHistoryItem>
{
    /// <summary>Latest statistics computed during the last GetPagedAsync call.</summary>
    public PriceHistoryStatistics Statistics { get; private set; } = new();

    public async Task<PagedResult<PriceHistoryItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(filters);
        var supplierFilter = GetString(filters, "SupplierName");
        var productFilter = GetString(filters, "ProductName");

        var all = await LoadAllItemsAsync(supplierFilter, productFilter, request, ct);

        // Apply client-side text filters
        if (!string.IsNullOrWhiteSpace(supplierFilter))
            all = all.Where(x => x.SupplierName.Contains(supplierFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(productFilter))
            all = all.Where(x =>
                x.ProductName.Contains(productFilter, StringComparison.OrdinalIgnoreCase) ||
                (x.ProductCode?.Contains(productFilter, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(x =>
                x.ProductName.ToUpperInvariant().Contains(term) ||
                x.SupplierName.ToUpperInvariant().Contains(term) ||
                (x.ProductCode?.ToUpperInvariant().Contains(term) ?? false)).ToList();
        }

        all = all.OrderByDescending(x => x.ChangedAt).ToList();

        Statistics = BuildStatistics(all);

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<PriceHistoryItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotSupportedException("Price history records cannot be deleted.");

    // ─────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────

    private async Task<List<PriceHistoryItem>> LoadAllItemsAsync(
        string? supplierFilter,
        string? productFilter,
        PriceHistoryRequest request,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(supplierFilter))
        {
            var suppliers = (await businessPartyService.SearchBusinessPartiesAsync(supplierFilter, BusinessPartyType.Supplier, 100))
                .Select(x => x.Id).Distinct().ToList();
            return await LoadSupplierHistoryAsync(suppliers, request, ct);
        }

        if (!string.IsNullOrWhiteSpace(productFilter))
        {
            var productIds = await LoadMatchingProductIdsAsync(productFilter, ct);
            return await LoadProductHistoryAsync(productIds, request, ct);
        }

        var allSuppliers = (await businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Supplier))
            .Select(x => x.Id).Distinct().ToList();
        return await LoadSupplierHistoryAsync(allSuppliers, request, ct);
    }

    private async Task<List<PriceHistoryItem>> LoadSupplierHistoryAsync(
        IEnumerable<Guid> supplierIds,
        PriceHistoryRequest request,
        CancellationToken ct)
    {
        var items = new List<PriceHistoryItem>();
        foreach (var supplierId in supplierIds)
        {
            var page = 1;
            while (true)
            {
                request.Page = page;
                var response = await priceHistoryService.GetSupplierPriceHistoryAsync(supplierId, request);
                if (response?.Items is null || response.Items.Count == 0) break;
                items.AddRange(response.Items);
                if (page >= response.TotalPages) break;
                page++;
            }
        }
        return items.GroupBy(x => x.Id).Select(g => g.First()).ToList();
    }

    private async Task<List<PriceHistoryItem>> LoadProductHistoryAsync(
        IEnumerable<Guid> productIds,
        PriceHistoryRequest request,
        CancellationToken ct)
    {
        var items = new List<PriceHistoryItem>();
        foreach (var productId in productIds)
        {
            var page = 1;
            while (true)
            {
                request.Page = page;
                var response = await priceHistoryService.GetProductAllSuppliersPriceHistoryAsync(productId, request);
                if (response?.Items is null || response.Items.Count == 0) break;
                items.AddRange(response.Items);
                if (page >= response.TotalPages) break;
                page++;
            }
        }
        return items.GroupBy(x => x.Id).Select(g => g.First()).ToList();
    }

    private async Task<List<Guid>> LoadMatchingProductIdsAsync(string searchTerm, CancellationToken ct)
    {
        const int batchSize = 100;
        var page = 1;
        var ids = new List<Guid>();
        while (true)
        {
            var result = await productService.GetProductsAsync(page, batchSize, searchTerm);
            var resultItems = result?.Items?.ToList();
            if (resultItems is null || resultItems.Count == 0) break;
            ids.AddRange(resultItems.Select(x => x.Id));
            if (resultItems.Count < batchSize || page * batchSize >= (result?.TotalCount ?? 0)) break;
            page++;
        }
        return ids.Distinct().ToList();
    }

    private static PriceHistoryRequest BuildRequest(Dictionary<string, object?>? filters)
    {
        DateTime? from = null;
        DateTime? to = null;
        decimal? minChange = null;

        if (filters != null)
        {
            if (filters.TryGetValue("From", out var rawFrom) && rawFrom is DateTime f)
                from = f.Date;
            if (filters.TryGetValue("To", out var rawTo) && rawTo is DateTime t)
                to = t.Date.AddDays(1).AddTicks(-1);
            if (filters.TryGetValue("MinChangePercentage", out var rawMin) && rawMin is decimal m)
                minChange = m;
        }

        return new PriceHistoryRequest
        {
            FromDate = from,
            ToDate = to,
            MinChangePercentage = minChange,
            Page = 1,
            PageSize = 100,
            SortBy = "ChangedAt",
            SortDirection = "Desc"
        };
    }

    private static string? GetString(Dictionary<string, object?>? filters, string key)
    {
        if (filters == null) return null;
        return filters.TryGetValue(key, out var v) && v is string s && !string.IsNullOrWhiteSpace(s) ? s : null;
    }

    private static PriceHistoryStatistics BuildStatistics(IReadOnlyCollection<PriceHistoryItem> items)
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
            LastChangeDate = items.Max(x => x.ChangedAt),
            TotalIncreases = increases.Count,
            TotalDecreases = decreases.Count
        };
    }
}
