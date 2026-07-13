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
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    public async Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(
        Guid sourcePriceListId,
        DuplicatePriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Recupera il listino sorgente
        var sourcePriceList = await context.PriceLists
            .AsNoTracking()
            .Include(pl => pl.ProductPrices)
                .ThenInclude(pp => pp.Product)
            .Include(pl => pl.BusinessParties)
                .ThenInclude(plbp => plbp.BusinessParty)
            .FirstOrDefaultAsync(pl => pl.Id == sourcePriceListId && !pl.IsDeleted, cancellationToken);

        if (sourcePriceList is null)
        {
            logger.LogWarning("Source price list {PriceListId} not found for duplication", sourcePriceListId);
            throw new InvalidOperationException($"Price list {sourcePriceListId} not found");
        }

        // Count source entries BEFORE adding the new price list
        var sourceEntriesCount = await context.PriceListEntries
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.PriceListId == sourcePriceListId && !e.IsDeleted)
            .CountAsync(cancellationToken);

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

        context.PriceLists.Add(newPriceList);

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
            var pricesToCopy = (sourcePriceList.ProductPrices ?? [])
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
                    pp.Product != null &&
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

                context.PriceListEntries.Add(newEntry);
                stats = stats with { CopiedPriceCount = stats.CopiedPriceCount + 1 };
            }

            stats = stats with
            {
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

                context.PriceListBusinessParties.Add(newBP);
                stats = stats with { CopiedBusinessPartyCount = stats.CopiedBusinessPartyCount + 1 };
            }
        }

        // 6. Salva tutto
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
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
            Status = (Prym.DTOs.Common.PriceListStatus)newPriceList.Status,
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

}
