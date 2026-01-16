using EventForge.Client.Models.Documents;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Implementazione del servizio di calcolo fiscale per righe documento.
/// Utilizza aritmetica decimale precisa per calcoli finanziari.
/// </summary>
public class DocumentRowCalculationService : IDocumentRowCalculationService
{
    private readonly ILogger<DocumentRowCalculationService> _logger;

    public DocumentRowCalculationService(ILogger<DocumentRowCalculationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DocumentRowCalculationResult CalculateRowTotals(DocumentRowCalculationInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // Validazione input
        if (input.Quantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(input));
        if (input.UnitPrice < 0)
            throw new ArgumentException("UnitPrice cannot be negative", nameof(input));
        if (input.VatRate < 0 || input.VatRate > 100)
            throw new ArgumentException("VatRate must be between 0 and 100", nameof(input));

        try
        {
            // 1. Calcolo imponibile lordo
            var grossAmount = input.Quantity * input.UnitPrice;

            // 2. Calcolo sconto
            var discountAmount = CalculateDiscountAmount(
                grossAmount,
                input.DiscountPercentage,
                input.DiscountValue,
                input.DiscountType);

            // 3. Calcolo imponibile netto (dopo sconto)
            var netAmount = grossAmount - discountAmount;

            // 4. Calcolo IVA
            var vatAmount = CalculateVatAmount(netAmount, input.VatRate);

            // 5. Calcolo totale
            var totalAmount = netAmount + vatAmount;

            // 6. Calcolo prezzo unitario lordo (per display)
            var unitPriceGross = ApplyVat(input.UnitPrice, input.VatRate);

            _logger.LogDebug(
                "Row calculation: Qty={Qty}, Price={Price}, Vat={Vat}% => Net={Net}, Total={Total}",
                input.Quantity, input.UnitPrice, input.VatRate, netAmount, totalAmount);

            return new DocumentRowCalculationResult
            {
                GrossAmount = Math.Round(grossAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                NetAmount = Math.Round(netAmount, 2, MidpointRounding.AwayFromZero),
                VatAmount = Math.Round(vatAmount, 2, MidpointRounding.AwayFromZero),
                TotalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero),
                UnitPriceGross = Math.Round(unitPriceGross, 2, MidpointRounding.AwayFromZero)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating row totals");
            throw;
        }
    }

    /// <inheritdoc />
    public decimal ConvertPrice(VatConversionInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.Price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(input));
        if (input.VatRate < 0 || input.VatRate > 100)
            throw new ArgumentException("VatRate must be between 0 and 100", nameof(input));

        return input.IsVatIncluded
            ? ExtractVat(input.Price, input.VatRate)
            : ApplyVat(input.Price, input.VatRate);
    }

    /// <inheritdoc />
    public decimal CalculateDiscountAmount(
        decimal baseAmount,
        decimal discountPercentage,
        decimal discountValue,
        DiscountType discountType)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        decimal discount = discountType switch
        {
            DiscountType.Percentage => baseAmount * (discountPercentage / 100m),
            DiscountType.Value => discountValue,
            _ => 0m
        };

        // Lo sconto non può mai eccedere l'importo base
        return Math.Min(discount, baseAmount);
    }

    /// <inheritdoc />
    public decimal CalculateVatAmount(decimal netAmount, decimal vatRate)
    {
        if (netAmount < 0)
            throw new ArgumentException("Net amount cannot be negative", nameof(netAmount));
        if (vatRate < 0 || vatRate > 100)
            throw new ArgumentException("VatRate must be between 0 and 100", nameof(vatRate));

        return netAmount * (vatRate / 100m);
    }

    /// <inheritdoc />
    public decimal ExtractVat(decimal grossPrice, decimal vatRate)
    {
        if (grossPrice < 0)
            throw new ArgumentException("Gross price cannot be negative", nameof(grossPrice));
        if (vatRate < 0 || vatRate > 100)
            throw new ArgumentException("VatRate must be between 0 and 100", nameof(vatRate));

        // Formula scorporo IVA: Netto = Lordo / (1 + IVA%)
        return vatRate == 0
            ? grossPrice
            : grossPrice / (1 + vatRate / 100m);
    }

    /// <inheritdoc />
    public decimal ApplyVat(decimal netPrice, decimal vatRate)
    {
        if (netPrice < 0)
            throw new ArgumentException("Net price cannot be negative", nameof(netPrice));
        if (vatRate < 0 || vatRate > 100)
            throw new ArgumentException("VatRate must be between 0 and 100", nameof(vatRate));

        // Formula applicazione IVA: Lordo = Netto × (1 + IVA%)
        return netPrice * (1 + vatRate / 100m);
    }
}
