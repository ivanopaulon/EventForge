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

    /// <summary>
    /// Builds the filter query for bulk product operations without loading entities.
    /// Used by both CountProductsMatchingFiltersAsync and BulkUpdateProductsAsync.
    /// </summary>
    private async Task<IQueryable<Data.Entities.Products.Product>> BuildBulkFilterQueryAsync(
        BulkUpdateProductsDto dto, Guid tenantId, CancellationToken cancellationToken)
    {
        IQueryable<Data.Entities.Products.Product> query;

        if (dto.ProductIds?.Count > 0)
        {
            query = context.Products.WhereActiveTenant(tenantId)
                .Where(p => dto.ProductIds.Contains(p.Id));
        }
        else
        {
            query = context.Products.WhereActiveTenant(tenantId);

            if (dto.FilterBrandId.HasValue)
                query = query.Where(p => p.BrandId == dto.FilterBrandId.Value);

            if (dto.FilterVatRateId.HasValue)
                query = query.Where(p => p.VatRateId == dto.FilterVatRateId.Value);

            if (dto.FilterUnitOfMeasureId.HasValue)
                query = query.Where(p => p.UnitOfMeasureId == dto.FilterUnitOfMeasureId.Value);

            if (dto.FilterModelId.HasValue)
                query = query.Where(p => p.ModelId == dto.FilterModelId.Value);

            if (dto.FilterStationId.HasValue)
                query = query.Where(p => p.StationId == dto.FilterStationId.Value);

            if (dto.FilterStatus.HasValue)
                query = query.Where(p => (int)p.Status == (int)dto.FilterStatus.Value);

            if (dto.FilterIsBundle.HasValue)
                query = query.Where(p => p.IsBundle == dto.FilterIsBundle.Value);

            if (dto.FilterClassificationNodeId.HasValue)
            {
                var descendantIds = await GetDescendantNodeIdsAsync(dto.FilterClassificationNodeId.Value, cancellationToken);
                descendantIds.Add(dto.FilterClassificationNodeId.Value);
                query = query.Where(p =>
                    (p.CategoryNodeId.HasValue && descendantIds.Contains(p.CategoryNodeId.Value)) ||
                    (p.FamilyNodeId.HasValue && descendantIds.Contains(p.FamilyNodeId.Value)) ||
                    (p.GroupNodeId.HasValue && descendantIds.Contains(p.GroupNodeId.Value)));
            }
        }

        return query;
    }

    public async Task<int> CountProductsMatchingFiltersAsync(BulkUpdateProductsDto dto, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for bulk product operations.");

        var query = await BuildBulkFilterQueryAsync(dto, currentTenantId.Value, cancellationToken);
        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Number of products processed per <see cref="BulkUpdateProductsAsync"/> database round-trip.
    /// Keeping batches small avoids long-running transactions and prevents excessive memory pressure
    /// when a filter matches tens of thousands of products.
    /// </summary>
    private const int BulkCatalogUpdateBatchSize = 500;

    /// <summary>
    /// Updates product catalog fields (VAT rate, UoM, brand, classification, stock parameters, …)
    /// for all products that match the filters or explicit ID list in <paramref name="dto"/>.
    /// There is no hard limit on the number of products that can be updated; processing is split
    /// into batches of <see cref="BulkCatalogUpdateBatchSize"/> to limit memory pressure and keep
    /// individual DB round-trips short. All batches run inside a single transaction so the overall
    /// operation is still atomic.
    /// </summary>
    /// <remarks>
    /// <b>ProductIds vs. filter predicates</b>: when <see cref="BulkUpdateProductsDto.ProductIds"/>
    /// is non-null and non-empty the explicit ID list is used exclusively (filter predicates are
    /// ignored). An <em>empty</em> <see cref="BulkUpdateProductsDto.ProductIds"/> list (Count == 0)
    /// is treated the same as a null list — i.e. filter predicates are applied instead.  Callers
    /// must not pass an empty list expecting zero products to be updated; pass null or omit the
    /// property to activate filter-predicate mode.
    /// </remarks>
    public async Task<BulkUpdateResult> BulkUpdateProductsAsync(BulkUpdateProductsDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for bulk product operations.");

        // Validate that at least one field is specified for update.
        // Without this guard the operation would open a transaction, load all matching products,
        // call SaveChangesAsync (writing nothing) and commit — a no-op with real DB cost.
        if (!dto.UnitOfMeasureId.HasValue && !dto.VatRateId.HasValue && !dto.BrandId.HasValue &&
            !dto.ModelId.HasValue && !dto.CategoryNodeId.HasValue && !dto.FamilyNodeId.HasValue &&
            !dto.GroupNodeId.HasValue && !dto.Status.HasValue && !dto.IsVatIncluded.HasValue &&
            !dto.ReorderPoint.HasValue && !dto.SafetyStock.HasValue && !dto.TargetStockLevel.HasValue &&
            !dto.AverageDailyDemand.HasValue && !dto.PreferredSupplierId.HasValue && !dto.StationId.HasValue)
        {
            throw new ArgumentException("At least one field must be specified for bulk catalog update.");
        }

        var query = await BuildBulkFilterQueryAsync(dto, currentTenantId.Value, cancellationToken);

        // Fetch only IDs up-front so the filter is evaluated once before any rows are mutated.
        // This avoids Skip/Take instability that could arise if updated fields overlap with filter predicates.
        var productIds = await query.Select(p => p.Id).ToListAsync(cancellationToken);

        var result = new BulkUpdateResult { TotalRequested = productIds.Count };

        if (productIds.Count == 0)
            return result;

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            for (int batchStart = 0; batchStart < productIds.Count; batchStart += BulkCatalogUpdateBatchSize)
            {
                var batchIds = productIds
                    .Skip(batchStart)
                    .Take(BulkCatalogUpdateBatchSize)
                    .ToList();

                var batch = await context.Products
                    .Where(p => batchIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var product in batch)
                {
                    try
                    {
                        if (dto.UnitOfMeasureId.HasValue) product.UnitOfMeasureId = dto.UnitOfMeasureId;
                        if (dto.VatRateId.HasValue) product.VatRateId = dto.VatRateId;
                        if (dto.BrandId.HasValue) product.BrandId = dto.BrandId;
                        if (dto.ModelId.HasValue) product.ModelId = dto.ModelId;
                        if (dto.CategoryNodeId.HasValue) product.CategoryNodeId = dto.CategoryNodeId;
                        if (dto.FamilyNodeId.HasValue) product.FamilyNodeId = dto.FamilyNodeId;
                        if (dto.GroupNodeId.HasValue) product.GroupNodeId = dto.GroupNodeId;
                        if (dto.Status.HasValue) product.Status = (Data.Entities.Products.ProductStatus)(int)dto.Status.Value;
                        if (dto.IsVatIncluded.HasValue) product.IsVatIncluded = dto.IsVatIncluded.Value;
                        if (dto.ReorderPoint.HasValue) product.ReorderPoint = dto.ReorderPoint;
                        if (dto.SafetyStock.HasValue) product.SafetyStock = dto.SafetyStock;
                        if (dto.TargetStockLevel.HasValue) product.TargetStockLevel = dto.TargetStockLevel;
                        if (dto.AverageDailyDemand.HasValue) product.AverageDailyDemand = dto.AverageDailyDemand;
                        if (dto.PreferredSupplierId.HasValue) product.PreferredSupplierId = dto.PreferredSupplierId;
                        if (dto.StationId.HasValue) product.StationId = dto.StationId;

                        product.ModifiedAt = now;
                        product.ModifiedBy = currentUser;

                        result.SuccessCount++;

                        logger.LogDebug(
                            "Bulk catalog update: Product {ProductId} ({ProductName}) updated by {User}. Reason: {Reason}.",
                            product.Id, product.Name, currentUser, dto.Reason ?? "N/A");
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BulkUpdateError { ProductId = product.Id, ErrorMessage = ex.Message });
                        logger.LogWarning(ex, "Error updating product {ProductId} during bulk update.", product.Id);
                    }
                }

                await context.SaveChangesAsync(cancellationToken);

                // Detach processed entities so the change tracker does not accumulate all rows in
                // memory across batches.  The transaction remains open — commit happens after all batches.
                context.ChangeTracker.Clear();

                logger.LogDebug(
                    "Bulk catalog update: batch {BatchEnd}/{Total} saved.",
                    Math.Min(batchStart + BulkCatalogUpdateBatchSize, productIds.Count),
                    productIds.Count);
            }

            if (result.SuccessCount > 0)
            {
                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation("Bulk catalog update committed: {Success}/{Total} products updated by {User}. Reason: {Reason}.",
                    result.SuccessCount, result.TotalRequested, currentUser, dto.Reason ?? "N/A");
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning("Bulk catalog update: no products were updated (all {Total} failed). Transaction rolled back. User: {User}.",
                    result.TotalRequested, currentUser);
            }

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            result.RolledBack = true;
            // Log before resetting counters so the message reflects how many in-memory mutations were abandoned.
            logger.LogError(ex, "Bulk catalog update rolled back. {RolledBackCount} in-memory mutations were discarded. User: {User}.",
                result.SuccessCount, currentUser);
            // Reset counters to reflect actual DB state: the transaction was rolled back,
            // so no products were persisted regardless of how many were processed in memory.
            result.SuccessCount = 0;
            result.FailureCount = result.TotalRequested;
            return result;
        }
    }

}
