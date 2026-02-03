namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Service for mapping VAT rates and payment methods to fiscal printer codes.
/// </summary>
public interface IFiscalMappingService
{
    /// <summary>
    /// Gets the fiscal code for a VAT rate by its ID.
    /// </summary>
    /// <param name="vatRateId">VAT rate identifier.</param>
    /// <returns>Fiscal code (1-10).</returns>
    int GetVatFiscalCode(Guid vatRateId);

    /// <summary>
    /// Gets the fiscal code for a VAT rate by its percentage, with optional configured code.
    /// </summary>
    /// <param name="vatPercentage">VAT percentage.</param>
    /// <param name="configuredCode">Optional configured fiscal code.</param>
    /// <returns>Fiscal code (1-10).</returns>
    int GetVatFiscalCode(decimal vatPercentage, int? configuredCode = null);

    /// <summary>
    /// Gets the fiscal code for a payment method by its ID.
    /// </summary>
    /// <param name="paymentMethodId">Payment method identifier.</param>
    /// <returns>Fiscal code (1-10).</returns>
    int GetPaymentFiscalCode(Guid paymentMethodId);

    /// <summary>
    /// Gets the fiscal code for a payment method by its code, with optional configured code.
    /// </summary>
    /// <param name="paymentCode">Payment method code.</param>
    /// <param name="configuredCode">Optional configured fiscal code.</param>
    /// <returns>Fiscal code (1-10).</returns>
    int GetPaymentFiscalCode(string paymentCode, int? configuredCode = null);
}
