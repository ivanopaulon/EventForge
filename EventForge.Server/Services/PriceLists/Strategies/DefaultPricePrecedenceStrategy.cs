using EventForge.Server.Data.Entities.PriceList;

namespace EventForge.Server.Services.PriceLists.Strategies;

/// <summary>
/// Default implementation of price precedence strategy.
/// Precedence order:
/// 1. BusinessParty-assigned price lists (if businessPartyId provided)
/// 2. Priority (higher value = higher precedence)
/// 3. Default price list flag
/// 4. Creation date (newer = higher precedence)
/// </summary>
public class DefaultPricePrecedenceStrategy : IPricePrecedenceStrategy
{
    public IEnumerable<PriceListEntry> OrderByPrecedence(
        IEnumerable<PriceListEntry> applicableEntries,
        Guid? businessPartyId = null)
    {
        if (businessPartyId.HasValue)
        {
            // Con BusinessParty: priorità ai listini assegnati
            return applicableEntries
                .OrderByDescending(e => e.PriceList!.BusinessParties
                    .Any(bp => bp.BusinessPartyId == businessPartyId.Value && !bp.IsDeleted))
                .ThenByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.IsDefault)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }
        else
        {
            // Senza BusinessParty: solo listini generici
            return applicableEntries
                .Where(e => !e.PriceList!.BusinessParties.Any())
                .OrderByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.IsDefault)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }
    }

    public bool IsEntryApplicable(
        PriceListEntry entry,
        DateTime evaluationDate,
        int quantity)
    {
        if (entry.Status != Data.Entities.PriceList.PriceListEntryStatus.Active)
            return false;

        if (entry.PriceList?.Status != Data.Entities.PriceList.PriceListStatus.Active)
            return false;

        // Validità temporale listino
        if (entry.PriceList.ValidFrom.HasValue && entry.PriceList.ValidFrom.Value > evaluationDate)
            return false;

        if (entry.PriceList.ValidTo.HasValue && entry.PriceList.ValidTo.Value < evaluationDate)
            return false;

        // Quantità min/max
        if (quantity < entry.MinQuantity)
            return false;

        if (entry.MaxQuantity > 0 && quantity > entry.MaxQuantity)
            return false;

        return true;
    }
}
