using EventForge.Client.Models.Documents;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Servizio per calcoli fiscali e totali delle righe documento.
/// Centralizza la logica di calcolo IVA, sconti e totali per garantire coerenza.
/// </summary>
public interface IDocumentRowCalculationService
{
    /// <summary>
    /// Calcola tutti i totali di una riga documento (imponibile, IVA, sconto, totale)
    /// </summary>
    /// <param name="input">Parametri di calcolo (quantità, prezzo, IVA, sconti)</param>
    /// <returns>Risultato con tutti i totali calcolati</returns>
    DocumentRowCalculationResult CalculateRowTotals(DocumentRowCalculationInput input);

    /// <summary>
    /// Converte un prezzo da lordo (IVA inclusa) a netto, o viceversa
    /// </summary>
    /// <param name="input">Parametri conversione (prezzo, aliquota, direzione)</param>
    /// <returns>Prezzo convertito</returns>
    decimal ConvertPrice(VatConversionInput input);

    /// <summary>
    /// Calcola l'importo dello sconto applicabile
    /// </summary>
    /// <param name="baseAmount">Importo base su cui calcolare lo sconto</param>
    /// <param name="discountPercentage">Percentuale sconto (0-100)</param>
    /// <param name="discountValue">Valore fisso sconto</param>
    /// <param name="discountType">Tipo di sconto</param>
    /// <returns>Importo sconto (non può eccedere baseAmount)</returns>
    decimal CalculateDiscountAmount(
        decimal baseAmount, 
        decimal discountPercentage, 
        decimal discountValue, 
        DiscountType discountType);

    /// <summary>
    /// Calcola l'IVA su un imponibile
    /// </summary>
    /// <param name="netAmount">Imponibile netto</param>
    /// <param name="vatRate">Aliquota IVA in percentuale</param>
    /// <returns>Importo IVA</returns>
    decimal CalculateVatAmount(decimal netAmount, decimal vatRate);

    /// <summary>
    /// Scorporo IVA da un prezzo lordo
    /// </summary>
    /// <param name="grossPrice">Prezzo con IVA inclusa</param>
    /// <param name="vatRate">Aliquota IVA in percentuale</param>
    /// <returns>Prezzo netto (senza IVA)</returns>
    decimal ExtractVat(decimal grossPrice, decimal vatRate);

    /// <summary>
    /// Applica IVA a un prezzo netto
    /// </summary>
    /// <param name="netPrice">Prezzo senza IVA</param>
    /// <param name="vatRate">Aliquota IVA in percentuale</param>
    /// <returns>Prezzo lordo (con IVA)</returns>
    decimal ApplyVat(decimal netPrice, decimal vatRate);
}
