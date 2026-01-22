using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using ProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;

namespace EventForge.Server.Services.PriceLists;

public class PriceListBulkOperationsService : IPriceListBulkOperationsService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PriceListBulkOperationsService> _logger;

    public PriceListBulkOperationsService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<PriceListBulkOperationsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new BulkImportResultDto
        {
            PriceListId = priceListId,
            ImportedBy = currentUser,
            ReplacedExisting = replaceExisting
        };

        try
        {
            // Verify price list exists
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (priceList == null)
            {
                result.Errors.Add(new BulkImportErrorDto
                {
                    RowIndex = 0,
                    ErrorCode = "PRICELIST_NOT_FOUND",
                    ErrorMessage = $"Price list {priceListId} not found"
                });
                result.FailureCount = 1;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var entriesList = entries.ToList();
            result.TotalProcessed = entriesList.Count;

            var rowIndex = 0;
            foreach (var entryDto in entriesList)
            {
                rowIndex++;

                try
                {
                    // Validate product exists
                    var productExists = await _context.Products
                        .AnyAsync(p => p.Id == entryDto.ProductId && !p.IsDeleted, cancellationToken);

                    if (!productExists)
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowIndex = rowIndex,
                            ProductId = entryDto.ProductId,
                            ErrorCode = "PRODUCT_NOT_FOUND",
                            ErrorMessage = $"Product {entryDto.ProductId} not found",
                            FieldName = nameof(entryDto.ProductId)
                        });
                        result.FailureCount++;
                        result.SkippedCount++;
                        continue;
                    }

                    // Check if entry already exists
                    var existingEntry = await _context.PriceListEntries
                        .FirstOrDefaultAsync(ple => ple.PriceListId == priceListId &&
                                                   ple.ProductId == entryDto.ProductId &&
                                                   !ple.IsDeleted,
                                                   cancellationToken);

                    if (existingEntry != null)
                    {
                        if (replaceExisting)
                        {
                            // Update existing entry
                            existingEntry.Price = entryDto.Price;
                            existingEntry.Currency = entryDto.Currency;
                            existingEntry.Score = entryDto.Score;
                            existingEntry.IsEditableInFrontend = entryDto.IsEditableInFrontend;
                            existingEntry.IsDiscountable = entryDto.IsDiscountable;
                            existingEntry.MinQuantity = entryDto.MinQuantity;
                            existingEntry.MaxQuantity = entryDto.MaxQuantity;
                            existingEntry.Notes = entryDto.Notes;
                            existingEntry.ModifiedAt = DateTime.UtcNow;
                            existingEntry.ModifiedBy = currentUser;

                            result.UpdatedCount++;
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.Warnings.Add(new BulkImportWarningDto
                            {
                                RowIndex = rowIndex,
                                ProductId = entryDto.ProductId,
                                WarningCode = "DUPLICATE_ENTRY",
                                WarningMessage = $"Entry for product {entryDto.ProductId} already exists",
                                ActionTaken = "Skipped"
                            });
                            result.SkippedCount++;
                            continue;
                        }
                    }
                    else
                    {
                        // Create new entry
                        var newEntry = new PriceListEntry
                        {
                            Id = Guid.NewGuid(),
                            PriceListId = priceListId,
                            ProductId = entryDto.ProductId,
                            Price = entryDto.Price,
                            Currency = entryDto.Currency,
                            Score = entryDto.Score,
                            IsEditableInFrontend = entryDto.IsEditableInFrontend,
                            IsDiscountable = entryDto.IsDiscountable,
                            Status = PriceListEntryStatus.Active,
                            MinQuantity = entryDto.MinQuantity,
                            MaxQuantity = entryDto.MaxQuantity,
                            Notes = entryDto.Notes,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUser
                        };

                        _ = _context.PriceListEntries.Add(newEntry);
                        result.CreatedCount++;
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing price list entry at row {RowIndex}", rowIndex);
                    result.Errors.Add(new BulkImportErrorDto
                    {
                        RowIndex = rowIndex,
                        ProductId = entryDto.ProductId,
                        ErrorCode = "IMPORT_ERROR",
                        ErrorMessage = ex.Message
                    });
                    result.FailureCount++;
                }
            }

            // Save all changes
            if (result.SuccessCount > 0)
            {
                _ = await _context.SaveChangesAsync(cancellationToken);

                _ = await _auditLogService.LogEntityChangeAsync(
                    "PriceList",
                    priceListId,
                    "BulkImport",
                    replaceExisting ? "BulkUpdate" : "BulkImport",
                    null,
                    $"Bulk import: {result.SuccessCount} entries imported/updated, {result.FailureCount} failed",
                    currentUser,
                    priceList.Name,
                    cancellationToken);
            }

            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Bulk import completed for price list {PriceListId}: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                priceListId, result.SuccessCount, result.FailureCount, result.SkippedCount, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk import for price list {PriceListId}", priceListId);
            result.Duration = DateTime.UtcNow - startTime;
            throw;
        }
    }

    public async Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get price list entries with related product information
            var query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product!)
                .ThenInclude(p => p.CategoryNode)
                .Include(ple => ple.Product!)
                .ThenInclude(p => p.Units.Where(pu => pu.UnitType == "Base" && !pu.IsDeleted))
                .ThenInclude(pu => pu.UnitOfMeasure)
                .AsQueryable();

            if (!includeInactiveEntries)
            {
                query = query.Where(ple => ple.Status == PriceListEntryStatus.Active);
            }

            var entries = await query.ToListAsync(cancellationToken);

            var exportableEntries = entries.Select(entry =>
            {
                var product = entry.Product;
                var baseUnit = product?.Units.FirstOrDefault();
                var unitOfMeasure = baseUnit?.UnitOfMeasure;

                return new ExportablePriceListEntryDto
                {
                    Id = entry.Id,
                    ProductId = entry.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    ProductCode = product?.Code,
                    ProductSku = product?.Code, // Product uses Code property
                    PriceListId = entry.PriceListId,
                    Price = entry.Price,
                    Currency = entry.Currency,
                    Score = entry.Score,
                    IsEditableInFrontend = entry.IsEditableInFrontend,
                    IsDiscountable = entry.IsDiscountable,
                    Status = entry.Status.ToString(),
                    MinQuantity = entry.MinQuantity,
                    MaxQuantity = entry.MaxQuantity,
                    Notes = entry.Notes,
                    CreatedAt = entry.CreatedAt,
                    CreatedBy = entry.CreatedBy,
                    ModifiedAt = entry.ModifiedAt,
                    ModifiedBy = entry.ModifiedBy,
                    IsActive = entry.Status == PriceListEntryStatus.Active,
                    ProductCategory = product?.CategoryNode?.Name,
                    UnitOfMeasure = unitOfMeasure?.Symbol,
                    ProductDefaultPrice = product?.DefaultPrice
                };
            }).ToList();

            _logger.LogInformation("Exported {Count} price list entries from price list {PriceListId}",
                exportableEntries.Count, priceListId);

            return exportableEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting price list entries for price list {PriceListId}", priceListId);
            throw;
        }
    }

    public async Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new PrecedenceValidationResultDto
        {
            EventId = eventId
        };

        try
        {
            // Get all price lists for the event
            var priceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .ToListAsync(cancellationToken);

            result.TotalPriceListsValidated = priceLists.Count;

            if (priceLists.Count == 0)
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.NoPriceListsFound,
                    Severity = ValidationSeverity.Critical,
                    Description = "No price lists found for this event",
                    SuggestedResolution = "Create at least one price list for the event",
                    Impact = "Products cannot be priced for this event"
                });
                result.ValidationDuration = DateTime.UtcNow - startTime;
                return result;
            }

            var activePriceLists = priceLists.Where(pl => pl.Status == PriceListStatus.Active).ToList();
            var defaultPriceLists = priceLists.Where(pl => pl.IsDefault).ToList();
            var now = DateTime.UtcNow;
            var expiredPriceLists = priceLists.Where(pl => pl.ValidTo.HasValue && pl.ValidTo.Value < now).ToList();

            result.ActivePriceListsCount = activePriceLists.Count;
            result.DefaultPriceListsCount = defaultPriceLists.Count;
            result.ExpiredPriceListsCount = expiredPriceLists.Count;

            // Validation 1: Check for multiple default price lists
            if (defaultPriceLists.Count > 1)
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.MultipleDefaultPriceLists,
                    Severity = ValidationSeverity.High,
                    Description = $"Multiple default price lists found ({defaultPriceLists.Count})",
                    AffectedPriceListIds = defaultPriceLists.Select(pl => pl.Id).ToList(),
                    AffectedPriceListNames = defaultPriceLists.Select(pl => pl.Name).ToList(),
                    SuggestedResolution = "Set only one price list as default",
                    Impact = "Ambiguous default price selection may cause inconsistent pricing"
                });
            }

            // Validation 2: Check for no default price list (warning)
            if (defaultPriceLists.Count == 0 && activePriceLists.Count > 0)
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.UnusualPriorityRange,
                    Description = "No default price list found",
                    AffectedPriceListIds = activePriceLists.Select(pl => pl.Id).ToList(),
                    Recommendation = "Consider setting one price list as default for fallback pricing"
                });
            }

            // Validation 3: Check for conflicting priorities (same priority values)
            var priorityGroups = activePriceLists.GroupBy(pl => pl.Priority).Where(g => g.Count() > 1).ToList();
            if (priorityGroups.Any())
            {
                foreach (var group in priorityGroups)
                {
                    result.Warnings.Add(new PrecedenceValidationWarningDto
                    {
                        WarningType = PrecedenceWarningType.DuplicatePriorities,
                        Description = $"Multiple price lists have priority {group.Key}",
                        AffectedPriceListIds = group.Select(pl => pl.Id).ToList(),
                        Recommendation = "Consider assigning unique priority values for clearer precedence"
                    });
                }
            }

            // Validation 4: Check if only expired price lists exist
            if (activePriceLists.Count > 0 && activePriceLists.All(pl => pl.ValidTo.HasValue && pl.ValidTo.Value < now))
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.ExpiredPriceListsOnly,
                    Severity = ValidationSeverity.Critical,
                    Description = "All active price lists have expired",
                    AffectedPriceListIds = activePriceLists.Select(pl => pl.Id).ToList(),
                    AffectedPriceListNames = activePriceLists.Select(pl => pl.Name).ToList(),
                    SuggestedResolution = "Extend validity dates or create new price lists",
                    Impact = "No valid prices available for current date"
                });
            }

            // Validation 5: Check for price lists expiring soon (within 7 days)
            var soonToExpire = activePriceLists
                .Where(pl => pl.ValidTo.HasValue &&
                           pl.ValidTo.Value >= now &&
                           pl.ValidTo.Value <= now.AddDays(7))
                .ToList();

            if (soonToExpire.Any())
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.SoonToExpire,
                    Description = $"{soonToExpire.Count} price list(s) expiring within 7 days",
                    AffectedPriceListIds = soonToExpire.Select(pl => pl.Id).ToList(),
                    Recommendation = "Review and extend validity dates or prepare replacement price lists"
                });
            }

            // Validation 6: Check for too many active price lists (warning only)
            if (activePriceLists.Count > 10)
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.ManyActivePriceLists,
                    Description = $"Large number of active price lists ({activePriceLists.Count})",
                    Recommendation = "Consider consolidating or archiving unused price lists for better performance"
                });
            }

            // Validation 7: Check for overlapping validity periods with same priority
            foreach (var group in priorityGroups)
            {
                var sortedByDate = group.OrderBy(pl => pl.ValidFrom ?? DateTime.MinValue).ToList();
                for (int i = 0; i < sortedByDate.Count - 1; i++)
                {
                    var current = sortedByDate[i];
                    var next = sortedByDate[i + 1];

                    var currentEnd = current.ValidTo ?? DateTime.MaxValue;
                    var nextStart = next.ValidFrom ?? DateTime.MinValue;

                    if (currentEnd >= nextStart)
                    {
                        result.Issues.Add(new PrecedenceValidationIssueDto
                        {
                            IssueType = PrecedenceIssueType.OverlappingValidityPeriods,
                            Severity = ValidationSeverity.Medium,
                            Description = $"Price lists '{current.Name}' and '{next.Name}' have overlapping validity periods with same priority",
                            AffectedPriceListIds = new List<Guid> { current.Id, next.Id },
                            AffectedPriceListNames = new List<string> { current.Name, next.Name },
                            SuggestedResolution = "Adjust validity dates or priorities to avoid ambiguity",
                            Impact = "Ambiguous price selection during overlap period"
                        });
                        result.IsValid = false;
                    }
                }
            }

            // Set recommended default price list
            if (defaultPriceLists.Count == 1)
            {
                result.RecommendedDefaultPriceListId = defaultPriceLists[0].Id;
                result.RecommendedDefaultPriceListName = defaultPriceLists[0].Name;
            }
            else if (activePriceLists.Any())
            {
                // Recommend the highest priority (lowest number) active price list
                var recommended = activePriceLists.OrderBy(pl => pl.Priority).First();
                result.RecommendedDefaultPriceListId = recommended.Id;
                result.RecommendedDefaultPriceListName = recommended.Name;
            }

            result.ValidationDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Precedence validation completed for event {EventId}: {IsValid}, {IssueCount} issues, {WarningCount} warnings in {Duration}ms",
                eventId, result.IsValid, result.Issues.Count, result.Warnings.Count, result.ValidationDuration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price list precedence for event {EventId}", eventId);
            result.ValidationDuration = DateTime.UtcNow - startTime;
            throw;
        }
    }

    #region Bulk Price Update Methods

    /// <summary>
    /// Anteprima aggiornamento massivo prezzi
    /// </summary>
    public async Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica esistenza listino
            var priceListExists = await _context.PriceLists
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (!priceListExists)
            {
                throw new InvalidOperationException($"Price list {priceListId} not found.");
            }

            // Query base per gli items del listino
            IQueryable<PriceListEntry> query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product);

            // Applica filtri
            query = ApplyBulkUpdateFilters(query, dto);

            // Recupera items
            var items = await query.ToListAsync(cancellationToken);

            var changes = new List<PriceChangePreview>();
            decimal totalCurrentValue = 0;
            decimal totalNewValue = 0;

            foreach (var item in items)
            {
                var currentPrice = item.Price;
                var newPrice = CalculateNewPrice(currentPrice, dto.Operation, dto.Value);
                newPrice = ApplyRounding(newPrice, dto.RoundingStrategy);

                // Assicura che il prezzo non sia negativo
                if (newPrice < 0)
                    newPrice = 0;

                var changeAmount = newPrice - currentPrice;
                var changePercentage = currentPrice != 0 
                    ? (changeAmount / currentPrice) * 100 
                    : 0;

                changes.Add(new PriceChangePreview
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Unknown",
                    ProductCode = item.Product?.Code,
                    CurrentPrice = currentPrice,
                    NewPrice = newPrice,
                    ChangeAmount = changeAmount,
                    ChangePercentage = changePercentage
                });

                totalCurrentValue += currentPrice;
                totalNewValue += newPrice;
            }

            var averageIncreasePercentage = totalCurrentValue != 0
                ? ((totalNewValue - totalCurrentValue) / totalCurrentValue) * 100
                : 0;

            return new BulkUpdatePreviewDto
            {
                AffectedCount = changes.Count,
                Changes = changes,
                TotalCurrentValue = totalCurrentValue,
                TotalNewValue = totalNewValue,
                AverageIncreasePercentage = averageIncreasePercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing bulk price update for price list {PriceListId}", priceListId);
            throw;
        }
    }

    /// <summary>
    /// Esegue aggiornamento massivo prezzi
    /// </summary>
    public async Task<BulkUpdateResultDto> BulkUpdatePricesAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica esistenza listino
            var priceListExists = await _context.PriceLists
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (!priceListExists)
            {
                throw new InvalidOperationException($"Price list {priceListId} not found.");
            }

            // Query base per gli items del listino
            IQueryable<PriceListEntry> query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product);

            // Applica filtri
            query = ApplyBulkUpdateFilters(query, dto);

            // Recupera items
            var items = await query.ToListAsync(cancellationToken);

            var result = new BulkUpdateResultDto
            {
                UpdatedAt = DateTime.UtcNow,
                Errors = new List<string>()
            };

            // Aggiorna prezzi
            foreach (var item in items)
            {
                try
                {
                    var currentPrice = item.Price;
                    var newPrice = CalculateNewPrice(currentPrice, dto.Operation, dto.Value);
                    newPrice = ApplyRounding(newPrice, dto.RoundingStrategy);

                    // Assicura che il prezzo non sia negativo
                    if (newPrice < 0)
                    {
                        result.Errors.Add($"Product {item.Product?.Name ?? item.ProductId.ToString()}: Calculated price is negative, skipping.");
                        result.FailedCount++;
                        continue;
                    }

                    item.Price = newPrice;
                    item.ModifiedBy = currentUser;
                    item.ModifiedAt = DateTime.UtcNow;

                    result.UpdatedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Product {item.Product?.Name ?? item.ProductId.ToString()}: {ex.Message}");
                    result.FailedCount++;
                    _logger.LogError(ex, "Error updating price for product {ProductId} in price list {PriceListId}", 
                        item.ProductId, priceListId);
                }
            }

            // Salva modifiche in una transazione
            if (result.UpdatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);

                // Audit log per l'operazione bulk
                await _auditLogService.LogEntityChangeAsync(
                    "PriceList",
                    priceListId,
                    "BulkUpdate",
                    "BulkUpdate",
                    null,
                    $"Operation: {dto.Operation}, Value: {dto.Value}, Updated: {result.UpdatedCount}, Failed: {result.FailedCount}",
                    currentUser,
                    null,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk price update for price list {PriceListId}", priceListId);
            throw;
        }
    }

    /// <summary>
    /// Applica i filtri alla query per il bulk update.
    /// </summary>
    private IQueryable<PriceListEntry> ApplyBulkUpdateFilters(
        IQueryable<PriceListEntry> query,
        BulkPriceUpdateDto dto)
    {
        // Filtro per ProductIds specifici
        if (dto.ProductIds != null && dto.ProductIds.Any())
        {
            query = query.Where(ple => dto.ProductIds.Contains(ple.ProductId));
        }

        // Filtro per CategoryIds
        if (dto.CategoryIds != null && dto.CategoryIds.Any())
        {
            query = query.Where(ple => ple.Product != null && 
                dto.CategoryIds.Contains(ple.Product.CategoryNodeId!.Value));
        }

        // Filtro per BrandIds
        if (dto.BrandIds != null && dto.BrandIds.Any())
        {
            query = query.Where(ple => ple.Product != null && 
                ple.Product.BrandId != null &&
                dto.BrandIds.Contains(ple.Product.BrandId.Value));
        }

        // Filtro per MinPrice
        if (dto.MinPrice.HasValue)
        {
            query = query.Where(ple => ple.Price >= dto.MinPrice.Value);
        }

        // Filtro per MaxPrice
        if (dto.MaxPrice.HasValue)
        {
            query = query.Where(ple => ple.Price <= dto.MaxPrice.Value);
        }

        return query;
    }

    /// <summary>
    /// Applica la strategia di arrotondamento al prezzo.
    /// </summary>
    private static decimal ApplyRounding(decimal value, EventForge.DTOs.Common.RoundingStrategy strategy)
    {
        return strategy switch
        {
            EventForge.DTOs.Common.RoundingStrategy.ToNearest5Cents =>
                Math.Round(value * 20, MidpointRounding.AwayFromZero) / 20m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearest10Cents =>
                Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearest50Cents =>
                Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearestEuro =>
                Math.Round(value, MidpointRounding.AwayFromZero),

            EventForge.DTOs.Common.RoundingStrategy.ToNearest99Cents =>
                Math.Floor(value) + 0.99m,

            _ => value
        };
    }

    /// <summary>
    /// Calcola il nuovo prezzo in base all'operazione e al valore.
    /// </summary>
    private static decimal CalculateNewPrice(decimal currentPrice, EventForge.DTOs.Common.BulkUpdateOperation operation, decimal value)
    {
        return operation switch
        {
            EventForge.DTOs.Common.BulkUpdateOperation.IncreaseByPercentage => currentPrice * (1 + value / 100),
            EventForge.DTOs.Common.BulkUpdateOperation.DecreaseByPercentage => currentPrice * (1 - value / 100),
            EventForge.DTOs.Common.BulkUpdateOperation.IncreaseByAmount => currentPrice + value,
            EventForge.DTOs.Common.BulkUpdateOperation.DecreaseByAmount => currentPrice - value,
            EventForge.DTOs.Common.BulkUpdateOperation.SetFixedPrice => value,
            EventForge.DTOs.Common.BulkUpdateOperation.MultiplyBy => currentPrice * value,
            _ => currentPrice
        };
    }

    #endregion

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice
    /// </summary>
    public async Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(
        ApplyPriceListToProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Validazione
        if (dto.OnlyUpdateIfHigher && dto.OnlyUpdateIfLower)
        {
            throw new InvalidOperationException("OnlyUpdateIfHigher e OnlyUpdateIfLower non possono essere entrambi true");
        }

        // 2. Carica listino con entries per ottenere TenantId
        var priceList = await _context.PriceLists
            .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId && !pl.IsDeleted, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        var result = new ApplyPriceListResultDto
        {
            PriceListId = priceList.Id,
            PriceListName = priceList.Name,
            AppliedAt = DateTime.UtcNow,
            AppliedBy = currentUser,
            UpdateDetails = new List<ProductPriceUpdateDetail>()
        };

        var updatedCount = 0;
        var skippedCount = 0;
        var notFoundCount = 0;

        // 3. Per ogni entry del listino
        foreach (var entry in priceList.ProductPrices)
        {
            if (entry.Status != PriceListEntryStatus.Active)
                continue;

            // Applica filtri se specificati
            if (dto.FilterByProductIds != null && dto.FilterByProductIds.Any() && 
                !dto.FilterByProductIds.Contains(entry.ProductId))
            {
                continue;
            }

            // Carica prodotto
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == entry.ProductId && p.TenantId == tenantId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                notFoundCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = entry.ProductId,
                    ProductName = "Unknown",
                    ProductCode = "Unknown",
                    OldPrice = 0,
                    NewPrice = entry.Price,
                    UpdateReason = "Not Found"
                });
                continue;
            }

            // Applica filtro categorie
            if (dto.FilterByCategoryIds != null && dto.FilterByCategoryIds.Any() &&
                (!product.CategoryNodeId.HasValue || !dto.FilterByCategoryIds.Contains(product.CategoryNodeId.Value)))
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = product.DefaultPrice ?? 0,
                    NewPrice = entry.Price,
                    UpdateReason = "Skipped - Category Filter"
                });
                continue;
            }

            var oldPrice = product.DefaultPrice ?? 0;
            var newPrice = entry.Price;

            // Verifica condizioni OnlyUpdateIfHigher/Lower
            if (dto.OnlyUpdateIfHigher && newPrice <= oldPrice)
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    UpdateReason = "Skipped - Not Higher"
                });
                continue;
            }

            if (dto.OnlyUpdateIfLower && newPrice >= oldPrice)
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    UpdateReason = "Skipped - Not Lower"
                });
                continue;
            }

            // Backup prezzo se richiesto
            if (dto.CreateBackup)
            {
                await _auditLogService.LogEntityChangeAsync(
                    "Product",
                    product.Id,
                    "DefaultPrice",
                    "ApplyPriceList",
                    oldPrice.ToString("F2"),
                    newPrice.ToString("F2"),
                    currentUser,
                    product.Name,
                    cancellationToken);
            }

            // Aggiorna Product.DefaultPrice
            product.DefaultPrice = newPrice;
            // Non aggiorniamo ModifiedAt e ModifiedBy come da requisiti

            updatedCount++;
            result.UpdateDetails.Add(new ProductPriceUpdateDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                UpdateReason = "Updated"
            });
        }

        // 5. Salva modifiche
        await _context.SaveChangesAsync(cancellationToken);

        // Aggiorna risultato con i contatori finali
        var finalResult = new ApplyPriceListResultDto
        {
            PriceListId = result.PriceListId,
            PriceListName = result.PriceListName,
            ProductsUpdated = updatedCount,
            ProductsSkipped = skippedCount,
            ProductsNotFound = notFoundCount,
            UpdateDetails = result.UpdateDetails,
            AppliedAt = result.AppliedAt,
            AppliedBy = result.AppliedBy
        };

        // Log applicazione listino
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Action",
            "ApplyToProducts",
            null,
            $"Applied to {updatedCount} products, skipped {skippedCount}, not found {notFoundCount}",
            currentUser,
            priceList.Name,
            cancellationToken);

        return finalResult;
    }
}
