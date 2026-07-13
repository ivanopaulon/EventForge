using Microsoft.EntityFrameworkCore;
using Prym.DTOs.PriceLists;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;

namespace EventForge.Server.Services.PriceLists;

public partial class PriceListGenerationService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<PriceListGenerationService> logger,
    ITenantContext tenantContext) : IPriceListGenerationService
{

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
        var query = context.DocumentRows
            .AsNoTracking()
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Include(dr => dr.Product)
            .Where(dr => dr.TenantId == tenantId &&
                        dr.DocumentHeader!.BusinessPartyId == supplierId &&
                        dr.DocumentHeader.Date >= fromDate &&
                        dr.DocumentHeader.Date <= toDate &&
                        dr.DocumentHeader.DocumentType != null && dr.DocumentHeader.DocumentType.IsStockIncrease &&
                        dr.ProductId != null &&
                        dr.UnitPrice > 0);

        // Filtri opzionali
        if (onlyActiveProducts)
        {
            query = query.Where(dr => dr.Product!.Status == Data.Entities.Products.ProductStatus.Active);
        }

        if (filterByCategoryIds is not null && filterByCategoryIds.Any())
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
        if (occurrences is null || !occurrences.Any())
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
        if (values is null || !values.Any())
            throw new ArgumentException("Lista valori vuota");

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
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

        while (await context.PriceLists.AsNoTracking().AnyAsync(pl => pl.TenantId == tenantId && pl.Code == code, cancellationToken))
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

        while (await context.PriceLists.AsNoTracking().AnyAsync(
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
    private static decimal ApplyRounding(decimal value, Prym.DTOs.Common.RoundingStrategy strategy)
    {
        return strategy switch
        {
            Prym.DTOs.Common.RoundingStrategy.ToNearest5Cents =>
                Math.Round(value * 20, MidpointRounding.AwayFromZero) / 20m,

            Prym.DTOs.Common.RoundingStrategy.ToNearest10Cents =>
                Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10m,

            Prym.DTOs.Common.RoundingStrategy.ToNearest50Cents =>
                Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2m,

            Prym.DTOs.Common.RoundingStrategy.ToNearestEuro =>
                Math.Round(value, MidpointRounding.AwayFromZero),

            Prym.DTOs.Common.RoundingStrategy.ToNearest99Cents =>
                Math.Floor(value) + 0.99m,

            _ => value
        };
    }

    #endregion

}
