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

public class PriceListGenerationService : IPriceListGenerationService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PriceListGenerationService> _logger;

    public PriceListGenerationService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<PriceListGenerationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Price List Generation from Products

    /// <summary>
    /// Genera nuovo listino dai prezzi DefaultPrice dei prodotti
    /// </summary>
    public async Task<Guid> GenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Validazione e recupero TenantId da un prodotto esistente
        // Prima troviamo almeno un prodotto per ottenere il TenantId
        var anyProduct = await _context.Products
            .Where(p => !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (anyProduct == null)
        {
            throw new InvalidOperationException("Nessun prodotto disponibile nel sistema");
        }

        var tenantId = anyProduct.TenantId;

        // 2. Validazione EventId se specificato
        if (dto.EventId.HasValue)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.Id == dto.EventId.Value && e.TenantId == tenantId && !e.IsDeleted, cancellationToken);
            
            if (!eventExists)
            {
                throw new InvalidOperationException($"Evento {dto.EventId.Value} non trovato");
            }
        }

        // 3. Query prodotti con filtri
        var query = _context.Products
            .Where(p => p.TenantId == tenantId && !p.IsDeleted);

        // Filtro prodotti attivi
        if (dto.OnlyActiveProducts)
        {
            query = query.Where(p => p.Status == EventForge.Server.Data.Entities.Products.ProductStatus.Active);
        }

        // Filtro prodotti con prezzo
        if (dto.OnlyProductsWithPrice)
        {
            query = query.Where(p => p.DefaultPrice.HasValue && p.DefaultPrice.Value > 0);
        }

        // Filtro per categorie
        if (dto.FilterByCategoryIds != null && dto.FilterByCategoryIds.Any())
        {
            query = query.Where(p => p.CategoryNodeId.HasValue && dto.FilterByCategoryIds.Contains(p.CategoryNodeId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        if (!products.Any())
        {
            throw new InvalidOperationException("Nessun prodotto trovato con i criteri specificati");
        }

        if (dto.OnlyProductsWithPrice && !products.Any(p => p.DefaultPrice.HasValue && p.DefaultPrice.Value > 0))
        {
            throw new InvalidOperationException("Nessun prodotto trovato con prezzo maggiore di 0");
        }

        // 4. Crea PriceList
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Code = dto.Code ?? await GenerateUniquePriceListCodeAsync(tenantId, cancellationToken),
            Type = dto.Type,
            Direction = dto.Direction,
            Status = PriceListStatus.Active,
            EventId = dto.EventId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        _context.PriceLists.Add(priceList);

        // 5. Crea PriceListEntries
        var entriesCount = 0;
        foreach (var product in products)
        {
            if (!product.DefaultPrice.HasValue || product.DefaultPrice.Value <= 0)
                continue;

            var price = product.DefaultPrice.Value;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                price *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            price = ApplyRounding(price, dto.RoundingStrategy);

            var entry = new PriceListEntry
            {
                Id = Guid.NewGuid(),
                PriceListId = priceList.Id,
                ProductId = product.Id,
                Price = price,
                Status = PriceListEntryStatus.Active,
                TenantId = tenantId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
            entriesCount++;
        }

        // 6. Associa BusinessParties se specificati
        if (dto.BusinessPartyIds != null && dto.BusinessPartyIds.Any())
        {
            foreach (var businessPartyId in dto.BusinessPartyIds)
            {
                // Verifica che il BusinessParty esista
                var businessPartyExists = await _context.BusinessParties
                    .AnyAsync(bp => bp.Id == businessPartyId && bp.TenantId == tenantId && !bp.IsDeleted, cancellationToken);

                if (!businessPartyExists)
                {
                    _logger.LogWarning("BusinessParty {BusinessPartyId} non trovato, skip associazione", businessPartyId);
                    continue;
                }

                var priceListBusinessParty = new PriceListBusinessParty
                {
                    PriceListId = priceList.Id,
                    BusinessPartyId = businessPartyId,
                    Status = PriceListBusinessPartyStatus.Active,
                    TenantId = tenantId,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                _context.PriceListBusinessParties.Add(priceListBusinessParty);
            }
        }

        // 7. Salva e audit log
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Create",
            "GenerateFromProductPrices",
            null,
            $"Generated price list '{priceList.Name}' from {entriesCount} products",
            currentUser,
            null,
            cancellationToken);

        return priceList.Id;
    }

    #endregion

    #region Phase 2C - PR #4: Price list generation from purchase documents

    /// <summary>
    /// Helper class per memorizzare occorrenze di prezzi nei documenti
    /// </summary>
    private class PriceOccurrence
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
        public Guid DocumentId { get; set; }
    }

    /// <summary>
    /// Preview generazione listino da documenti (senza salvataggio)
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validazione fornitore e recupero TenantId
        var supplier = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);
        
        if (supplier == null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        if (dto.ToDate > DateTime.UtcNow)
        {
            throw new InvalidOperationException("La data di fine non può essere nel futuro");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            dto.SupplierId,
            dto.FromDate,
            dto.ToDate,
            dto.FilterByCategoryIds,
            dto.OnlyActiveProducts,
            dto.MinimumQuantity,
            tenantId,
            cancellationToken);

        var productPreviews = new List<ProductPricePreview>();
        decimal totalValue = 0;

        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);
            var originalPrice = calculatedPrice;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
                continue;

            productPreviews.Add(new ProductPricePreview
            {
                ProductId = productId,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                CalculatedPrice = calculatedPrice,
                OriginalPrice = originalPrice,
                OccurrencesInDocuments = occurrences.Count,
                LowestPrice = occurrences.Min(o => o.Price),
                HighestPrice = occurrences.Max(o => o.Price),
                AveragePrice = occurrences.Average(o => o.Price),
                LastPurchaseDate = occurrences.Max(o => o.Date)
            });

            totalValue += calculatedPrice;
        }

        // Conta documenti distinti
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        return new GeneratePriceListPreviewDto
        {
            TotalDocumentsAnalyzed = documentCount,
            TotalProductsFound = productPricesDict.Count,
            ProductsWithMultiplePrices = productPricesDict.Count(kvp => kvp.Value.Count > 1),
            ProductPreviews = productPreviews,
            TotalEstimatedValue = totalValue,
            AnalysisFromDate = dto.FromDate,
            AnalysisToDate = dto.ToDate
        };
    }

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    public async Task<Guid> GenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Validazione fornitore e recupero TenantId
        var supplier = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);
        
        if (supplier == null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        if (dto.ToDate > DateTime.UtcNow)
        {
            throw new InvalidOperationException("La data di fine non può essere nel futuro");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            dto.SupplierId,
            dto.FromDate,
            dto.ToDate,
            dto.FilterByCategoryIds,
            dto.OnlyActiveProducts,
            dto.MinimumQuantity,
            tenantId,
            cancellationToken);

        if (!productPricesDict.Any())
        {
            throw new InvalidOperationException("Nessun prodotto trovato nei documenti del periodo specificato");
        }

        // Crea PriceList
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Code = dto.Code ?? await GenerateUniquePriceListCodeAsync(tenantId, cancellationToken),
            Type = PriceListType.Purchase,
            Direction = PriceListDirection.Input,
            Status = PriceListStatus.Active,
            IsGeneratedFromDocuments = true,
            LastSyncedAt = DateTime.UtcNow,
            LastSyncedBy = currentUser,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        // Conta documenti per metadati
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        // Salva metadati
        var metadata = new PriceListGenerationMetadata
        {
            Strategy = dto.CalculationStrategy,
            Rounding = dto.RoundingStrategy,
            MarkupPercentage = dto.MarkupPercentage,
            AnalysisFromDate = dto.FromDate,
            AnalysisToDate = dto.ToDate,
            DocumentsAnalyzed = documentCount,
            ProductsGenerated = productPricesDict.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = currentUser
        };

        priceList.GenerationMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);

        _context.PriceLists.Add(priceList);

        // Crea PriceListBusinessParty
        var priceListBusinessParty = new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = dto.SupplierId,
            Status = PriceListBusinessPartyStatus.Active,
            TenantId = tenantId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        _context.PriceListBusinessParties.Add(priceListBusinessParty);

        // Crea PriceListEntries
        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var entry = new PriceListEntry
            {
                Id = Guid.NewGuid(),
                PriceListId = priceList.Id,
                ProductId = productId,
                Price = calculatedPrice,
                Status = PriceListEntryStatus.Active,
                TenantId = tenantId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Create",
            "GenerateFromPurchases",
            null,
            $"Generated price list '{priceList.Name}' from {productPricesDict.Count} products in {documentCount} purchase documents",
            currentUser,
            null,
            cancellationToken);

        return priceList.Id;
    }

    /// <summary>
    /// Preview aggiornamento listino esistente
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Carica listino esistente
        var priceList = await _context.PriceLists
            .Include(pl => pl.BusinessParties)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation == null)
        {
            throw new InvalidOperationException("Listino non ha un fornitore assegnato");
        }

        var supplierId = supplierRelation.BusinessPartyId;

        // Default range date
        var fromDate = dto.FromDate ?? DateTime.UtcNow.AddDays(-90);
        var toDate = dto.ToDate ?? DateTime.UtcNow;

        // Validazione range date
        if (fromDate >= toDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            supplierId,
            fromDate,
            toDate,
            null,
            false,
            null,
            tenantId,
            cancellationToken);

        var productPreviews = new List<ProductPricePreview>();
        decimal totalValue = 0;

        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);
            var originalPrice = calculatedPrice;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
                continue;

            productPreviews.Add(new ProductPricePreview
            {
                ProductId = productId,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                CalculatedPrice = calculatedPrice,
                OriginalPrice = originalPrice,
                OccurrencesInDocuments = occurrences.Count,
                LowestPrice = occurrences.Min(o => o.Price),
                HighestPrice = occurrences.Max(o => o.Price),
                AveragePrice = occurrences.Average(o => o.Price),
                LastPurchaseDate = occurrences.Max(o => o.Date)
            });

            totalValue += calculatedPrice;
        }

        // Conta documenti distinti
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        return new GeneratePriceListPreviewDto
        {
            TotalDocumentsAnalyzed = documentCount,
            TotalProductsFound = productPricesDict.Count,
            ProductsWithMultiplePrices = productPricesDict.Count(kvp => kvp.Value.Count > 1),
            ProductPreviews = productPreviews,
            TotalEstimatedValue = totalValue,
            AnalysisFromDate = fromDate,
            AnalysisToDate = toDate
        };
    }

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    public async Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Carica listino esistente
        var priceList = await _context.PriceLists
            .Include(pl => pl.BusinessParties)
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation == null)
        {
            throw new InvalidOperationException("Listino non ha un fornitore assegnato");
        }

        var supplierId = supplierRelation.BusinessPartyId;

        // Default range date
        var fromDate = dto.FromDate ?? DateTime.UtcNow.AddDays(-90);
        var toDate = dto.ToDate ?? DateTime.UtcNow;

        // Validazione range date
        if (fromDate >= toDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            supplierId,
            fromDate,
            toDate,
            null,
            false,
            null,
            tenantId,
            cancellationToken);

        int pricesUpdated = 0;
        int pricesAdded = 0;
        int pricesRemoved = 0;
        int pricesUnchanged = 0;
        var warnings = new List<string>();

        // Calcola nuovi prezzi
        var newPrices = new Dictionary<Guid, decimal>();
        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            newPrices[productId] = calculatedPrice;
        }

        // Aggiorna prezzi esistenti
        foreach (var entry in priceList.ProductPrices.ToList())
        {
            if (newPrices.TryGetValue(entry.ProductId, out var newPrice))
            {
                if (Math.Abs(entry.Price - newPrice) > 0.001m)
                {
                    entry.Price = newPrice;
                    entry.ModifiedBy = currentUser;
                    entry.ModifiedAt = DateTime.UtcNow;
                    pricesUpdated++;
                }
                else
                {
                    pricesUnchanged++;
                }

                newPrices.Remove(entry.ProductId);
            }
            else if (dto.RemoveObsoleteProducts)
            {
                _context.PriceListEntries.Remove(entry);
                pricesRemoved++;
            }
            else
            {
                pricesUnchanged++;
            }
        }

        // Aggiungi nuovi prodotti
        if (dto.AddNewProducts)
        {
            foreach (var kvp in newPrices)
            {
                var entry = new PriceListEntry
                {
                    Id = Guid.NewGuid(),
                    PriceListId = priceList.Id,
                    ProductId = kvp.Key,
                    Price = kvp.Value,
                    Status = PriceListEntryStatus.Active,
                    TenantId = tenantId,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                _context.PriceListEntries.Add(entry);
                pricesAdded++;
            }
        }
        else if (newPrices.Any())
        {
            warnings.Add($"{newPrices.Count} nuovi prodotti trovati ma non aggiunti (AddNewProducts = false)");
        }

        // Conta documenti per metadati
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        // Aggiorna metadati listino
        var metadata = new PriceListGenerationMetadata
        {
            Strategy = dto.CalculationStrategy,
            Rounding = dto.RoundingStrategy,
            MarkupPercentage = dto.MarkupPercentage,
            AnalysisFromDate = fromDate,
            AnalysisToDate = toDate,
            DocumentsAnalyzed = documentCount,
            ProductsGenerated = productPricesDict.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = currentUser
        };

        priceList.GenerationMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);
        priceList.LastSyncedAt = DateTime.UtcNow;
        priceList.LastSyncedBy = currentUser;
        priceList.IsGeneratedFromDocuments = true;
        priceList.ModifiedBy = currentUser;
        priceList.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Update",
            "UpdateFromPurchases",
            null,
            $"Updated: {pricesUpdated}, Added: {pricesAdded}, Removed: {pricesRemoved}, Unchanged: {pricesUnchanged}",
            currentUser,
            null,
            cancellationToken);

        return new UpdatePriceListResultDto
        {
            PriceListId = priceList.Id,
            PriceListName = priceList.Name,
            PricesUpdated = pricesUpdated,
            PricesAdded = pricesAdded,
            PricesRemoved = pricesRemoved,
            PricesUnchanged = pricesUnchanged,
            SyncedAt = DateTime.UtcNow,
            SyncedBy = currentUser,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Ottiene prezzi da documenti di carico per un fornitore
    /// </summary>
    private async Task<Dictionary<Guid, List<PriceOccurrence>>> GetProductPricesFromDocumentsAsync(
        Guid supplierId,
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? filterByCategoryIds,
        bool onlyActiveProducts,
        decimal? minimumQuantity,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Query documenti di carico (IsStockIncrease = true)
        var query = _context.DocumentRows
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Include(dr => dr.Product)
            .Where(dr => dr.TenantId == tenantId &&
                        dr.DocumentHeader!.BusinessPartyId == supplierId &&
                        dr.DocumentHeader.Date >= fromDate &&
                        dr.DocumentHeader.Date <= toDate &&
                        dr.DocumentHeader.DocumentType!.IsStockIncrease &&
                        dr.ProductId != null &&
                        dr.UnitPrice > 0);

        // Filtri opzionali
        if (onlyActiveProducts)
        {
            query = query.Where(dr => dr.Product!.Status == Data.Entities.Products.ProductStatus.Active);
        }

        if (filterByCategoryIds != null && filterByCategoryIds.Any())
        {
            query = query.Where(dr => dr.Product!.CategoryNodeId != null &&
                                    filterByCategoryIds.Contains(dr.Product.CategoryNodeId.Value));
        }

        var documentRows = await query.ToListAsync(cancellationToken);

        // Raggruppa per prodotto
        var productPrices = documentRows
            .Where(dr => dr.ProductId.HasValue)
            .GroupBy(dr => dr.ProductId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(dr => new PriceOccurrence
                {
                    Price = dr.UnitPrice,
                    Quantity = dr.Quantity,
                    Date = dr.DocumentHeader?.Date ?? DateTime.UtcNow,
                    DocumentId = dr.DocumentHeaderId
                }).ToList()
            );

        // Filtra per quantità minima
        if (minimumQuantity.HasValue)
        {
            productPrices = productPrices
                .Where(kvp => kvp.Value.Sum(o => o.Quantity) >= minimumQuantity.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return productPrices;
    }

    /// <summary>
    /// Calcola il prezzo secondo la strategia specificata
    /// </summary>
    private decimal CalculatePriceByStrategy(
        List<PriceOccurrence> occurrences,
        PriceCalculationStrategy strategy)
    {
        if (occurrences == null || !occurrences.Any())
            throw new InvalidOperationException("Nessun prezzo disponibile per il calcolo");

        return strategy switch
        {
            PriceCalculationStrategy.LastPurchasePrice =>
                occurrences.OrderByDescending(p => p.Date).First().Price,

            PriceCalculationStrategy.WeightedAveragePrice =>
                occurrences.Sum(p => p.Price * p.Quantity) / occurrences.Sum(p => p.Quantity),

            PriceCalculationStrategy.SimpleAveragePrice =>
                occurrences.Average(p => p.Price),

            PriceCalculationStrategy.LowestPrice =>
                occurrences.Min(p => p.Price),

            PriceCalculationStrategy.HighestPrice =>
                occurrences.Max(p => p.Price),

            PriceCalculationStrategy.MedianPrice =>
                CalculateMedian(occurrences.Select(p => p.Price).ToList()),

            _ => throw new ArgumentException($"Strategia non supportata: {strategy}")
        };
    }

    /// <summary>
    /// Calcola la mediana di una lista di valori
    /// </summary>
    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values == null || !values.Any())
            throw new ArgumentException("Lista valori vuota");

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    #endregion

    #region Phase 2C - Price List Duplication

    /// <summary>
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    public async Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(
        Guid sourcePriceListId,
        DuplicatePriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Recupera il listino sorgente
            var sourcePriceList = await _context.PriceLists
                .Include(pl => pl.ProductPrices)
                    .ThenInclude(pp => pp.Product)
                .Include(pl => pl.BusinessParties)
                    .ThenInclude(plbp => plbp.BusinessParty)
                .FirstOrDefaultAsync(pl => pl.Id == sourcePriceListId && !pl.IsDeleted, cancellationToken);

            if (sourcePriceList == null)
            {
                _logger.LogWarning("Source price list {PriceListId} not found for duplication", sourcePriceListId);
                throw new InvalidOperationException($"Price list {sourcePriceListId} not found");
            }

            // Count source entries BEFORE adding the new price list
            var sourceEntriesCount = sourcePriceList.ProductPrices?.Count(pp => !pp.IsDeleted) ?? 0;
            
            // If the navigation property is empty, do a direct query (can happen with in-memory DB in tests)
            if (sourceEntriesCount == 0)
            {
                sourceEntriesCount = await _context.PriceListEntries
                    .Where(e => e.PriceListId == sourcePriceListId && !e.IsDeleted)
                    .CountAsync(cancellationToken);
            }

            // 2. Genera codice se non fornito
            var newCode = dto.Code ?? await GenerateUniquePriceListCodeAsync(
                dto.Name, cancellationToken);

            // 3. Crea il nuovo listino (copia metadati)
            var newPriceList = new Data.Entities.PriceList.PriceList
            {
                Id = Guid.NewGuid(),
                TenantId = sourcePriceList.TenantId,
                Name = dto.Name,
                Description = dto.Description ?? $"Duplicato da: {sourcePriceList.Name}",
                Code = newCode,
                Type = dto.NewType ?? sourcePriceList.Type,
                Direction = dto.NewDirection ?? sourcePriceList.Direction,
                Status = (Data.Entities.PriceList.PriceListStatus)dto.NewStatus,
                Priority = dto.NewPriority ?? sourcePriceList.Priority,
                ValidFrom = dto.NewValidFrom ?? sourcePriceList.ValidFrom,
                ValidTo = dto.NewValidTo ?? sourcePriceList.ValidTo,
                EventId = dto.NewEventId ?? sourcePriceList.EventId,
                IsDefault = false, // Mai copiare IsDefault
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.PriceLists.Add(newPriceList);

            var stats = new
            {
                SourcePriceCount = sourceEntriesCount,
                CopiedPriceCount = 0,
                SkippedPriceCount = 0,
                CopiedBusinessPartyCount = 0
            };

            // 4. Copia le voci di prezzo (se richiesto)
            if (dto.CopyPrices)
            {
                var pricesToCopy = sourcePriceList.ProductPrices
                    .Where(pp => !pp.IsDeleted && pp.Status == PriceListEntryStatus.Active);

                // Applica filtri
                if (dto.OnlyActiveProducts)
                {
                    pricesToCopy = pricesToCopy.Where(pp => pp.Product != null && !pp.Product.IsDeleted);
                }

                if (dto.FilterByProductIds?.Any() == true)
                {
                    pricesToCopy = pricesToCopy.Where(pp =>
                        dto.FilterByProductIds.Contains(pp.ProductId));
                }

                if (dto.FilterByCategoryIds?.Any() == true)
                {
                    pricesToCopy = pricesToCopy.Where(pp =>
                        pp.Product.CategoryNodeId.HasValue &&
                        dto.FilterByCategoryIds.Contains(pp.Product.CategoryNodeId.Value));
                }

                var pricesList = pricesToCopy.ToList();

                foreach (var sourcePrice in pricesList)
                {
                    var newPrice = sourcePrice.Price;

                    // Applica maggiorazione se specificata
                    if (dto.ApplyMarkupPercentage.HasValue)
                    {
                        newPrice *= (1 + dto.ApplyMarkupPercentage.Value / 100);
                    }

                    // Applica arrotondamento se specificato
                    if (dto.RoundingStrategy.HasValue)
                    {
                        newPrice = ApplyRounding(newPrice, dto.RoundingStrategy.Value);
                    }

                    var newEntry = new PriceListEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = sourcePriceList.TenantId,
                        PriceListId = newPriceList.Id,
                        ProductId = sourcePrice.ProductId,
                        Price = newPrice,
                        Currency = sourcePrice.Currency,
                        MinQuantity = sourcePrice.MinQuantity,
                        MaxQuantity = sourcePrice.MaxQuantity,
                        LeadTimeDays = sourcePrice.LeadTimeDays,
                        MinimumOrderQuantity = sourcePrice.MinimumOrderQuantity,
                        SupplierProductCode = sourcePrice.SupplierProductCode,
                        IsEditableInFrontend = sourcePrice.IsEditableInFrontend,
                        IsDiscountable = sourcePrice.IsDiscountable,
                        Score = sourcePrice.Score,
                        Status = PriceListEntryStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };

                    _context.PriceListEntries.Add(newEntry);
                    stats = stats with { CopiedPriceCount = stats.CopiedPriceCount + 1 };
                }

                stats = stats with {
                    SkippedPriceCount = stats.SourcePriceCount - stats.CopiedPriceCount
                };
            }

            // 5. Copia le assegnazioni BusinessParty (se richiesto)
            if (dto.CopyBusinessParties)
            {
                foreach (var sourceBP in sourcePriceList.BusinessParties.Where(bp => !bp.IsDeleted))
                {
                    var newBP = new PriceListBusinessParty
                    {
                        Id = Guid.NewGuid(),
                        TenantId = sourcePriceList.TenantId,
                        PriceListId = newPriceList.Id,
                        BusinessPartyId = sourceBP.BusinessPartyId,
                        IsPrimary = sourceBP.IsPrimary,
                        OverridePriority = sourceBP.OverridePriority,
                        GlobalDiscountPercentage = sourceBP.GlobalDiscountPercentage,
                        SpecificValidFrom = sourceBP.SpecificValidFrom,
                        SpecificValidTo = sourceBP.SpecificValidTo,
                        Status = sourceBP.Status,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };

                    _context.PriceListBusinessParties.Add(newBP);
                    stats = stats with { CopiedBusinessPartyCount = stats.CopiedBusinessPartyCount + 1 };
                }
            }

            // 6. Salva tutto
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price list duplicated: {SourceId} -> {NewId} ({CopiedPrices} prices, {CopiedBP} business parties)",
                sourcePriceListId, newPriceList.Id, stats.CopiedPriceCount, stats.CopiedBusinessPartyCount);

            // 7. Recupera il listino completo per il DTO - need to use the interface method from PriceListService
            // For now we'll create a minimal DTO
            var newPriceListDto = new PriceListDto
            {
                Id = newPriceList.Id,
                Name = newPriceList.Name,
                Description = newPriceList.Description,
                Code = newPriceList.Code,
                Type = newPriceList.Type,
                Direction = newPriceList.Direction,
                Status = (EventForge.DTOs.Common.PriceListStatus)newPriceList.Status,
                Priority = newPriceList.Priority,
                ValidFrom = newPriceList.ValidFrom,
                ValidTo = newPriceList.ValidTo,
                IsDefault = newPriceList.IsDefault,
                EventId = newPriceList.EventId,
                CreatedAt = newPriceList.CreatedAt,
                CreatedBy = newPriceList.CreatedBy
            };

            return new DuplicatePriceListResultDto
            {
                SourcePriceListId = sourcePriceListId,
                SourcePriceListName = sourcePriceList.Name,
                NewPriceList = newPriceListDto,
                SourcePriceCount = stats.SourcePriceCount,
                CopiedPriceCount = stats.CopiedPriceCount,
                SkippedPriceCount = stats.SkippedPriceCount,
                CopiedBusinessPartyCount = stats.CopiedBusinessPartyCount,
                AppliedMarkupPercentage = dto.ApplyMarkupPercentage,
                AppliedRoundingStrategy = dto.RoundingStrategy,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating price list {PriceListId}", sourcePriceListId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Genera un codice univoco per il listino
    /// </summary>
    private async Task<string> GenerateUniquePriceListCodeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var baseCode = $"PL-{DateTime.UtcNow:yyyyMMdd}";
        var code = baseCode;
        var counter = 1;

        while (await _context.PriceLists.AnyAsync(pl => pl.TenantId == tenantId && pl.Code == code, cancellationToken))
        {
            code = $"{baseCode}-{counter:D3}";
            counter++;
        }

        return code;
    }

    /// <summary>
    /// Genera un codice univoco per il listino basato sul nome.
    /// </summary>
    private async Task<string> GenerateUniquePriceListCodeAsync(
        string name,
        CancellationToken cancellationToken)
    {
        // Normalizza il nome per creare un codice base usando System.Text per rimuovere accenti
        var normalized = name.Normalize(System.Text.NormalizationForm.FormD);
        var withoutAccents = new string(normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) 
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(System.Text.NormalizationForm.FormC);

        var baseCode = new string(withoutAccents
            .ToUpperInvariant()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .Take(20)
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = "PRICELIST";

        var code = baseCode;
        var counter = 1;

        while (await _context.PriceLists.AnyAsync(
            pl => pl.Code == code && !pl.IsDeleted,
            cancellationToken))
        {
            code = $"{baseCode}-{counter}";
            counter++;
        }

        return code;
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

    #endregion
}
