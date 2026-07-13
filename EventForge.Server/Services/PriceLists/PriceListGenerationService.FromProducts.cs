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
    /// Genera nuovo listino dai prezzi DefaultPrice dei prodotti
    /// </summary>
    public async Task<Guid> GenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Recupero TenantId dal contesto multi-tenant
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for price list generation.");

        // 2. Validazione EventId se specificato
        if (dto.EventId.HasValue)
        {
            var eventExists = await context.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id == dto.EventId.Value && e.TenantId == tenantId.Value && !e.IsDeleted, cancellationToken);

            if (!eventExists)
            {
                throw new InvalidOperationException($"Evento {dto.EventId.Value} non trovato");
            }
        }

        // 3. Query prodotti con filtri
        var query = context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted);

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
        if (dto.FilterByCategoryIds is not null && dto.FilterByCategoryIds.Any())
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
            TenantId = tenantId.Value,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Code = dto.Code ?? await GenerateUniquePriceListCodeAsync(tenantId.Value, cancellationToken),
            Type = dto.Type,
            Direction = dto.Direction,
            Priority = dto.Priority,
            IsDefault = dto.IsDefault,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Status = PriceListStatus.Active,
            EventId = dto.EventId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        context.PriceLists.Add(priceList);

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

            // Applica prezzo minimo se specificato
            if (dto.MinimumPrice.HasValue && price < dto.MinimumPrice.Value)
            {
                continue; // Salta questo prodotto se il prezzo è inferiore al minimo
            }

            var entry = new PriceListEntry
            {
                Id = Guid.NewGuid(),
                PriceListId = priceList.Id,
                ProductId = product.Id,
                Price = price,
                Status = PriceListEntryStatus.Active,
                TenantId = tenantId.Value,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            context.PriceListEntries.Add(entry);
            entriesCount++;
        }

        // 6. Associa BusinessParties se specificati
        if (dto.BusinessPartyIds is not null && dto.BusinessPartyIds.Any())
        {
            foreach (var businessPartyId in dto.BusinessPartyIds)
            {
                // Verifica che il BusinessParty esista
                var businessPartyExists = await context.BusinessParties
                    .AsNoTracking()
                    .AnyAsync(bp => bp.Id == businessPartyId && bp.TenantId == tenantId.Value && !bp.IsDeleted, cancellationToken);

                if (!businessPartyExists)
                {
                    logger.LogWarning("BusinessParty {BusinessPartyId} non trovato, skip associazione", businessPartyId);
                    continue;
                }

                var priceListBusinessParty = new PriceListBusinessParty
                {
                    PriceListId = priceList.Id,
                    BusinessPartyId = businessPartyId,
                    Status = PriceListBusinessPartyStatus.Active,
                    TenantId = tenantId.Value,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                context.PriceListBusinessParties.Add(priceListBusinessParty);
            }
        }

        // 7. Salva e audit log
        await context.SaveChangesAsync(cancellationToken);

        await auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Create",
            "GenFromProducts",
            null,
            $"Generated price list '{priceList.Name}' from {entriesCount} products",
            currentUser,
            null,
            cancellationToken);

        return priceList.Id;
    }

    /// <summary>
    /// Preview generazione listino da prezzi default prodotti (senza salvare)
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        // Recupero TenantId dal contesto multi-tenant
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for price list generation.");

        // Validazione EventId se specificato
        if (dto.EventId.HasValue)
        {
            var eventExists = await context.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id == dto.EventId.Value && e.TenantId == tenantId.Value && !e.IsDeleted, cancellationToken);

            if (!eventExists)
            {
                throw new InvalidOperationException($"Event {dto.EventId.Value} non trovato");
            }
        }

        // Query prodotti con stessa logica di GenerateFromProductPricesAsync
        var query = context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (dto.OnlyActiveProducts)
        {
            query = query.Where(p => p.Status == EventForge.Server.Data.Entities.Products.ProductStatus.Active);
        }

        if (dto.OnlyProductsWithPrice)
        {
            query = query.Where(p => p.DefaultPrice.HasValue && p.DefaultPrice.Value > 0);
        }

        if (dto.FilterByCategoryIds is not null && dto.FilterByCategoryIds.Any())
        {
            query = query.Where(p => p.CategoryNodeId.HasValue && dto.FilterByCategoryIds.Contains(p.CategoryNodeId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        if (!products.Any())
        {
            throw new InvalidOperationException("Nessun prodotto trovato con i criteri specificati");
        }

        // Calcola preview entries
        var previewEntries = new List<ProductPricePreview>();
        decimal totalEstimatedValue = 0m;
        int validProductsCount = 0;

        foreach (var product in products)
        {
            if (!product.DefaultPrice.HasValue || product.DefaultPrice.Value <= 0)
                continue;

            var basePrice = product.DefaultPrice.Value;

            // Applica markup se specificato
            if (dto.MarkupPercentage.HasValue)
            {
                basePrice *= (1 + dto.MarkupPercentage.Value / 100m);
            }

            // Applica arrotondamento se specificato
            basePrice = ApplyRounding(basePrice, dto.RoundingStrategy);

            // Applica prezzo minimo se specificato
            if (dto.MinimumPrice.HasValue && basePrice < dto.MinimumPrice.Value)
            {
                continue; // Salta questo prodotto se il prezzo è inferiore al minimo
            }

            previewEntries.Add(new ProductPricePreview
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                OriginalPrice = product.DefaultPrice.Value,
                CalculatedPrice = basePrice,
                OccurrencesInDocuments = 0, // Not applicable for default price generation
                LowestPrice = null,
                HighestPrice = null,
                AveragePrice = null,
                LastPurchaseDate = null
            });

            totalEstimatedValue += basePrice;
            validProductsCount++;
        }

        return new GeneratePriceListPreviewDto
        {
            TotalDocumentsAnalyzed = 0, // Not applicable for default price generation
            TotalProductsFound = validProductsCount,
            ProductsWithMultiplePrices = 0, // Not applicable for default price generation
            ProductPreviews = previewEntries,
            TotalEstimatedValue = totalEstimatedValue,
            AnalysisFromDate = DateTime.MinValue, // Not applicable for default price generation
            AnalysisToDate = DateTime.MinValue, // Not applicable for default price generation
            ProductsExcluded = products.Count - validProductsCount
        };
    }

}
