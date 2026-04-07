using EventForge.DTOs.Common;

namespace EventForge.Client.Models.Documents;

/// <summary>
/// Input per il calcolo dei totali di una riga documento
/// </summary>
public class DocumentRowCalculationInput
{
    /// <summary>
    /// Quantità articolo
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Prezzo unitario NETTO (senza IVA)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Aliquota IVA in percentuale (es: 22 per 22%)
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Sconto percentuale sulla riga (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Sconto a valore assoluto in euro
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Tipo di sconto applicato
    /// </summary>
    public DiscountType DiscountType { get; set; }
}

/// <summary>
/// Risultato del calcolo totali riga documento
/// </summary>
public class DocumentRowCalculationResult
{
    /// <summary>
    /// Imponibile lordo (Quantità × Prezzo Unitario)
    /// </summary>
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Importo sconto applicato
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Imponibile netto (GrossAmount - DiscountAmount)
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Importo IVA calcolato su NetAmount
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Totale riga (NetAmount + VatAmount)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Prezzo unitario lordo (con IVA)
    /// </summary>
    public decimal UnitPriceGross { get; set; }
}

/// <summary>
/// Parametri per conversione prezzo con IVA inclusa
/// </summary>
public class VatConversionInput
{
    /// <summary>
    /// Prezzo da convertire
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Aliquota IVA in percentuale
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// true se il prezzo ha IVA inclusa (da scorporare)
    /// false se il prezzo è netto (da aggiungere IVA)
    /// </summary>
    public bool IsVatIncluded { get; set; }
}
