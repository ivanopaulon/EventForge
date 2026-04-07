namespace EventForge.Server.Services.PriceLists.Strategies;

/// <summary>
/// Strategy interface for determining price list precedence.
/// Allows different algorithms for selecting which price list to apply.
/// </summary>
public interface IPricePrecedenceStrategy
{
    /// <summary>
    /// Ordina le entries applicabili secondo la strategia di precedenza.
    /// </summary>
    /// <param name="applicableEntries">Entries applicabili</param>
    /// <param name="businessPartyId">ID partner commerciale (opzionale)</param>
    /// <returns>Entries ordinate per precedenza (prima = priorità massima)</returns>
    IEnumerable<PriceListEntry> OrderByPrecedence(
        IEnumerable<PriceListEntry> applicableEntries,
        Guid? businessPartyId = null);

    /// <summary>
    /// Determina se una entry è applicabile.
    /// </summary>
    /// <param name="entry">Entry da valutare</param>
    /// <param name="evaluationDate">Data valutazione</param>
    /// <param name="quantity">Quantità</param>
    /// <returns>True se applicabile</returns>
    bool IsEntryApplicable(
        PriceListEntry entry,
        DateTime evaluationDate,
        int quantity);
}
