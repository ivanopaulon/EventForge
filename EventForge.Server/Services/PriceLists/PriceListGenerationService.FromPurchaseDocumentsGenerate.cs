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
    /// Preview generazione listino da documenti (senza salvataggio)
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validazione fornitore e recupero TenantId
        var supplier = await context.BusinessParties
            .AsNoTracking()
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Allow dates up to "tomorrow UTC" to accommodate clients in UTC+ timezones where local
        // "today" can be one calendar day ahead of the server's UTC date.
        if (dto.ToDate.Date > DateTime.UtcNow.Date.AddDays(1))
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
            .AsNoTracking()
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType != null && dh.DocumentType.IsStockIncrease)
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
        var supplier = await context.BusinessParties
            .AsNoTracking()
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Allow dates up to "tomorrow UTC" to accommodate clients in UTC+ timezones where local
        // "today" can be one calendar day ahead of the server's UTC date.
        if (dto.ToDate.Date > DateTime.UtcNow.Date.AddDays(1))
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
        var documentCount = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType != null && dh.DocumentType.IsStockIncrease)
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

        context.PriceLists.Add(priceList);

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

        context.PriceListBusinessParties.Add(priceListBusinessParty);

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

            context.PriceListEntries.Add(entry);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Audit log
        await auditLogService.LogEntityChangeAsync(
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

}
