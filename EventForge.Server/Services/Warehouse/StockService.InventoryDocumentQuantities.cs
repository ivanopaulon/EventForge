using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<IEnumerable<StockSnapshotDto>> GetInventoryDocumentQuantitiesAsync(
        Guid documentHeaderId,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        // ── Step 1: Validate and load the inventory document header (scalar projection) ──────
        // Only archived inventory documents are authoritative. An active/draft document
        // represents an in-progress count and must not be used for stock valuation.
        var header = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.Id == documentHeaderId
                         && dh.TenantId == currentTenantId.Value
                         && !dh.IsDeleted
                         && dh.DocumentType != null
                         && dh.DocumentType.IsInventoryDocument
                         && (dh.Status == DocumentStatus.Archived))
            .Select(dh => new { dh.Id, dh.Date })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
            return Enumerable.Empty<StockSnapshotDto>();

        var documentDate = header.Date.Date;
        // The cutoff for movement cost look-up: include all inbounds up to end of document day.
        var movementCutoff = documentDate.AddDays(1);

        // ── Step 2: Load DocumentRows via projection ─────────────────────────────────────────
        // DocumentRows only have ProductId and LocationId — they do not track lots. One row
        // per (Product, Location) is the expected structure; if there are duplicates the
        // quantities are summed in-memory (group step below).
        var rowsQuery = context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.DocumentHeaderId == documentHeaderId
                         && dr.TenantId == currentTenantId.Value
                         && !dr.IsDeleted
                         && dr.ProductId.HasValue
                         && dr.LocationId.HasValue);

        // Push location/warehouse filters to the database to avoid loading irrelevant rows.
        if (locationId.HasValue)
        {
            rowsQuery = rowsQuery.Where(dr => dr.LocationId == locationId.Value);
        }
        else if (warehouseId.HasValue)
        {
            var wid = warehouseId.Value;
            rowsQuery = rowsQuery.Where(dr =>
                dr.Location != null && dr.Location.WarehouseId == wid);
        }

        var rawRows = await rowsQuery
            .Select(dr => new
            {
                ProductId = dr.ProductId!.Value,
                LocationId = dr.LocationId!.Value,
                dr.Quantity
            })
            .ToListAsync(cancellationToken);

        if (rawRows.Count == 0)
            return Enumerable.Empty<StockSnapshotDto>();

        // ── Step 3: Group by (ProductId, LocationId) and sum quantities ──────────────────────
        // Handles the rare case of multiple rows per (Product, Location) in one document.
        var grouped = rawRows
            .GroupBy(r => (r.ProductId, r.LocationId))
            .Select(g => (ProductId: g.Key.ProductId, LocationId: g.Key.LocationId, Quantity: g.Sum(r => r.Quantity)))
            .ToList();

        var relevantProductIds = grouped.Select(r => r.ProductId).Distinct().ToList();
        var relevantLocationIds = grouped.Select(r => r.LocationId).Distinct().ToList();

        // ── Step 4: Bulk-load Products and Locations ─────────────────────────────────────────
        var productLookup = await context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenantId.Value
                        && !p.IsDeleted
                        && relevantProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var locationLookup = await context.StorageLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => l.TenantId == currentTenantId.Value
                        && !l.IsDeleted
                        && relevantLocationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // ── Step 5: Resolve purchase unit cost per (Product, Location) ───────────────────────
        // Strategy: most recent Completed Inbound movement up to end of document day.
        // If no inbound has a cost, fall back to the current Stock.UnitCost.
        var lastCostLookup = (await context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == currentTenantId.Value
                         && !sm.IsDeleted
                         && sm.Status == MovementStatus.Completed
                         && sm.ToLocationId.HasValue
                         && relevantProductIds.Contains(sm.ProductId)
                         && relevantLocationIds.Contains(sm.ToLocationId!.Value)
                         && sm.MovementDate < movementCutoff
                         && sm.UnitCost.HasValue)
            .Select(sm => new
            {
                sm.ProductId,
                LocationId = sm.ToLocationId!.Value,
                sm.MovementDate,
                sm.UnitCost
            })
            .ToListAsync(cancellationToken))
            .GroupBy(sm => (sm.ProductId, sm.LocationId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(sm => sm.MovementDate).First().UnitCost);

        var stockCostLookup = await context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                        && !s.IsDeleted
                        && relevantProductIds.Contains(s.ProductId)
                        && relevantLocationIds.Contains(s.StorageLocationId))
            .Select(s => new { s.ProductId, s.StorageLocationId, s.UnitCost })
            .ToDictionaryAsync(
                s => (s.ProductId, s.StorageLocationId),
                s => s.UnitCost,
                cancellationToken);

        // ── Step 6: Resolve sale prices at the document date ─────────────────────────────────
        var salePriceLookup = await BuildSalePriceLookupAsync(
            relevantProductIds, documentDate, currentTenantId.Value, cancellationToken);

        // ── Step 7: Build result DTOs ─────────────────────────────────────────────────────────
        var effectiveSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var result = new List<StockSnapshotDto>(grouped.Count);

        foreach (var (productId, locId, quantity) in grouped)
        {
            if (!productLookup.TryGetValue(productId, out var product)) continue;
            if (!locationLookup.TryGetValue(locId, out var loc)) continue;

            if (effectiveSearch is not null &&
                !product.Name.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase) &&
                !product.Code.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Resolve unit cost: last inbound before document date → fallback to Stock.UnitCost.
            decimal? unitCost = null;
            lastCostLookup.TryGetValue((productId, locId), out unitCost);
            if (!unitCost.HasValue)
                stockCostLookup.TryGetValue((productId, locId), out unitCost);

            salePriceLookup.TryGetValue(productId, out var salePriceEntry);

            result.Add(new StockSnapshotDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                WarehouseId = loc.Warehouse?.Id ?? Guid.Empty,
                WarehouseName = loc.Warehouse?.Name ?? string.Empty,
                WarehouseCode = loc.Warehouse?.Code ?? string.Empty,
                LocationId = loc.Id,
                LocationCode = loc.Code,
                LocationDescription = loc.Description,
                LotId = null,    // inventory documents do not track at lot level
                LotCode = null,
                LotExpiry = null,
                Quantity = quantity,
                UnitCost = unitCost,
                SalePrice = salePriceEntry?.Price ?? product.DefaultPrice,
                IsPriceFromList = salePriceEntry is not null,
                PriceListName = salePriceEntry?.PriceListName,
                ReferenceDate = documentDate
            });
        }

        return result
            .OrderBy(s => s.ProductCode)
            .ThenBy(s => s.LocationCode);
    }

    // ── Private projection type ───────────────────────────────────────────────

    /// <summary>
    /// Lightweight projection of StockMovement data — only the scalar fields needed
    /// for snapshot accumulation. Using this avoids loading navigation-property columns
    /// (AuditableEntity audit fields, Notes, Reference, etc.) for every movement row.
    /// </summary>
    private sealed record MovementProjection(
        Guid ProductId,
        Guid? ToLocationId,
        Guid? FromLocationId,
        Guid? LotId,
        decimal Quantity,
        DateTime MovementDate,
        decimal? UnitCost);

    /// <summary>Accumulates quantity and purchase cost data for a snapshot group.</summary>
    private sealed class SnapshotAccumulator
    {
        /// <summary>Running net quantity (starts at the inventory anchor quantity when an anchor exists).</summary>
        public decimal Quantity { get; set; }

        public decimal? LastInboundUnitCost { get; set; }
        public DateTime LastInboundDate { get; set; }

        /// <summary>
        /// Pre-computed exclusive cutoff date for the inventory anchor:
        /// <c>anchorDate.Date.AddDays(1)</c>.
        /// Movements with <c>MovementDate &lt; InventoryAnchorCutoff</c> are excluded from the
        /// running total (they are already baked into the anchor quantity).
        /// Null when no inventory anchor is available — the full movement history is used.
        /// </summary>
        public DateTime? InventoryAnchorCutoff { get; set; }
    }

    /// <summary>
    /// Creates a <see cref="SnapshotAccumulator"/> pre-seeded with the inventory-document anchor
    /// quantity for the given key, if one exists in <paramref name="anchorLookup"/>.
    /// When no anchor exists the accumulator starts at zero (original behaviour).
    /// </summary>
    /// <remarks>
    /// The anchor lookup is keyed by <c>(ProductId, LocationId)</c> only — LotId is intentionally
    /// ignored because inventory documents record counts at the (Product, Location) granularity
    /// without lot-level tracking. One anchor therefore covers ALL lot-specific buckets within
    /// the same (Product, Location) pair, which is the correct semantic.
    /// </remarks>
    private static SnapshotAccumulator BuildAccumulatorWithAnchor(
        (Guid ProductId, Guid LocationId, Guid? LotId) key,
        Dictionary<(Guid ProductId, Guid LocationId), (decimal Quantity, DateTime Cutoff)> anchorLookup)
    {
        var acc = new SnapshotAccumulator();

        // Look up with null LotId regardless of the movement's actual LotId.
        // Inventory documents do not track at lot level, so one anchor covers all lots.
        if (anchorLookup.TryGetValue((key.ProductId, key.LocationId), out var anchor))
        {
            acc.Quantity = anchor.Quantity;
            acc.InventoryAnchorCutoff = anchor.Cutoff;
        }

        return acc;
    }

    /// <summary>Resolved sale-price entry for a product from a price list.</summary>
    private sealed record SalePriceEntry(decimal Price, string PriceListName);

    /// <summary>
    /// Builds a lookup of resolved sale prices for the given products at the reference date.
    /// <para>
    /// Resolution logic (mirrors PriceResolutionService priority 4 — general active price list):
    /// <list type="number">
    ///   <item>Find active Output price lists valid at <paramref name="referenceDateUtc"/>
    ///         (Status = Active, ValidFrom ≤ date, ValidTo = null or ≥ date), ordered by Priority.</item>
    ///   <item>For each product, pick the entry from the highest-priority list that contains it.</item>
    ///   <item>Products without a price-list entry are absent from the returned dictionary;
    ///         callers fall back to <c>Product.DefaultPrice</c>.</item>
    /// </list>
    /// A single DB query fetches all matching entries at once to avoid N+1 problems.
    /// </para>
    /// </summary>
    private async Task<Dictionary<Guid, SalePriceEntry>> BuildSalePriceLookupAsync(
        IReadOnlyCollection<Guid> productIds,
        DateTime referenceDateUtc,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Load all active Output price-list entries for the relevant products in one query.
        // We include the price list so we can use Priority and Name.
        var entries = await context.PriceListEntries
            .AsNoTracking()
            .Include(e => e.PriceList)
            .Where(e => productIds.Contains(e.ProductId)
                        && e.TenantId == tenantId
                        && !e.IsDeleted
                        && e.Status == Data.Entities.PriceList.PriceListEntryStatus.Active
                        && e.PriceList != null
                        && e.PriceList.TenantId == tenantId
                        && !e.PriceList.IsDeleted
                        && e.PriceList.Status == Data.Entities.PriceList.PriceListStatus.Active
                        && e.PriceList.Direction == PriceListDirection.Output
                        && (e.PriceList.ValidFrom == null || e.PriceList.ValidFrom.Value.Date <= referenceDateUtc)
                        && (e.PriceList.ValidTo == null || e.PriceList.ValidTo.Value.Date >= referenceDateUtc))
            .Select(e => new
            {
                e.ProductId,
                e.Price,
                PriceListName = e.PriceList!.Name,
                Priority = e.PriceList!.Priority
            })
            .ToListAsync(cancellationToken);

        // For each product keep only the entry from the highest-priority price list (lowest Priority value).
        return entries
            .GroupBy(e => e.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var best = g.OrderBy(e => e.Priority).First();
                    return new SalePriceEntry(best.Price, best.PriceListName);
                });
    }

}
