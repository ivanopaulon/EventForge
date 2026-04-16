using Prym.DTOs.Sales;

namespace Prym.Web.Models.Sales;

/// <summary>Modalità di ordinamento articoli nel POS 2026.</summary>
public enum Pos26SortMode
{
    UltimiAcquisti,
    BestSeller,
    Alfabetico,
    Prezzo,
    Categoria
}

/// <summary>Metodi di pagamento standard POS 2026 (mappati sui PaymentMethodDto del sistema).</summary>
public enum Pos26PaymentMethod
{
    Carta,
    QrDigitale,
    Contanti,
    BuonoCredito
}

/// <summary>Elemento nel carrello POS 2026 (wrapper locale, sincronizzato con SaleItemDto).</summary>
public class Pos26CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal PrezzoUnitario { get; set; }
    public decimal Quantita { get; set; }
    public decimal ScontoPercent { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string? VatRateName { get; set; }
    public string? Note { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? BrandName { get; set; }
    public bool IsService { get; set; }

    /// <summary>Totale riga (al netto dello sconto).</summary>
    public decimal Totale => PrezzoUnitario * Quantita * (1 - ScontoPercent / 100m);

    /// <summary>Costruisce da un SaleItemDto esistente.</summary>
    public static Pos26CartItem FromSaleItem(SaleItemDto item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        Nome = item.ProductName ?? item.ProductCode ?? "Articolo",
        ProductCode = item.ProductCode,
        PrezzoUnitario = item.UnitPrice,
        Quantita = item.Quantity,
        ScontoPercent = item.DiscountPercent,
        TaxRate = item.TaxRate,
        TaxAmount = item.TaxAmount,
        VatRateName = item.VatRateName,
        Note = item.Notes,
        ThumbnailUrl = item.ProductThumbnailUrl,
        BrandName = item.BrandName,
        IsService = item.IsService
    };
}

/// <summary>Risultato di un pagamento completato nel dialog POS 2026.</summary>
public record RisultatoPagamento(
    decimal TotaleOrdine,
    decimal TotalePagato,
    decimal Resto,
    List<RigaPagamento> Righe
);

/// <summary>Singola riga pagamento nel dialog POS 2026.</summary>
public record RigaPagamento(
    PaymentMethodDto Metodo,
    decimal Importo
);
