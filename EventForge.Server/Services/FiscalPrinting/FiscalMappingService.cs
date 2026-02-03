using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Implementation of fiscal mapping service for VAT rates and payment methods.
/// Provides automatic fallback mapping when fiscal codes are not configured.
/// </summary>
public class FiscalMappingService : IFiscalMappingService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<FiscalMappingService> _logger;

    public FiscalMappingService(EventForgeDbContext context, ILogger<FiscalMappingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int GetVatFiscalCode(Guid vatRateId)
    {
        try
        {
            var vatRate = _context.VatRates
                .AsNoTracking()
                .Where(v => v.Id == vatRateId && !v.IsDeleted)
                .Select(v => new { v.FiscalCode, v.Percentage })
                .FirstOrDefault();

            if (vatRate == null)
            {
                _logger.LogWarning("VAT rate {VatRateId} not found. Using default fiscal code 1.", vatRateId);
                return 1;
            }

            return GetVatFiscalCode(vatRate.Percentage, vatRate.FiscalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal code for VAT rate {VatRateId}. Using default code 1.", vatRateId);
            return 1;
        }
    }

    /// <inheritdoc/>
    public int GetVatFiscalCode(decimal vatPercentage, int? configuredCode = null)
    {
        // If configured code is provided, use it
        if (configuredCode.HasValue && configuredCode.Value >= 1 && configuredCode.Value <= 10)
        {
            _logger.LogDebug("Using configured fiscal code {FiscalCode} for VAT {Percentage}%", configuredCode.Value, vatPercentage);
            return configuredCode.Value;
        }

        // Automatic fallback mapping based on Italian VAT rates
        int fiscalCode = vatPercentage switch
        {
            22m => 1,              // IVA 22% (ordinaria)
            10m => 2,              // IVA 10% (ridotta)
            >= 4m and <= 5m => 3,  // IVA 4-5% (super ridotta)
            0m => 4,               // IVA 0% (esente/non imponibile)
            _ => 1                 // Default to code 1 for unknown rates
        };

        if (configuredCode == null)
        {
            _logger.LogDebug("Using automatic fiscal code {FiscalCode} for VAT {Percentage}% (not configured)", fiscalCode, vatPercentage);
        }
        else
        {
            _logger.LogWarning("Configured fiscal code {ConfiguredCode} for VAT {Percentage}% is invalid. Using fallback code {FiscalCode}.", configuredCode, vatPercentage, fiscalCode);
        }

        return fiscalCode;
    }

    /// <inheritdoc/>
    public int GetPaymentFiscalCode(Guid paymentMethodId)
    {
        try
        {
            var paymentMethod = _context.PaymentMethods
                .AsNoTracking()
                .Where(pm => pm.Id == paymentMethodId && !pm.IsDeleted)
                .Select(pm => new { pm.FiscalCode, pm.Code })
                .FirstOrDefault();

            if (paymentMethod == null)
            {
                _logger.LogWarning("Payment method {PaymentMethodId} not found. Using default fiscal code 1.", paymentMethodId);
                return 1;
            }

            return GetPaymentFiscalCode(paymentMethod.Code, paymentMethod.FiscalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal code for payment method {PaymentMethodId}. Using default code 1.", paymentMethodId);
            return 1;
        }
    }

    /// <inheritdoc/>
    public int GetPaymentFiscalCode(string paymentCode, int? configuredCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentCode);

        // If configured code is provided, use it
        if (configuredCode.HasValue && configuredCode.Value >= 1 && configuredCode.Value <= 10)
        {
            _logger.LogDebug("Using configured fiscal code {FiscalCode} for payment method {PaymentCode}", configuredCode.Value, paymentCode);
            return configuredCode.Value;
        }

        // Automatic fallback mapping based on common payment method codes
        var normalizedCode = paymentCode.ToUpperInvariant();
        int fiscalCode = normalizedCode switch
        {
            "CASH" => 1,                    // Contanti
            "CHECK" => 2,                   // Assegno
            "CC" or "DEBIT" or "CARD" => 4, // Carte di credito/debito
            "TICKET" or "VOUCHER" => 5,     // Ticket/Buoni
            _ => 1                          // Default to code 1 (cash) for unknown methods
        };

        if (configuredCode == null)
        {
            _logger.LogDebug("Using automatic fiscal code {FiscalCode} for payment method {PaymentCode} (not configured)", fiscalCode, paymentCode);
        }
        else
        {
            _logger.LogWarning("Configured fiscal code {ConfiguredCode} for payment method {PaymentCode} is invalid. Using fallback code {FiscalCode}.", configuredCode, paymentCode, fiscalCode);
        }

        return fiscalCode;
    }
}
