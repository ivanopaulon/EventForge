using Microsoft.EntityFrameworkCore;
using Prym.DTOs.PriceLists;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;


namespace EventForge.Server.Services.PriceLists;

public partial class PriceListGenerationService
{
    /// <summary>
    /// Preview aggiornamento listino esistente
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Carica listino esistente
        var priceList = await context.PriceLists
            .AsNoTracking()
            .Include(pl => pl.BusinessParties)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList is null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation is null)
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

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product is null)
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
        var documentCount = await context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType != null && dh.DocumentType.IsStockIncrease)
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
        var priceList = await context.PriceLists
            .Include(pl => pl.BusinessParties)
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList is null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation is null)
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
                context.PriceListEntries.Remove(entry);
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

                context.PriceListEntries.Add(entry);
                pricesAdded++;
            }
        }
        else if (newPrices.Any())
        {
            warnings.Add($"{newPrices.Count} nuovi prodotti trovati ma non aggiunti (AddNewProducts = false)");
        }

        // Conta documenti per metadati
        var documentCount = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType != null && dh.DocumentType.IsStockIncrease)
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

        await context.SaveChangesAsync(cancellationToken);

        // Audit log
        await auditLogService.LogEntityChangeAsync(
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

}
