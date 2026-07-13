using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<IEnumerable<StockSnapshotDto>> GetStockSnapshotAsync(
        DateTime referenceDate,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        // Cover the full reference day in UTC: movements on the reference date are included.
        var cutoff = referenceDate.Date.AddDays(1);

        // ── Step 1: Load movements via projection ────────────────────────────
        // Using a scalar projection avoids loading unused navigation-property columns
        // (AuditableEntity fields, Notes, Reference, …) for every movement row.
        // Only Completed movements contribute to historical stock levels.
        // POS sales (SaleSessionService) and document approval (DocumentHeaderService) both
        // create movements directly with MovementStatus.Completed, so the filter is correct.
        // Planned/InProgress/Cancelled/Failed movements represent uncommitted or voided intent
        // and must NOT influence the historical balance.
        var movementsBase = context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == currentTenantId.Value
                         && !sm.IsDeleted
                         && sm.MovementDate < cutoff
                         && sm.Status == MovementStatus.Completed);

        // Push location/warehouse filters to the database to avoid loading irrelevant rows.
        if (locationId.HasValue)
        {
            movementsBase = movementsBase.Where(sm =>
                sm.ToLocationId == locationId.Value || sm.FromLocationId == locationId.Value);
        }
        else if (warehouseId.HasValue)
        {
            var wid = warehouseId.Value;
            // Navigation-property access in WHERE is translated to a JOIN without loading the entity.
            movementsBase = movementsBase.Where(sm =>
                (sm.ToLocation != null && sm.ToLocation.WarehouseId == wid) ||
                (sm.FromLocation != null && sm.FromLocation.WarehouseId == wid));
        }

        var rawMovements = await movementsBase
            .Select(sm => new MovementProjection(
                sm.ProductId,
                sm.ToLocationId,
                sm.FromLocationId,
                sm.LotId,
                sm.Quantity,
                sm.MovementDate,
                sm.UnitCost))
            .ToListAsync(cancellationToken);

        if (rawMovements.Count == 0)
            return Enumerable.Empty<StockSnapshotDto>();

        // ── Step 2: Collect IDs for bulk-loading reference data ──────────────
        var relevantProductIds = rawMovements.Select(m => m.ProductId).Distinct().ToList();
        var relevantLocationIds = rawMovements
            .SelectMany(m => new[] { m.ToLocationId, m.FromLocationId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var relevantLotIds = rawMovements
            .Where(m => m.LotId.HasValue)
            .Select(m => m.LotId!.Value)
            .Distinct()
            .ToList();

        // ── Step 3: Bulk-load Products, Locations, Lots ──────────────────────
        // DbContext is not thread-safe — queries must be sequential.

        var productLookup = await context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenantId.Value
                        && !p.IsDeleted
                        && relevantProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Location lookup with warehouse navigation so we can fill warehouse name/code in the DTO.
        var locationLookup = await context.StorageLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => l.TenantId == currentTenantId.Value
                        && !l.IsDeleted
                        && relevantLocationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Lot lookup is only needed when at least one movement references a lot.
        var lotLookup = relevantLotIds.Count > 0
            ? await context.Lots
                .AsNoTracking()
                .Where(l => l.TenantId == currentTenantId.Value
                            && !l.IsDeleted
                            && relevantLotIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, cancellationToken)
            : new Dictionary<Guid, Data.Entities.Warehouse.Lot>();

        // ── Step 4: Load inventory-document anchors (projected) ───────────────
        // For each (ProductId, LocationId) pair, the most recent closed inventory document
        // whose date is before the cutoff seeds the accumulator with the counted quantity.
        // Only movements dated *after* the inventory document's date are then applied on top.
        // DocumentRow has no LotId column — inventory counting is done at the (Product, Location)
        // level, so one anchor covers all lot-specific buckets within the same location.
        // Using a scalar projection avoids loading full DocumentRow/DocumentHeader/DocumentType entities.
        var inventoryAnchorLookup = (await context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.TenantId == currentTenantId.Value
                         && !dr.IsDeleted
                         && dr.ProductId.HasValue
                         && relevantProductIds.Contains(dr.ProductId!.Value)
                         && dr.LocationId.HasValue
                         && relevantLocationIds.Contains(dr.LocationId!.Value)
                         && dr.DocumentHeader != null
                         && dr.DocumentHeader.DocumentType != null
                         && dr.DocumentHeader.DocumentType.IsInventoryDocument
                         && (dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived)
                         && dr.DocumentHeader.Date < cutoff)
            .Select(dr => new
            {
                ProductId = dr.ProductId!.Value,
                LocationId = dr.LocationId!.Value,
                dr.Quantity,
                DocumentDate = dr.DocumentHeader!.Date
            })
            .ToListAsync(cancellationToken))
            .GroupBy(r => (r.ProductId, r.LocationId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Most recent inventory document for this (Product, Location) pair.
                    var best = g.OrderByDescending(r => r.DocumentDate).First();
                    // Pre-compute the cutoff so the inner loop does not call AddDays(1) per iteration.
                    return (Quantity: best.Quantity, Cutoff: best.DocumentDate.Date.AddDays(1));
                });

        // ── Step 5: Load Stock.UnitCost fallback ─────────────────────────────
        var stockCostLookup = await context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                        && !s.IsDeleted
                        && relevantProductIds.Contains(s.ProductId)
                        && relevantLocationIds.Contains(s.StorageLocationId))
            .Select(s => new { s.ProductId, s.StorageLocationId, s.LotId, s.UnitCost })
            .ToDictionaryAsync(
                s => (s.ProductId, s.StorageLocationId, s.LotId),
                s => s.UnitCost,
                cancellationToken);

        // ── Step 6: Resolve sale prices ──────────────────────────────────────
        // Mirrors the normal pricing cascade (priority 4 of PriceResolutionService) without
        // business-party or document context (irrelevant for a stock valuation snapshot).
        var referenceDateUtc = referenceDate.Date;
        var salePriceLookup = await BuildSalePriceLookupAsync(
            relevantProductIds, referenceDateUtc, currentTenantId.Value, cancellationToken);

        // ── Step 7: Accumulate movements into per-(Product, Location, Lot) buckets ──
        // Inflows  → ToLocationId  (+Quantity, tracks last UnitCost)
        // Outflows → FromLocationId (-Quantity)
        // Convention: StockMovement.Quantity is always stored as a positive value regardless of
        // direction — the sign is implicit from MovementType / FromLocationId / ToLocationId.
        // Math.Abs is applied defensively so that any legacy rows with a negative Quantity
        // (e.g. written by old code) do not silently invert the sign of the accumulator.
        //
        // When an inventory-document anchor exists for a (ProductId, LocationId) pair, the
        // accumulator starts at the anchored quantity instead of zero, and only movements
        // dated strictly after the inventory document's date are applied.
        var groups = new Dictionary<(Guid ProductId, Guid LocationId, Guid? LotId), SnapshotAccumulator>(
            rawMovements.Count / 2);

        foreach (var mv in rawMovements)
        {
            var absQty = Math.Abs(mv.Quantity);

            // Inflow contribution.
            if (mv.ToLocationId.HasValue)
            {
                var key = (mv.ProductId, mv.ToLocationId.Value, mv.LotId);
                if (!groups.TryGetValue(key, out var acc))
                {
                    acc = BuildAccumulatorWithAnchor(key, inventoryAnchorLookup);
                    groups[key] = acc;
                }

                // Skip movements that pre-date (or are on the same day as) the inventory anchor.
                // InventoryAnchorCutoff is pre-computed (anchorDate.Date + 1 day), so no AddDays() here.
                if (acc.InventoryAnchorCutoff.HasValue && mv.MovementDate < acc.InventoryAnchorCutoff.Value)
                    continue;

                acc.Quantity += absQty;
                // Track the most recent inbound UnitCost for the purchase price.
                if (mv.UnitCost.HasValue && mv.MovementDate > acc.LastInboundDate)
                {
                    acc.LastInboundDate = mv.MovementDate;
                    acc.LastInboundUnitCost = mv.UnitCost;
                }
            }

            // Outflow contribution.
            if (mv.FromLocationId.HasValue)
            {
                var key = (mv.ProductId, mv.FromLocationId.Value, mv.LotId);
                if (!groups.TryGetValue(key, out var acc))
                {
                    acc = BuildAccumulatorWithAnchor(key, inventoryAnchorLookup);
                    groups[key] = acc;
                }

                if (acc.InventoryAnchorCutoff.HasValue && mv.MovementDate < acc.InventoryAnchorCutoff.Value)
                    continue;

                acc.Quantity -= absQty;
            }
        }

        // ── Step 8: Build result DTOs ─────────────────────────────────────────
        // Trim the search term once. StringComparison.OrdinalIgnoreCase is used in Contains.
        var effectiveSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var result = new List<StockSnapshotDto>(groups.Count);

        foreach (var (key, acc) in groups)
        {
            if (!productLookup.TryGetValue(key.ProductId, out var product)) continue;
            if (!locationLookup.TryGetValue(key.LocationId, out var loc)) continue;

            var lot = key.LotId.HasValue && lotLookup.TryGetValue(key.LotId.Value, out var l) ? l : null;

            // Apply optional search filter (in-memory, after aggregation).
            if (effectiveSearch is not null &&
                !product.Name.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase) &&
                !product.Code.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Resolve purchase unit cost: last inbound movement's cost → fallback to Stock.UnitCost.
            decimal? unitCost = acc.LastInboundUnitCost;
            if (!unitCost.HasValue)
                stockCostLookup.TryGetValue((key.ProductId, key.LocationId, key.LotId), out unitCost);

            // Resolve sale price: price-list entry (Output, active at referenceDate) → Product.DefaultPrice.
            salePriceLookup.TryGetValue(key.ProductId, out var salePriceEntry);

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
                LotId = lot?.Id,
                LotCode = lot?.Code,
                LotExpiry = lot?.ExpiryDate,
                Quantity = acc.Quantity,
                UnitCost = unitCost,
                SalePrice = salePriceEntry?.Price ?? product.DefaultPrice,
                IsPriceFromList = salePriceEntry is not null,
                PriceListName = salePriceEntry?.PriceListName,
                ReferenceDate = referenceDate.Date
            });
        }

        return result
            .OrderBy(s => s.ProductCode)
            .ThenBy(s => s.LocationCode);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InventorySnapshotDateDto>> GetRecentInventoryDatesAsync(
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            return Array.Empty<InventorySnapshotDateDto>();

        try
        {
            // Project only the scalar fields that are needed — this avoids loading full entity
            // columns and, critically, avoids using CLR-only properties such as DateTime.Kind
            // that EF Core cannot translate to SQL against a real (non-InMemory) database.
            var raw = await context.DocumentHeaders
                .AsNoTracking()
                .Where(dh => dh.TenantId == currentTenantId.Value
                             && !dh.IsDeleted
                             && dh.DocumentType != null
                             && dh.DocumentType.IsInventoryDocument
                             && (dh.Status == DocumentStatus.Archived))
                .OrderByDescending(dh => dh.Date)
                .Take(count)
                .Select(dh => new { dh.Id, dh.Date, dh.Number })
                .ToListAsync(cancellationToken);

            // Map to DTO in-memory (safe to use .Date here, outside the LINQ-to-SQL boundary).
            // SpecifyKind ensures the serialized value always carries a UTC timezone offset,
            // matching the documented contract on InventorySnapshotDateDto.Date.
            return raw
                .Select(r => new InventorySnapshotDateDto
                {
                    DocumentHeaderId = r.Id,
                    Date = DateTime.SpecifyKind(r.Date.Date, DateTimeKind.Utc),
                    DocumentNumber = r.Number
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent inventory dates for tenant {TenantId}.", currentTenantId.Value);
            return Array.Empty<InventorySnapshotDateDto>();
        }
    }

    /// <inheritdoc/>
}
