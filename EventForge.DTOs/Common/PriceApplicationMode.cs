namespace EventForge.DTOs.Common;

/// <summary>
/// Modalità di applicazione del prezzo di vendita
/// </summary>
public enum PriceApplicationMode
{
    /// <summary>
    /// Automatico: sistema sceglie listino migliore per precedenza
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Listino specifico forzato: usa sempre questo listino
    /// </summary>
    ForcedPriceList = 1,

    /// <summary>
    /// Prezzo completamente manuale: ignora tutti i listini
    /// </summary>
    Manual = 2,

    /// <summary>
    /// Ibrido: listino forzato + override manuali per alcuni prodotti
    /// </summary>
    HybridForcedWithOverrides = 3
}
