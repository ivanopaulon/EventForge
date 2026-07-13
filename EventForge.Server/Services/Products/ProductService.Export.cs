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

    public async Task<IEnumerable<Prym.DTOs.Export.ProductExportDto>> GetProductsForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.CategoryNode)
            .Where(p => !p.IsDeleted && p.TenantId == currentTenantId.Value)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);


        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetProductsInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(p => new Prym.DTOs.Export.ProductExportDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            ShortDescription = p.ShortDescription ?? string.Empty,
            Description = p.Description,
            Category = p.CategoryNode?.Name ?? string.Empty,
            UnitOfMeasure = p.UnitOfMeasure?.Symbol ?? string.Empty,
            Price = p.DefaultPrice ?? 0,
            Cost = 0, // Not available in Product entity
            StockQuantity = 0, // Not available in Product entity
            Brand = p.Brand?.Name,
            Model = p.Model?.Name,
            IsActive = p.Status == EntityProductStatus.Active,
            CreatedAt = p.CreatedAt
        });
    }

    private async Task<IEnumerable<Prym.DTOs.Export.ProductExportDto>> GetProductsInBatchesAsync(
        IQueryable<Product> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.ProductExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(p => new Prym.DTOs.Export.ProductExportDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                ShortDescription = p.ShortDescription ?? string.Empty,
                Description = p.Description,
                Category = p.CategoryNode?.Name ?? string.Empty,
                UnitOfMeasure = p.UnitOfMeasure?.Symbol ?? string.Empty,
                Price = p.DefaultPrice ?? 0,
                Cost = 0, // Not available in Product entity
                StockQuantity = 0, // Not available in Product entity
                Brand = p.Brand?.Name,
                Model = p.Model?.Name,
                IsActive = p.Status == EntityProductStatus.Active,
                CreatedAt = p.CreatedAt
            }));

            skip += batchSize;

        }

        return results;
    }

    /// <summary>
    /// Performs a bulk price update on multiple products in a single transaction.
    /// </summary>
    public async Task<Prym.DTOs.Bulk.BulkUpdateResultDto> BulkUpdatePricesAsync(
        Prym.DTOs.Bulk.BulkUpdatePricesDto bulkUpdateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<Prym.DTOs.Bulk.BulkItemError>();
        var successCount = 0;

        // Validate batch size
        if (bulkUpdateDto.ProductIds.Count > 500)
        {
            throw new ArgumentException("Maximum 500 products can be updated at once.");
        }

        // Validate required fields based on update type
        switch (bulkUpdateDto.UpdateType)
        {
            case Prym.DTOs.Bulk.PriceUpdateType.Replace:
                if (!bulkUpdateDto.NewPrice.HasValue)
                {
                    throw new ArgumentException("NewPrice is required for Replace operation.");
                }
                break;
            case Prym.DTOs.Bulk.PriceUpdateType.IncreaseByPercentage:
            case Prym.DTOs.Bulk.PriceUpdateType.DecreaseByPercentage:
                if (!bulkUpdateDto.Percentage.HasValue)
                {
                    throw new ArgumentException("Percentage is required for percentage-based operations.");
                }
                break;
            case Prym.DTOs.Bulk.PriceUpdateType.IncreaseByAmount:
            case Prym.DTOs.Bulk.PriceUpdateType.DecreaseByAmount:
                if (!bulkUpdateDto.Amount.HasValue)
                {
                    throw new ArgumentException("Amount is required for amount-based operations.");
                }
                break;
        }

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Fetch all products in one query
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for bulk update operations.");
            }

            var products = await context.Products
                .Where(p => bulkUpdateDto.ProductIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
                .ToListAsync(cancellationToken);

            // Check for missing products
            var foundIds = products.Select(p => p.Id).ToHashSet();
            var missingIds = bulkUpdateDto.ProductIds.Where(id => !foundIds.Contains(id)).ToList();
            foreach (var missingId in missingIds)
            {
                errors.Add(new Prym.DTOs.Bulk.BulkItemError
                {
                    ItemId = missingId,
                    ErrorMessage = "Product not found or does not belong to the current tenant."
                });
            }

            // Update prices
            foreach (var product in products)
            {
                try
                {
                    var newPrice = CalculateNewPrice(product.DefaultPrice ?? 0, bulkUpdateDto);

                    if (newPrice < 0)
                    {
                        errors.Add(new Prym.DTOs.Bulk.BulkItemError
                        {
                            ItemId = product.Id,
                            ItemName = product.Name,
                            ErrorMessage = "Calculated price is negative."
                        });
                        continue;
                    }

                    product.DefaultPrice = newPrice;
                    product.ModifiedAt = DateTime.UtcNow;
                    product.ModifiedBy = currentUser;
                    successCount++;

                    logger.LogInformation(
                        "Bulk price update: Product {ProductId} price changed to {NewPrice}. Reason: {Reason}",
                        product.Id, newPrice, bulkUpdateDto.Reason ?? "N/A");
                }
                catch (Exception ex)
                {
                    errors.Add(new Prym.DTOs.Bulk.BulkItemError
                    {
                        ItemId = product.Id,
                        ItemName = product.Name,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Bulk price update completed: {SuccessCount} successful, {FailureCount} failed",
                successCount, errors.Count);

            return new Prym.DTOs.Bulk.BulkUpdateResultDto
            {
                TotalCount = bulkUpdateDto.ProductIds.Count,
                SuccessCount = successCount,
                FailedCount = errors.Count,
                Errors = errors,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = false
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Bulk price update failed and was rolled back");

            return new Prym.DTOs.Bulk.BulkUpdateResultDto
            {
                TotalCount = bulkUpdateDto.ProductIds.Count,
                SuccessCount = 0,
                FailedCount = bulkUpdateDto.ProductIds.Count,
                Errors = new List<Prym.DTOs.Bulk.BulkItemError>
                {
                    new Prym.DTOs.Bulk.BulkItemError
                    {
                        ItemId = Guid.Empty,
                        ErrorMessage = $"Transaction failed and was rolled back: {ex.Message}"
                    }
                },
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = true
            };
        }
    }

    private decimal CalculateNewPrice(decimal currentPrice, Prym.DTOs.Bulk.BulkUpdatePricesDto dto)
    {
        return dto.UpdateType switch
        {
            Prym.DTOs.Bulk.PriceUpdateType.Replace => dto.NewPrice ?? 0,
            Prym.DTOs.Bulk.PriceUpdateType.IncreaseByPercentage =>
                currentPrice * (1 + (dto.Percentage ?? 0) / 100),
            Prym.DTOs.Bulk.PriceUpdateType.DecreaseByPercentage =>
                currentPrice * (1 - (dto.Percentage ?? 0) / 100),
            Prym.DTOs.Bulk.PriceUpdateType.IncreaseByAmount =>
                currentPrice + (dto.Amount ?? 0),
            Prym.DTOs.Bulk.PriceUpdateType.DecreaseByAmount =>
                currentPrice - (dto.Amount ?? 0),
            _ => currentPrice
        };
    }

}
