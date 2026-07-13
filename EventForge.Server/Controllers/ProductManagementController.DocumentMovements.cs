using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class ProductManagementController
{

    /// <summary>
    /// Gets paginated document movements for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="fromDate">Optional filter: start date</param>
    /// <param name="toDate">Optional filter: end date</param>
    /// <param name="businessPartyName">Optional filter: customer/supplier name</param>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product document movements</returns>
    /// <response code="200">Returns the paginated list of movements</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/document-movements")]
    [ProducesResponseType(typeof(PagedResult<ProductDocumentMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ProductDocumentMovementDto>>> GetProductDocumentMovements(
        Guid id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? businessPartyName = null,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination = null!,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            // Get document movements using DocumentHeaderService
            var queryParameters = new Prym.DTOs.Documents.DocumentHeaderQueryParameters
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                ProductId = id,
                FromDate = fromDate,
                ToDate = toDate,
                CustomerName = businessPartyName,
                SortBy = "Date",
                SortDirection = "desc",
                IncludeRows = true
            };

            var documentsResult = await documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

            // Transform documents to ProductDocumentMovementDto
            var movements = new List<ProductDocumentMovementDto>();
            foreach (var doc in documentsResult.Items)
            {
                if (doc.Rows is null) continue;

                // Find rows that contain this product
                var productRows = doc.Rows.Where(r => r.ProductId == id);
                foreach (var row in productRows)
                {
                    // Determine if this is a stock increase based on document type
                    bool isStockIncrease = doc.IsDocumentTypeStockIncrease;

                    movements.Add(new ProductDocumentMovementDto
                    {
                        DocumentHeaderId = doc.Id,
                        DocumentNumber = doc.Number,
                        DocumentDate = doc.Date,
                        DocumentTypeName = doc.DocumentTypeName ?? "Unknown",
                        BusinessPartyName = doc.BusinessPartyName ?? doc.CustomerName,
                        Status = doc.Status.ToString(),
                        Quantity = row.Quantity,
                        UnitOfMeasure = row.UnitOfMeasure,
                        UnitPrice = row.UnitPrice,
                        LineTotal = row.LineTotal,
                        IsStockIncrease = isStockIncrease,
                        WarehouseId = isStockIncrease ? doc.DestinationWarehouseId : doc.SourceWarehouseId,
                        WarehouseName = isStockIncrease ? doc.DestinationWarehouseName : doc.SourceWarehouseName
                    });
                }
            }

            var result = new PagedResult<ProductDocumentMovementDto>
            {
                Items = movements,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = documentsResult.TotalCount
            };

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document movements.", ex);
        }
    }

    /// <summary>
    /// Gets stock trend data for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="year">Year for trend data (defaults to current year)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stock trend data including data points and statistics</returns>
    /// <response code="200">Returns the stock trend data</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/stock-trend")]
    [ProducesResponseType(typeof(StockTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTrendDto>> GetProductStockTrend(
        Guid id,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            // Get stock movements for the year
            var movementsResult = await stockMovementService.GetMovementsAsync(
                page: 1,
                pageSize: 10000, // Get all movements for the year
                productId: id,
                fromDate: startDate,
                toDate: endDate,
                cancellationToken: cancellationToken);

            // Build data points - aggregate by day
            var dataPointsDict = new Dictionary<DateTime, (decimal Quantity, string? MovementType)>();
            var stockIncreasesList = new List<StockMovementPoint>();
            var stockDecreasesList = new List<StockMovementPoint>();
            decimal runningTotal = 0;

            // Order movements by date
            var orderedMovements = movementsResult.Items.OrderBy(m => m.MovementDate).ToList();

            foreach (var movement in orderedMovements)
            {
                var dateKey = movement.MovementDate.Date;

                // Update running total based on movement type
                if (movement.MovementType.Contains("Inbound", StringComparison.OrdinalIgnoreCase) ||
                    (movement.MovementType.Contains("Adjustment", StringComparison.OrdinalIgnoreCase) && movement.Quantity > 0))
                {
                    runningTotal += movement.Quantity;
                    stockIncreasesList.Add(new StockMovementPoint
                    {
                        Date = dateKey,
                        Quantity = movement.Quantity,
                        MovementType = movement.MovementType
                    });
                }
                else if (movement.MovementType.Contains("Outbound", StringComparison.OrdinalIgnoreCase) ||
                         (movement.MovementType.Contains("Adjustment", StringComparison.OrdinalIgnoreCase) && movement.Quantity < 0))
                {
                    runningTotal -= Math.Abs(movement.Quantity);
                    stockDecreasesList.Add(new StockMovementPoint
                    {
                        Date = dateKey,
                        Quantity = Math.Abs(movement.Quantity),
                        MovementType = movement.MovementType
                    });
                }

                dataPointsDict[dateKey] = (runningTotal, movement.MovementType);
            }

            var dataPoints = dataPointsDict
                .Select(kvp => new StockTrendDataPoint
                {
                    Date = kvp.Key,
                    Quantity = kvp.Value.Quantity,
                    MovementType = kvp.Value.MovementType
                })
                .OrderBy(dp => dp.Date)
                .ToList();

            // Calculate statistics
            var quantities = dataPoints.Select(dp => dp.Quantity).ToList();
            var currentStock = quantities.Any() ? quantities.Last() : 0;
            var minStock = quantities.Any() ? quantities.Min() : 0;
            var maxStock = quantities.Any() ? quantities.Max() : 0;
            var avgStock = quantities.Any() ? quantities.Average() : 0;

            var trendDto = new StockTrendDto
            {
                ProductId = id,
                Year = targetYear,
                DataPoints = dataPoints,
                StockIncreases = stockIncreasesList,
                StockDecreases = stockDecreasesList,
                CurrentStock = currentStock,
                MinStock = minStock,
                MaxStock = maxStock,
                AverageStock = avgStock
            };

            return Ok(trendDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock trend.", ex);
        }
    }

    /// <summary>
    /// Gets price trend data for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="year">Year for trend data (defaults to current year)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price trend data including purchase and sale prices with statistics</returns>
    /// <response code="200">Returns the price trend data</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/price-trend")]
    [ProducesResponseType(typeof(PriceTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceTrendDto>> GetProductPriceTrend(
        Guid id,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            // Get document movements for the year to extract price data
            var queryParameters = new Prym.DTOs.Documents.DocumentHeaderQueryParameters
            {
                Page = 1,
                PageSize = 10000, // Get all movements for the year
                ProductId = id,
                FromDate = startDate,
                ToDate = endDate,
                SortBy = "Date",
                SortDirection = "asc",
                IncludeRows = true
            };

            var documentsResult = await documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

            var purchasePricesList = new List<PriceTrendDataPoint>();
            var salePricesList = new List<PriceTrendDataPoint>();

            // Process document movements to extract price data
            foreach (var doc in documentsResult.Items)
            {
                if (doc.Rows is null) continue;

                // Find rows that contain this product
                var productRows = doc.Rows.Where(r => r.ProductId == id);

                foreach (var row in productRows)
                {
                    // Normalize unit price to base unit if available
                    decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;

                    // Weight quantity: prefer BaseQuantity if available
                    decimal weightQuantity = row.BaseQuantity ?? row.Quantity;

                    // Calculate discount per unit
                    decimal unitDiscount = 0m;
                    if (row.Quantity > 0)
                    {
                        if (row.DiscountType == Prym.DTOs.Common.DiscountType.Percentage)
                        {
                            unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
                        }
                        else // DiscountType.Value
                        {
                            unitDiscount = row.LineDiscountValue / row.Quantity;
                        }
                        // Clamp to prevent negative unit price
                        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
                    }

                    // Effective unit price after discount (net price)
                    // For purchase documents, this is considered VAT-exempt
                    decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

                    // Skip if effective price is zero or negative
                    if (effectiveUnitPrice <= 0) continue;

                    bool isStockIncrease = doc.IsDocumentTypeStockIncrease;

                    var pricePoint = new PriceTrendDataPoint
                    {
                        Date = doc.Date.Date,
                        Price = Math.Round(effectiveUnitPrice, 4),
                        Quantity = weightQuantity,
                        DocumentType = doc.DocumentTypeName,
                        BusinessPartyName = doc.BusinessPartyName ?? doc.CustomerName
                    };

                    if (isStockIncrease)
                    {
                        purchasePricesList.Add(pricePoint);
                    }
                    else
                    {
                        salePricesList.Add(pricePoint);
                    }
                }
            }

            // Calculate statistics for purchase prices
            var purchasePrices = purchasePricesList.Select(p => p.Price).Where(p => p > 0).ToList();
            var minPurchasePrice = purchasePrices.Any() ? purchasePrices.Min() : 0;
            var maxPurchasePrice = purchasePrices.Any() ? purchasePrices.Max() : 0;
            var avgPurchasePrice = purchasePrices.Any() ? purchasePrices.Average() : 0;

            // Calculate weighted average purchase price (by quantity)
            var totalPurchaseValue = purchasePricesList.Sum(p => p.Price * (p.Quantity ?? 0));
            var totalPurchaseQuantity = purchasePricesList.Sum(p => p.Quantity ?? 0);
            var currentAvgPurchasePrice = totalPurchaseQuantity > 0 ? totalPurchaseValue / totalPurchaseQuantity : 0;

            // Calculate statistics for sale prices
            var salePrices = salePricesList.Select(p => p.Price).Where(p => p > 0).ToList();
            var minSalePrice = salePrices.Any() ? salePrices.Min() : 0;
            var maxSalePrice = salePrices.Any() ? salePrices.Max() : 0;
            var avgSalePrice = salePrices.Any() ? salePrices.Average() : 0;

            // Calculate weighted average sale price (by quantity)
            var totalSaleValue = salePricesList.Sum(p => p.Price * (p.Quantity ?? 0));
            var totalSaleQuantity = salePricesList.Sum(p => p.Quantity ?? 0);
            var currentAvgSalePrice = totalSaleQuantity > 0 ? totalSaleValue / totalSaleQuantity : 0;

            var trendDto = new PriceTrendDto
            {
                ProductId = id,
                Year = targetYear,
                PurchasePrices = purchasePricesList.OrderBy(p => p.Date).ToList(),
                SalePrices = salePricesList.OrderBy(p => p.Date).ToList(),
                CurrentAveragePurchasePrice = currentAvgPurchasePrice,
                CurrentAverageSalePrice = currentAvgSalePrice,
                MinPurchasePrice = minPurchasePrice,
                MaxPurchasePrice = maxPurchasePrice,
                MinSalePrice = minSalePrice,
                MaxSalePrice = maxSalePrice,
                AveragePurchasePrice = avgPurchasePrice,
                AverageSalePrice = avgSalePrice
            };

            return Ok(trendDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price trend.", ex);
        }
    }

    /// <summary>
    /// Helper method to determine if a document type increases stock.
    /// This is a simplified implementation - adjust based on your DocumentType configuration.
    /// </summary>
    private bool DetermineStockIncrease(string? documentTypeName)
    {
        if (string.IsNullOrEmpty(documentTypeName))
            return false;

        // Common patterns for stock increase (purchases, receipts, returns from customers)
        var increaseKeywords = new[] { "purchase", "receipt", "return", "acquisto", "carico", "reso" };

        // Common patterns for stock decrease (sales, shipments, returns to suppliers)
        var decreaseKeywords = new[] { "sale", "invoice", "shipment", "delivery", "vendita", "fattura", "scarico", "consegna" };

        var lowerName = documentTypeName.ToLower();

        if (increaseKeywords.Any(k => lowerName.Contains(k)))
            return true;

        if (decreaseKeywords.Any(k => lowerName.Contains(k)))
            return false;

        // Default to false if uncertain
        return false;
    }

}
