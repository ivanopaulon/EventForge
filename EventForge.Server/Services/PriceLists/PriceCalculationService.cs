using EventForge.DTOs.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using ProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;

namespace EventForge.Server.Services.PriceLists;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly EventForgeDbContext _context;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<PriceCalculationService> _logger;

    public PriceCalculationService(
        EventForgeDbContext context,
        IUnitConversionService unitConversionService,
        ILogger<PriceCalculationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitConversionService = unitConversionService ?? throw new ArgumentNullException(nameof(unitConversionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppliedPriceDto?> GetAppliedPriceAsync(
        Guid productId,
        Guid eventId,
        Guid? businessPartyId = null,
        DateTime? evaluationDate = null,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evalDate = evaluationDate ?? DateTime.UtcNow;

            // Step 1: Trova listini vendita applicabili
            var query = _context.PriceLists
                .Where(pl => pl.Type == PriceListType.Sales &&
                             pl.Direction == PriceListDirection.Output &&
                             !pl.IsDeleted &&
                             pl.Status == PriceListStatus.Active)
                .Where(pl => pl.EventId == eventId)
                .Where(pl => (pl.ValidFrom == null || pl.ValidFrom <= evalDate) &&
                             (pl.ValidTo == null || pl.ValidTo >= evalDate))
                .Include(pl => pl.BusinessParties.Where(bp =>
                    !bp.IsDeleted &&
                    bp.Status == PriceListBusinessPartyStatus.Active))
                .ThenInclude(bp => bp.BusinessParty)
                .Include(pl => pl.ProductPrices.Where(ple =>
                    ple.ProductId == productId &&
                    !ple.IsDeleted &&
                    ple.Status == PriceListEntryStatus.Active &&
                    ple.MinQuantity <= quantity &&
                    (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity)));

            var applicablePriceLists = await query.ToListAsync(cancellationToken);

            // Step 2: Filtra per BusinessParty se specificato
            if (businessPartyId.HasValue)
            {
                applicablePriceLists = applicablePriceLists
                    .Where(pl =>
                        // Listino generico (senza BusinessParty assegnati)
                        !pl.BusinessParties.Any() ||
                        // Oppure listino assegnato a questo BusinessParty
                        pl.BusinessParties.Any(bp =>
                            bp.BusinessPartyId == businessPartyId.Value &&
                            (!bp.SpecificValidFrom.HasValue || bp.SpecificValidFrom.Value <= evalDate) &&
                            (!bp.SpecificValidTo.HasValue || bp.SpecificValidTo.Value >= evalDate)))
                    .ToList();
            }
            else
            {
                // Solo listini generici (senza BusinessParty)
                applicablePriceLists = applicablePriceLists
                    .Where(pl => !pl.BusinessParties.Any())
                    .ToList();
            }

            // Step 3: Ordina per precedenza
            var orderedPriceList = applicablePriceLists
                .SelectMany(pl => pl.ProductPrices.Select(ple => new
                {
                    PriceList = pl,
                    Entry = ple,
                    BusinessPartyRel = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                        : null,
                    EffectivePriority = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                            ?.OverridePriority ?? pl.Priority
                        : pl.Priority,
                    GlobalDiscount = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                            ?.GlobalDiscountPercentage
                        : null
                }))
                .OrderBy(x => x.EffectivePriority)  // Priorità più bassa = più importante
                .ThenBy(x => x.PriceList.IsDefault ? 0 : 1)
                .ThenByDescending(x => x.PriceList.CreatedAt)
                .FirstOrDefault();

            if (orderedPriceList == null)
                return null;

            // Step 4: Calcola prezzo finale con sconto globale
            var finalPrice = orderedPriceList.Entry.Price;
            if (orderedPriceList.GlobalDiscount.HasValue)
            {
                finalPrice *= (1 - orderedPriceList.GlobalDiscount.Value / 100m);
            }

            return new AppliedPriceDto
            {
                ProductId = productId,
                EventId = eventId,
                Price = finalPrice,
                OriginalPrice = orderedPriceList.Entry.Price,
                Currency = orderedPriceList.Entry.Currency,
                PriceListId = orderedPriceList.PriceList.Id,
                PriceListName = orderedPriceList.PriceList.Name,
                PriceListPriority = orderedPriceList.EffectivePriority,
                MinQuantity = orderedPriceList.Entry.MinQuantity,
                MaxQuantity = orderedPriceList.Entry.MaxQuantity,
                IsEditableInFrontend = orderedPriceList.Entry.IsEditableInFrontend,
                IsDiscountable = orderedPriceList.Entry.IsDiscountable,
                Score = orderedPriceList.Entry.Score,
                CalculatedAt = DateTime.UtcNow,
                BusinessPartyId = orderedPriceList.BusinessPartyRel?.BusinessPartyId,
                BusinessPartyName = orderedPriceList.BusinessPartyRel?.BusinessParty?.Name,
                AppliedDiscountPercentage = orderedPriceList.GlobalDiscount,
                UnitOfMeasureId = Guid.Empty, // TODO: Gestire UnitOfMeasure se necessario
                UnitOfMeasureName = string.Empty,
                UnitSymbol = string.Empty,
                CalculationNotes = orderedPriceList.GlobalDiscount.HasValue
                    ? $"Applied {orderedPriceList.GlobalDiscount.Value:F2}% global discount for {orderedPriceList.BusinessPartyRel?.BusinessParty?.Name}"
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating applied price for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, Guid? businessPartyId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // First get the base applied price
            var basePrice = await GetAppliedPriceAsync(productId, eventId, null, evaluationDate, quantity, cancellationToken);
            if (basePrice == null)
            {
                return null;
            }

            // Get the target unit information
            var targetProductUnit = await _context.ProductUnits
                .Include(pu => pu.UnitOfMeasure)
                .FirstOrDefaultAsync(pu => pu.ProductId == productId &&
                                         pu.UnitOfMeasureId == targetUnitId &&
                                         !pu.IsDeleted &&
                                         pu.Status == ProductUnitStatus.Active,
                                         cancellationToken);

            if (targetProductUnit == null)
            {
                _logger.LogWarning("Target unit {TargetUnitId} not found for product {ProductId}", targetUnitId, productId);
                return basePrice; // Return base price if target unit not found
            }

            // Get the base unit information for conversion
            var baseProductUnit = await _context.ProductUnits
                .Include(pu => pu.UnitOfMeasure)
                .FirstOrDefaultAsync(pu => pu.ProductId == productId &&
                                         pu.UnitOfMeasureId == basePrice.UnitOfMeasureId &&
                                         !pu.IsDeleted &&
                                         pu.Status == ProductUnitStatus.Active,
                                         cancellationToken);

            if (baseProductUnit == null)
            {
                _logger.LogWarning("Base unit {BaseUnitId} not found for product {ProductId}", basePrice.UnitOfMeasureId, productId);
                return basePrice; // Return base price if conversion not possible
            }

            // Perform price conversion using the unit conversion service
            var convertedPrice = _unitConversionService.ConvertPrice(
                basePrice.Price,
                baseProductUnit.ConversionFactor,
                targetProductUnit.ConversionFactor,
                2); // 2 decimal places for currency

            // Create the result with conversion information
            var result = new AppliedPriceDto
            {
                ProductId = basePrice.ProductId,
                EventId = basePrice.EventId,
                Price = convertedPrice,
                Currency = basePrice.Currency,
                UnitOfMeasureId = targetUnitId,
                UnitOfMeasureName = targetProductUnit.UnitOfMeasure?.Name ?? "Unknown",
                UnitSymbol = targetProductUnit.UnitOfMeasure?.Symbol ?? "?",
                ConversionFactor = targetProductUnit.ConversionFactor,
                OriginalPrice = basePrice.Price,
                OriginalUnitOfMeasureId = basePrice.UnitOfMeasureId,
                PriceListId = basePrice.PriceListId,
                PriceListName = basePrice.PriceListName,
                PriceListPriority = basePrice.PriceListPriority,
                MinQuantity = basePrice.MinQuantity,
                MaxQuantity = basePrice.MaxQuantity,
                CalculatedAt = DateTime.UtcNow,
                IsEditableInFrontend = basePrice.IsEditableInFrontend,
                IsDiscountable = basePrice.IsDiscountable,
                Score = basePrice.Score,
                CalculationNotes = $"Price converted from {basePrice.UnitOfMeasureName} (factor: {baseProductUnit.ConversionFactor}) to {targetProductUnit.UnitOfMeasure?.Name} (factor: {targetProductUnit.ConversionFactor}). Original: {basePrice.Price:F2} {basePrice.Currency}"
            };

            _logger.LogInformation("Converted price from {OriginalPrice} {Currency}/{OriginalUnit} to {ConvertedPrice} {Currency}/{TargetUnit} for product {ProductId}",
                basePrice.Price, basePrice.Currency, basePrice.UnitOfMeasureName,
                convertedPrice, result.Currency, result.UnitOfMeasureName, productId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating applied price with unit conversion for product {ProductId} in event {EventId} to unit {TargetUnitId}",
                productId, eventId, targetUnitId);
            throw;
        }
    }

    // Enhanced methods implementation (Issue #245)
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddYears(-1); // Default to last year
            var to = toDate ?? DateTime.UtcNow;

            // Get all price lists for the event
            var priceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => ple.ProductId == productId && !ple.IsDeleted))
                .ToListAsync(cancellationToken);

            var historyEntries = new List<PriceHistoryDto>();

            foreach (var priceList in priceLists)
            {
                foreach (var entry in priceList.ProductPrices)
                {
                    // Determine effective date range
                    var effectiveFrom = priceList.ValidFrom ?? entry.CreatedAt;
                    var effectiveTo = priceList.ValidTo;

                    // Skip entries outside the requested date range
                    if (effectiveTo.HasValue && effectiveTo.Value < from)
                        continue;
                    if (effectiveFrom > to)
                        continue;

                    var wasActive = priceList.Status == PriceListStatus.Active &&
                                   entry.Status == PriceListEntryStatus.Active &&
                                   (!priceList.ValidFrom.HasValue || priceList.ValidFrom.Value <= to) &&
                                   (!priceList.ValidTo.HasValue || priceList.ValidTo.Value >= from);

                    historyEntries.Add(new PriceHistoryDto
                    {
                        ProductId = productId,
                        EventId = eventId,
                        PriceListId = priceList.Id,
                        PriceListName = priceList.Name,
                        Price = entry.Price,
                        Currency = entry.Currency,
                        EffectiveFrom = effectiveFrom,
                        EffectiveTo = effectiveTo,
                        Priority = priceList.Priority,
                        IsDefault = priceList.IsDefault,
                        CreatedAt = entry.CreatedAt,
                        CreatedBy = entry.CreatedBy,
                        ModifiedAt = entry.ModifiedAt,
                        ModifiedBy = entry.ModifiedBy,
                        MinQuantity = entry.MinQuantity,
                        MaxQuantity = entry.MaxQuantity,
                        WasActive = wasActive,
                        Notes = entry.Notes
                    });
                }
            }

            // Order by effective date descending, then by priority
            var result = historyEntries
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenBy(h => h.Priority)
                .ToList();

            _logger.LogInformation("Retrieved {Count} price history entries for product {ProductId} in event {EventId}",
                result.Count, productId, eventId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for product {ProductId} in event {EventId}",
                productId, eventId);
            throw;
        }
    }

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// </summary>
    public async Task<ProductPriceResultDto> GetProductPriceAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Recupera il prodotto
            var product = await _context.Products
                .Include(p => p.Codes)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID {request.ProductId} not found");
            }

            // 2. Determina la modalità di applicazione
            var mode = await DeterminePriceApplicationModeAsync(request, cancellationToken);

            // 3. Applica la strategia corretta
            return mode switch
            {
                PriceApplicationMode.Manual => await ApplyManualPriceAsync(request, product, cancellationToken),
                PriceApplicationMode.ForcedPriceList => await ApplyForcedPriceListAsync(request, product, cancellationToken),
                PriceApplicationMode.HybridForcedWithOverrides => await ApplyHybridPriceAsync(request, product, cancellationToken),
                PriceApplicationMode.Automatic => await ApplyAutomaticPriceAsync(request, product, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown price application mode: {mode}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating product price for ProductId {ProductId}", request.ProductId);
            throw;
        }
    }

    #region Purchase Price Comparison

    public async Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(
        Guid productId,
        int quantity = 1,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evalDate = evaluationDate ?? DateTime.UtcNow;

            // Trova tutti i listini acquisto applicabili
            var purchasePriceLists = await _context.PriceLists
                .Where(pl => pl.Type == PriceListType.Purchase &&
                             pl.Direction == PriceListDirection.Input &&
                             !pl.IsDeleted &&
                             pl.Status == PriceListStatus.Active)
                .Where(pl => (pl.ValidFrom == null || pl.ValidFrom <= evalDate) &&
                             (pl.ValidTo == null || pl.ValidTo >= evalDate))
                .Include(pl => pl.BusinessParties.Where(bp =>
                    !bp.IsDeleted &&
                    bp.Status == PriceListBusinessPartyStatus.Active &&
                    (bp.SpecificValidFrom == null || bp.SpecificValidFrom <= evalDate) &&
                    (bp.SpecificValidTo == null || bp.SpecificValidTo >= evalDate)))
                .ThenInclude(bp => bp.BusinessParty)
                .Include(pl => pl.ProductPrices.Where(ple =>
                    ple.ProductId == productId &&
                    !ple.IsDeleted &&
                    ple.Status == PriceListEntryStatus.Active &&
                    ple.MinQuantity <= quantity &&
                    (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity)))
                .ToListAsync(cancellationToken);

            var comparisons = new List<PurchasePriceComparisonDto>();

            foreach (var priceList in purchasePriceLists)
            {
                var entry = priceList.ProductPrices.FirstOrDefault();
                if (entry == null) continue;

                // Se non ci sono BusinessParty assegnati, salta questo listino
                if (!priceList.BusinessParties.Any()) continue;

                foreach (var businessPartyRel in priceList.BusinessParties)
                {
                    var effectivePrice = entry.Price;
                    decimal? discountPercentage = null;

                    // Applica sconto globale se presente
                    if (businessPartyRel.GlobalDiscountPercentage.HasValue)
                    {
                        discountPercentage = businessPartyRel.GlobalDiscountPercentage.Value;
                        effectivePrice *= (1 - discountPercentage.Value / 100m);
                    }

                    comparisons.Add(new PurchasePriceComparisonDto
                    {
                        ProductId = productId,
                        SupplierId = businessPartyRel.BusinessPartyId,
                        SupplierName = businessPartyRel.BusinessParty?.Name ?? "Unknown",
                        PriceListId = priceList.Id,
                        PriceListName = priceList.Name,
                        Price = effectivePrice,
                        OriginalPrice = entry.Price,
                        Currency = entry.Currency,
                        LeadTimeDays = entry.LeadTimeDays,
                        MinimumOrderQuantity = entry.MinimumOrderQuantity,
                        QuantityIncrement = entry.QuantityIncrement,
                        SupplierProductCode = entry.SupplierProductCode,
                        IsPrimarySupplier = businessPartyRel.IsPrimary,
                        Priority = businessPartyRel.OverridePriority ?? priceList.Priority,
                        AppliedDiscountPercentage = discountPercentage
                    });
                }
            }

            // Ordina per prezzo (migliore prima)
            return comparisons.OrderBy(c => c.Price).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing purchase prices for product {ProductId}", productId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Determina quale modalità usare per il calcolo del prezzo.
    /// </summary>
    private async Task<PriceApplicationMode> DeterminePriceApplicationModeAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken)
    {
        // 1. Override esplicito nella richiesta → priorità massima
        if (request.PriceApplicationMode.HasValue)
        {
            _logger.LogDebug("Using explicit price application mode from request: {Mode}", request.PriceApplicationMode.Value);
            return request.PriceApplicationMode.Value;
        }

        // 2. Nessun BusinessParty → Automatic
        if (!request.BusinessPartyId.HasValue)
        {
            _logger.LogDebug("No BusinessParty specified, using Automatic mode");
            return PriceApplicationMode.Automatic;
        }

        // 3. Configurazione del BusinessParty
        var businessParty = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == request.BusinessPartyId.Value && !bp.IsDeleted, cancellationToken);

        if (businessParty == null)
        {
            _logger.LogWarning("BusinessParty {BusinessPartyId} not found, using Automatic mode", request.BusinessPartyId.Value);
            return PriceApplicationMode.Automatic;
        }

        _logger.LogDebug("Using BusinessParty default price application mode: {Mode}", businessParty.DefaultPriceApplicationMode);
        return businessParty.DefaultPriceApplicationMode;
    }

    /// <summary>
    /// Gestisce modalità Manual: usa il prezzo manuale fornito.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyManualPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        if (!request.ManualPrice.HasValue || request.ManualPrice.Value <= 0)
        {
            throw new InvalidOperationException("ManualPrice is required and must be greater than 0 when using Manual mode");
        }

        _logger.LogInformation("Applying manual price {Price} for product {ProductId}", request.ManualPrice.Value, product.Id);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = request.ManualPrice.Value,
            Currency = "EUR",
            AppliedMode = PriceApplicationMode.Manual,
            IsManual = true,
            IsPriceListForced = false,
            SearchPath = new List<string> { "Manual price specified in request" }
        };
    }

    /// <summary>
    /// Gestisce modalità ForcedPriceList: cerca il prezzo nel listino forzato.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyForcedPriceListAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        // Determina quale listino forzare (da request o da BusinessParty)
        Guid? forcedPriceListId = request.ForcedPriceListId;

        if (!forcedPriceListId.HasValue && request.BusinessPartyId.HasValue)
        {
            var businessParty = await _context.BusinessParties
                .FirstOrDefaultAsync(bp => bp.Id == request.BusinessPartyId.Value && !bp.IsDeleted, cancellationToken);

            forcedPriceListId = businessParty?.ForcedPriceListId;
        }

        if (!forcedPriceListId.HasValue)
        {
            throw new InvalidOperationException("ForcedPriceListId is required when using ForcedPriceList mode");
        }

        var evaluationDate = request.ReferenceDate ?? DateTime.UtcNow;

        // Cerca il prezzo nel listino forzato
        var priceEntry = await _context.PriceListEntries
            .Include(e => e.PriceList)
            .Where(e => e.PriceListId == forcedPriceListId.Value
                     && e.ProductId == product.Id
                     && !e.IsDeleted
                     && e.PriceList!.Status == PriceListStatus.Active
                     && (!e.PriceList.ValidFrom.HasValue || e.PriceList.ValidFrom <= evaluationDate)
                     && (!e.PriceList.ValidTo.HasValue || e.PriceList.ValidTo >= evaluationDate)
                     && (e.MinQuantity == 0 || e.MinQuantity <= request.Quantity)
                     && (e.MaxQuantity == 0 || e.MaxQuantity >= request.Quantity))
            .FirstOrDefaultAsync(cancellationToken);

        if (priceEntry == null)
        {
            throw new InvalidOperationException($"Product {product.Id} not found in forced price list {forcedPriceListId.Value}");
        }

        var basePrice = priceEntry.Price;
        var finalPrice = basePrice;
        decimal? discountPercentage = null;

        // Applica eventuale sconto BusinessParty
        if (request.BusinessPartyId.HasValue)
        {
            var businessPartyRelation = await _context.PriceListBusinessParties
                .FirstOrDefaultAsync(plbp => plbp.PriceListId == forcedPriceListId.Value
                                          && plbp.BusinessPartyId == request.BusinessPartyId.Value
                                          && !plbp.IsDeleted
                                          && plbp.Status == PriceListBusinessPartyStatus.Active,
                                          cancellationToken);

            if (businessPartyRelation?.GlobalDiscountPercentage.HasValue == true)
            {
                discountPercentage = businessPartyRelation.GlobalDiscountPercentage.Value;
                finalPrice = basePrice * (1 - discountPercentage.Value / 100m);
            }
        }

        _logger.LogInformation("Applied forced price list {PriceListId} for product {ProductId}: {Price}",
            forcedPriceListId.Value, product.Id, finalPrice);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = finalPrice,
            Currency = priceEntry.Currency,
            AppliedMode = PriceApplicationMode.ForcedPriceList,
            AppliedPriceListId = priceEntry.PriceListId,
            AppliedPriceListName = priceEntry.PriceList?.Name,
            BasePriceFromPriceList = basePrice,
            AppliedDiscountPercentage = discountPercentage,
            PriceAfterDiscount = discountPercentage.HasValue ? finalPrice : null,
            IsManual = false,
            IsPriceListForced = true,
            SearchPath = new List<string> { $"Forced price list: {priceEntry.PriceList?.Name}" }
        };
    }

    /// <summary>
    /// Gestisce modalità Automatic: cerca il listino migliore secondo precedenza.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyAutomaticPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        var evaluationDate = request.ReferenceDate ?? DateTime.UtcNow;
        var searchPath = new List<string>();

        // Recupera tutti i listini applicabili (attivi, validi per data)
        var applicablePriceEntries = await _context.PriceListEntries
            .Include(e => e.PriceList)
                .ThenInclude(pl => pl!.BusinessParties.Where(bp => !bp.IsDeleted))
            .Where(e => e.ProductId == product.Id
                     && !e.IsDeleted
                     && e.PriceList!.Status == PriceListStatus.Active
                     && (!e.PriceList.ValidFrom.HasValue || e.PriceList.ValidFrom <= evaluationDate)
                     && (!e.PriceList.ValidTo.HasValue || e.PriceList.ValidTo >= evaluationDate)
                     && (e.MinQuantity == 0 || e.MinQuantity <= request.Quantity)
                     && (e.MaxQuantity == 0 || e.MaxQuantity >= request.Quantity))
            .ToListAsync(cancellationToken);

        if (!applicablePriceEntries.Any())
        {
            // Fallback a prezzo base prodotto
            searchPath.Add("No price lists available, using product base price");
            _logger.LogWarning("No price lists found for product {ProductId}, using base price {BasePrice}",
                product.Id, product.DefaultPrice ?? 0m);

            return new ProductPriceResultDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Codes?.FirstOrDefault()?.Code,
                FinalPrice = product.DefaultPrice ?? 0m,
                Currency = "EUR",
                AppliedMode = PriceApplicationMode.Automatic,
                IsManual = false,
                IsPriceListForced = false,
                SearchPath = searchPath
            };
        }

        // Ordina per precedenza
        IEnumerable<PriceListEntry> orderedEntries;

        if (request.BusinessPartyId.HasValue)
        {
            // Con BusinessParty: priorità ai listini assegnati al BusinessParty
            searchPath.Add($"BusinessParty {request.BusinessPartyId.Value} specified");

            orderedEntries = applicablePriceEntries
                .OrderByDescending(e => e.PriceList!.BusinessParties.Any(bp => bp.BusinessPartyId == request.BusinessPartyId.Value))
                .ThenByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }
        else
        {
            // Senza BusinessParty: solo listini generici (senza BusinessParty assegnati)
            searchPath.Add("No BusinessParty specified, using generic price lists only");

            orderedEntries = applicablePriceEntries
                .Where(e => !e.PriceList!.BusinessParties.Any())
                .OrderByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }

        var selectedEntry = orderedEntries.FirstOrDefault();

        if (selectedEntry == null)
        {
            // Fallback a prezzo base prodotto
            searchPath.Add("No applicable price lists found, using product base price");
            _logger.LogWarning("No applicable price lists found for product {ProductId}, using base price", product.Id);

            return new ProductPriceResultDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Codes?.FirstOrDefault()?.Code,
                FinalPrice = product.DefaultPrice ?? 0m,
                Currency = "EUR",
                AppliedMode = PriceApplicationMode.Automatic,
                IsManual = false,
                IsPriceListForced = false,
                SearchPath = searchPath
            };
        }

        var basePrice = selectedEntry.Price;
        var finalPrice = basePrice;
        decimal? discountPercentage = null;

        // Applica sconto BusinessParty se presente
        if (request.BusinessPartyId.HasValue)
        {
            var businessPartyRelation = selectedEntry.PriceList!.BusinessParties
                .FirstOrDefault(bp => bp.BusinessPartyId == request.BusinessPartyId.Value
                                   && !bp.IsDeleted
                                   && bp.Status == PriceListBusinessPartyStatus.Active);

            if (businessPartyRelation?.GlobalDiscountPercentage.HasValue == true)
            {
                discountPercentage = businessPartyRelation.GlobalDiscountPercentage.Value;
                finalPrice = basePrice * (1 - discountPercentage.Value / 100m);
                searchPath.Add($"Applied BusinessParty discount: {discountPercentage.Value}%");
            }
        }

        searchPath.Add($"Selected price list: {selectedEntry.PriceList?.Name} (Priority: {selectedEntry.PriceList?.Priority})");

        // Costruisci lista listini disponibili per UI
        var availablePriceLists = applicablePriceEntries
            .Select(e => new AvailablePriceListDto
            {
                PriceListId = e.PriceListId,
                Name = e.PriceList?.Name ?? string.Empty,
                Priority = e.PriceList?.Priority ?? 0,
                Price = e.Price,
                IsAssignedToBusinessParty = request.BusinessPartyId.HasValue &&
                    e.PriceList!.BusinessParties.Any(bp => bp.BusinessPartyId == request.BusinessPartyId.Value),
                IsDefault = e.PriceList?.IsDefault ?? false
            })
            .OrderByDescending(pl => pl.Priority)
            .ToList();

        _logger.LogInformation("Applied automatic price from price list {PriceListId} for product {ProductId}: {Price}",
            selectedEntry.PriceListId, product.Id, finalPrice);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = finalPrice,
            Currency = selectedEntry.Currency,
            AppliedMode = PriceApplicationMode.Automatic,
            AppliedPriceListId = selectedEntry.PriceListId,
            AppliedPriceListName = selectedEntry.PriceList?.Name,
            BasePriceFromPriceList = basePrice,
            AppliedDiscountPercentage = discountPercentage,
            PriceAfterDiscount = discountPercentage.HasValue ? finalPrice : null,
            IsManual = false,
            IsPriceListForced = false,
            AvailablePriceLists = availablePriceLists,
            SearchPath = searchPath
        };
    }

    /// <summary>
    /// Gestisce modalità HybridForcedWithOverrides: usa manuale se presente, altrimenti listino forzato.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyHybridPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        ProductPriceResultDto result;

        // Se ManualPrice presente → usa ApplyManualPriceAsync
        if (request.ManualPrice.HasValue && request.ManualPrice.Value > 0)
        {
            result = await ApplyManualPriceAsync(request, product, cancellationToken);
        }
        else
        {
            // Altrimenti → usa ApplyForcedPriceListAsync
            result = await ApplyForcedPriceListAsync(request, product, cancellationToken);
        }

        // Update mode to Hybrid
        return result with { AppliedMode = PriceApplicationMode.HybridForcedWithOverrides };
    }

    #endregion
}
