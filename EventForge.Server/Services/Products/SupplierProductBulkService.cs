using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for bulk operations on supplier products with transaction safety and audit logging.
/// </summary>
public class SupplierProductBulkService : ISupplierProductBulkService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<SupplierProductBulkService> _logger;

    public SupplierProductBulkService(
        EventForgeDbContext context,
        ILogger<SupplierProductBulkService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BulkUpdateResult> BulkUpdateSupplierProductsAsync(
        Guid supplierId,
        BulkUpdateSupplierProductsRequest request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkUpdateResult
        {
            TotalRequested = request.ProductIds.Count
        };

        if (request.ProductIds.Count == 0)
        {
            _logger.LogWarning("Bulk update requested with no product IDs for supplier {SupplierId}", supplierId);
            return result;
        }

        IDbContextTransaction? transaction = null;

        try
        {
            // Start transaction for atomic operation
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Starting bulk update for {Count} products for supplier {SupplierId} by user {User}",
                request.ProductIds.Count,
                supplierId,
                currentUser);

            // Load all product suppliers in one query
            var productSuppliers = await _context.ProductSuppliers
                .Include(ps => ps.Product)
                .Where(ps => ps.SupplierId == supplierId && request.ProductIds.Contains(ps.ProductId))
                .ToListAsync(cancellationToken);

            foreach (var productId in request.ProductIds)
            {
                try
                {
                    var productSupplier = productSuppliers.FirstOrDefault(ps => ps.ProductId == productId);

                    if (productSupplier == null)
                    {
                        result.Errors.Add(new BulkUpdateError
                        {
                            ProductId = productId,
                            ErrorMessage = "Product supplier relationship not found"
                        });
                        result.FailureCount++;
                        continue;
                    }

                    // Apply updates and log changes
                    var hasChanges = await ApplyUpdatesAndLogAsync(
                        productSupplier,
                        request,
                        currentUser,
                        cancellationToken);

                    if (hasChanges)
                    {
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product {ProductId} for supplier {SupplierId}", productId, supplierId);
                    result.Errors.Add(new BulkUpdateError
                    {
                        ProductId = productId,
                        ProductName = productSuppliers.FirstOrDefault(ps => ps.ProductId == productId)?.Product?.Name,
                        ErrorMessage = ex.Message
                    });
                    result.FailureCount++;
                }
            }

            // If there are errors, decide whether to rollback
            if (result.FailureCount > 0)
            {
                _logger.LogWarning(
                    "Bulk update had {FailureCount} failures out of {TotalRequested} for supplier {SupplierId}. Rolling back transaction.",
                    result.FailureCount,
                    result.TotalRequested,
                    supplierId);

                await transaction.RollbackAsync(cancellationToken);
                result.RolledBack = true;
                result.SuccessCount = 0; // Reset success count since we rolled back
            }
            else
            {
                // Save changes and commit transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully completed bulk update for {SuccessCount} products for supplier {SupplierId}",
                    result.SuccessCount,
                    supplierId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during bulk update for supplier {SupplierId}", supplierId);

            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                result.RolledBack = true;
            }

            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<SupplierProductPreview>> PreviewBulkUpdateAsync(
        Guid supplierId,
        BulkUpdateSupplierProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var previews = new List<SupplierProductPreview>();

        if (request.ProductIds.Count == 0)
        {
            return previews;
        }

        // Load all product suppliers
        var productSuppliers = await _context.ProductSuppliers
            .Include(ps => ps.Product)
            .Where(ps => ps.SupplierId == supplierId && request.ProductIds.Contains(ps.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var ps in productSuppliers)
        {
            var preview = new SupplierProductPreview
            {
                ProductId = ps.ProductId,
                ProductName = ps.Product?.Name,
                CurrentUnitCost = ps.UnitCost,
                CurrentLeadTimeDays = ps.LeadTimeDays,
                CurrentCurrency = ps.Currency,
                CurrentMinOrderQty = ps.MinOrderQty,
                CurrentPreferred = ps.Preferred
            };

            // Calculate new values
            if (request.UpdateMode.HasValue && request.UnitCostValue.HasValue)
            {
                preview.NewUnitCost = CalculateNewPrice(ps.UnitCost, request.UpdateMode.Value, request.UnitCostValue.Value);
                preview.Delta = preview.NewUnitCost - ps.UnitCost;
            }
            else
            {
                preview.NewUnitCost = ps.UnitCost;
                preview.Delta = 0;
            }

            preview.NewLeadTimeDays = request.LeadTimeDays ?? ps.LeadTimeDays;
            preview.NewCurrency = request.Currency ?? ps.Currency;
            preview.NewMinOrderQty = request.MinOrderQuantity ?? ps.MinOrderQty;
            preview.NewPreferred = request.IsPreferred ?? ps.Preferred;

            previews.Add(preview);
        }

        return previews;
    }

    /// <summary>
    /// Applies updates to a product supplier and logs changes.
    /// </summary>
    private async Task<bool> ApplyUpdatesAndLogAsync(
        ProductSupplier productSupplier,
        BulkUpdateSupplierProductsRequest request,
        string currentUser,
        CancellationToken cancellationToken)
    {
        var hasChanges = false;
        var now = DateTime.UtcNow;

        // Update unit cost
        if (request.UpdateMode.HasValue && request.UnitCostValue.HasValue)
        {
            var oldUnitCost = productSupplier.UnitCost;
            var newUnitCost = CalculateNewPrice(oldUnitCost, request.UpdateMode.Value, request.UnitCostValue.Value);

            if (newUnitCost != oldUnitCost)
            {
                // Validate new price
                if (newUnitCost < 0)
                {
                    throw new InvalidOperationException($"Calculated unit cost cannot be negative: {newUnitCost}");
                }

                await LogChangeAsync(
                    productSupplier.Id,
                    "ProductSupplier",
                    productSupplier.Product?.Name,
                    "UnitCost",
                    "BulkUpdate",
                    oldUnitCost?.ToString("F6"),
                    newUnitCost?.ToString("F6"),
                    currentUser,
                    now,
                    cancellationToken);

                productSupplier.UnitCost = newUnitCost;
                hasChanges = true;
            }
        }

        // Update lead time
        if (request.LeadTimeDays.HasValue && request.LeadTimeDays.Value != productSupplier.LeadTimeDays)
        {
            if (request.LeadTimeDays.Value < 0)
            {
                throw new InvalidOperationException("Lead time days cannot be negative");
            }

            await LogChangeAsync(
                productSupplier.Id,
                "ProductSupplier",
                productSupplier.Product?.Name,
                "LeadTimeDays",
                "BulkUpdate",
                productSupplier.LeadTimeDays?.ToString(),
                request.LeadTimeDays.Value.ToString(),
                currentUser,
                now,
                cancellationToken);

            productSupplier.LeadTimeDays = request.LeadTimeDays.Value;
            hasChanges = true;
        }

        // Update currency
        if (!string.IsNullOrWhiteSpace(request.Currency) && request.Currency != productSupplier.Currency)
        {
            await LogChangeAsync(
                productSupplier.Id,
                "ProductSupplier",
                productSupplier.Product?.Name,
                "Currency",
                "BulkUpdate",
                productSupplier.Currency,
                request.Currency,
                currentUser,
                now,
                cancellationToken);

            productSupplier.Currency = request.Currency;
            hasChanges = true;
        }

        // Update minimum order quantity
        if (request.MinOrderQuantity.HasValue && request.MinOrderQuantity.Value != productSupplier.MinOrderQty)
        {
            if (request.MinOrderQuantity.Value < 0)
            {
                throw new InvalidOperationException("Minimum order quantity cannot be negative");
            }

            await LogChangeAsync(
                productSupplier.Id,
                "ProductSupplier",
                productSupplier.Product?.Name,
                "MinOrderQty",
                "BulkUpdate",
                productSupplier.MinOrderQty?.ToString(),
                request.MinOrderQuantity.Value.ToString(),
                currentUser,
                now,
                cancellationToken);

            productSupplier.MinOrderQty = request.MinOrderQuantity.Value;
            hasChanges = true;
        }

        // Update preferred status
        if (request.IsPreferred.HasValue && request.IsPreferred.Value != productSupplier.Preferred)
        {
            await LogChangeAsync(
                productSupplier.Id,
                "ProductSupplier",
                productSupplier.Product?.Name,
                "Preferred",
                "BulkUpdate",
                productSupplier.Preferred.ToString(),
                request.IsPreferred.Value.ToString(),
                currentUser,
                now,
                cancellationToken);

            productSupplier.Preferred = request.IsPreferred.Value;
            hasChanges = true;
        }

        // Update audit fields
        if (hasChanges)
        {
            productSupplier.ModifiedAt = now;
            productSupplier.ModifiedBy = currentUser;
        }

        return hasChanges;
    }

    /// <summary>
    /// Calculates the new price based on the update mode.
    /// </summary>
    private static decimal? CalculateNewPrice(decimal? currentPrice, UpdateMode mode, decimal value)
    {
        if (!currentPrice.HasValue)
        {
            // If no current price, only Set mode makes sense
            return mode == UpdateMode.Set ? value : null;
        }

        return mode switch
        {
            UpdateMode.Set => value,
            UpdateMode.Increase => currentPrice.Value + value,
            UpdateMode.Decrease => currentPrice.Value - value,
            UpdateMode.PercentageIncrease => currentPrice.Value * (1 + value / 100m),
            UpdateMode.PercentageDecrease => currentPrice.Value * (1 - value / 100m),
            _ => currentPrice
        };
    }

    /// <summary>
    /// Logs a change to the EntityChangeLog table.
    /// </summary>
    private async Task LogChangeAsync(
        Guid entityId,
        string entityName,
        string? entityDisplayName,
        string propertyName,
        string operationType,
        string? oldValue,
        string? newValue,
        string changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        var changeLog = new EntityChangeLog
        {
            EntityId = entityId,
            EntityName = entityName,
            EntityDisplayName = entityDisplayName,
            PropertyName = propertyName,
            OperationType = operationType,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAt = changedAt
        };

        _context.EntityChangeLogs.Add(changeLog);
    }
}
