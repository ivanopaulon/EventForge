using EventForge.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using PriceListDirection = EventForge.DTOs.Common.PriceListDirection;
using ProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;
using BusinessPartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Implementazione validazioni business rules per listini prezzi
/// </summary>
public class PriceListValidationService : IPriceListValidationService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<PriceListValidationService> _logger;
    
    // Valute supportate
    private static readonly string[] SupportedCurrencies = { "EUR", "USD", "GBP", "CHF" };
    
    public PriceListValidationService(
        EventForgeDbContext context,
        ILogger<PriceListValidationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // ==================== VALIDAZIONI TEMPORALI ====================
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidatePriceListDateRangeAsync(
        Guid priceListId, 
        DateTime evaluationDate,
        CancellationToken ct = default)
    {
        var priceList = await _context.PriceLists
            .Where(pl => pl.Id == priceListId && !pl.IsDeleted)
            .Select(pl => new { pl.ValidFrom, pl.ValidTo, pl.Name })
            .FirstOrDefaultAsync(ct);
        
        if (priceList == null)
        {
            return ValidationResult.NotFound("Listino non trovato");
        }
        
        // Check data inizio validità
        if (priceList.ValidFrom.HasValue && priceList.ValidFrom.Value > evaluationDate)
        {
            _logger.LogWarning(
                "Price list {PriceListId} not yet valid. Valid from: {ValidFrom}, Evaluation date: {Date}",
                priceListId, priceList.ValidFrom.Value, evaluationDate);
            
            return ValidationResult.Invalid(
                $"Listino '{priceList.Name}' non ancora valido. Valido dal: {priceList.ValidFrom.Value:dd/MM/yyyy}",
                "PRICE_LIST_NOT_YET_VALID"
            );
        }
        
        // Check data fine validità
        if (priceList.ValidTo.HasValue && priceList.ValidTo.Value < evaluationDate)
        {
            _logger.LogWarning(
                "Price list {PriceListId} expired. Valid until: {ValidTo}, Evaluation date: {Date}",
                priceListId, priceList.ValidTo.Value, evaluationDate);
            
            return ValidationResult.Invalid(
                $"Listino '{priceList.Name}' scaduto. Valido fino al: {priceList.ValidTo.Value:dd/MM/yyyy}",
                "PRICE_LIST_EXPIRED"
            );
        }
        
        _logger.LogDebug(
            "Price list {PriceListId} is valid for date {Date}",
            priceListId, evaluationDate);
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateNoPriceListOverlapAsync(
        Guid businessPartyId,
        PriceListDirection direction,
        DateTime? validFrom,
        DateTime? validTo,
        Guid? excludePriceListId = null,
        CancellationToken ct = default)
    {
        var overlappingLists = await _context.PriceListBusinessParties
            .Where(plbp => plbp.BusinessPartyId == businessPartyId
                        && !plbp.IsDeleted
                        && plbp.PriceList.Direction == direction
                        && plbp.PriceList.Status == PriceListStatus.Active)
            .Where(plbp => excludePriceListId == null || plbp.PriceListId != excludePriceListId)
            .Select(plbp => new
            {
                plbp.PriceListId,
                plbp.PriceList.Name,
                plbp.SpecificValidFrom,
                plbp.SpecificValidTo
            })
            .ToListAsync(ct);
        
        var newFrom = validFrom ?? DateTime.MinValue;
        var newTo = validTo ?? DateTime.MaxValue;
        
        foreach (var existing in overlappingLists)
        {
            var existingFrom = existing.SpecificValidFrom ?? DateTime.MinValue;
            var existingTo = existing.SpecificValidTo ?? DateTime.MaxValue;
            
            // Check sovrapposizione
            if (newFrom <= existingTo && newTo >= existingFrom)
            {
                _logger.LogWarning(
                    "Price list overlap detected for BusinessParty {BusinessPartyId}. " +
                    "Overlapping with price list {PriceListId}",
                    businessPartyId, existing.PriceListId);
                
                return ValidationResult.Invalid(
                    $"Il listino si sovrappone con '{existing.Name}' " +
                    $"(valido: {existingFrom:dd/MM/yyyy} - {existingTo:dd/MM/yyyy})",
                    "PRICE_LIST_OVERLAP"
                );
            }
        }
        
        return ValidationResult.Success();
    }
    
    // ==================== VALIDAZIONI STATO ====================
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidatePriceListStatusAsync(
        Guid priceListId,
        PriceListStatus requiredStatus,
        CancellationToken ct = default)
    {
        var priceList = await _context.PriceLists
            .Where(pl => pl.Id == priceListId && !pl.IsDeleted)
            .Select(pl => new { pl.Status, pl.Name })
            .FirstOrDefaultAsync(ct);
        
        if (priceList == null)
        {
            return ValidationResult.NotFound("Listino non trovato");
        }
        
        if (priceList.Status != requiredStatus)
        {
            _logger.LogWarning(
                "Price list {PriceListId} has invalid status. Current: {Current}, Required: {Required}",
                priceListId, priceList.Status, requiredStatus);
            
            return ValidationResult.Invalid(
                $"Listino '{priceList.Name}' in stato '{priceList.Status}'. Richiesto stato '{requiredStatus}'",
                "INVALID_PRICE_LIST_STATUS"
            );
        }
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public ValidationResult ValidateStatusTransition(
        PriceListStatus currentStatus,
        PriceListStatus newStatus)
    {
        // Matrice transizioni permesse
        var allowedTransitions = new Dictionary<PriceListStatus, List<PriceListStatus>>
        {
            [PriceListStatus.Active] = new() { PriceListStatus.Suspended, PriceListStatus.Deleted },
            [PriceListStatus.Suspended] = new() { PriceListStatus.Active, PriceListStatus.Deleted },
            [PriceListStatus.Deleted] = new() { } // Stato finale
        };
        
        if (!allowedTransitions.ContainsKey(currentStatus) || 
            !allowedTransitions[currentStatus].Contains(newStatus))
        {
            _logger.LogWarning(
                "Invalid status transition: {Current} → {New}",
                currentStatus, newStatus);
            
            return ValidationResult.Invalid(
                $"Transizione di stato non permessa: {currentStatus} → {newStatus}",
                "INVALID_STATUS_TRANSITION"
            );
        }
        
        return ValidationResult.Success();
    }
    
    // ==================== VALIDAZIONI DUPLICATI ====================
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateNoDuplicateProductAsync(
        Guid priceListId,
        Guid productId,
        Guid? excludeEntryId = null,
        CancellationToken ct = default)
    {
        var exists = await _context.PriceListEntries
            .AnyAsync(ple => ple.PriceListId == priceListId
                          && ple.ProductId == productId
                          && !ple.IsDeleted
                          && (excludeEntryId == null || ple.Id != excludeEntryId), ct);
        
        if (exists)
        {
            var product = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => new { p.Name, p.Code })
                .FirstOrDefaultAsync(ct);
            
            _logger.LogWarning(
                "Duplicate product {ProductId} in price list {PriceListId}",
                productId, priceListId);
            
            return ValidationResult.Invalid(
                $"Prodotto '{product?.Name}' ({product?.Code}) già presente nel listino",
                "DUPLICATE_PRODUCT_IN_PRICE_LIST"
            );
        }
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateNoDuplicateBusinessPartyAsync(
        Guid priceListId,
        Guid businessPartyId,
        CancellationToken ct = default)
    {
        var exists = await _context.PriceListBusinessParties
            .AnyAsync(plbp => plbp.PriceListId == priceListId
                           && plbp.BusinessPartyId == businessPartyId
                           && !plbp.IsDeleted, ct);
        
        if (exists)
        {
            var bp = await _context.BusinessParties
                .Where(b => b.Id == businessPartyId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(ct);
            
            _logger.LogWarning(
                "Duplicate business party {BusinessPartyId} in price list {PriceListId}",
                businessPartyId, priceListId);
            
            return ValidationResult.Invalid(
                $"Business Party '{bp}' già assegnato al listino",
                "DUPLICATE_BUSINESS_PARTY_IN_PRICE_LIST"
            );
        }
        
        return ValidationResult.Success();
    }
    
    // ==================== VALIDAZIONI PREZZI ====================
    
    /// <inheritdoc/>
    public ValidationResult ValidatePriceValue(decimal price, string fieldName = "Price")
    {
        var errors = new List<string>();
        
        // Check prezzo positivo
        if (price <= 0)
        {
            errors.Add($"{fieldName} deve essere maggiore di zero");
        }
        
        // Check precisione decimali (max 4 cifre)
        // Decimal structure: bits[3] contains scale in bits 16-23
        var decimalPlaces = (decimal.GetBits(price)[3] >> 16) & 0xFF;
        if (decimalPlaces > 4)
        {
            errors.Add($"{fieldName} può avere massimo 4 cifre decimali");
        }
        
        // Check range realistico
        if (price > 1_000_000m)
        {
            _logger.LogWarning("Price {Price} exceeds realistic threshold of 1,000,000", price);
            errors.Add($"{fieldName} supera il limite di 1.000.000");
        }
        
        if (errors.Any())
        {
            return ValidationResult.Invalid(
                string.Join("; ", errors),
                "INVALID_PRICE_VALUE"
            );
        }
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public ValidationResult ValidateQuantityRange(int minQuantity, int maxQuantity)
    {
        if (minQuantity < 0)
        {
            return ValidationResult.Invalid(
                "MinQuantity non può essere negativa",
                "INVALID_MIN_QUANTITY"
            );
        }
        
        if (maxQuantity > 0 && maxQuantity < minQuantity)
        {
            return ValidationResult.Invalid(
                $"MaxQuantity ({maxQuantity}) deve essere >= MinQuantity ({minQuantity})",
                "INVALID_QUANTITY_RANGE"
            );
        }
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public ValidationResult ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return ValidationResult.Invalid(
                "Currency è obbligatoria",
                "MISSING_CURRENCY"
            );
        }
        
        // Normalize to uppercase once for comparison
        var normalizedCurrency = currency.ToUpperInvariant();
        
        if (!SupportedCurrencies.Contains(normalizedCurrency))
        {
            _logger.LogWarning("Unsupported currency: {Currency}", currency);
            
            return ValidationResult.Invalid(
                $"Valuta '{currency}' non supportata. Valute valide: {string.Join(", ", SupportedCurrencies)}",
                "UNSUPPORTED_CURRENCY"
            );
        }
        
        return ValidationResult.Success();
    }
    
    // ==================== VALIDAZIONI RELAZIONALI ====================
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateProductIsActiveAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        var product = await _context.Products
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => new { p.Status, p.Name, p.Code })
            .FirstOrDefaultAsync(ct);
        
        if (product == null)
        {
            return ValidationResult.NotFound("Prodotto non trovato");
        }
        
        if (product.Status != ProductStatus.Active)
        {
            _logger.LogWarning(
                "Product {ProductId} is not active. Status: {Status}",
                productId, product.Status);
            
            return ValidationResult.Invalid(
                $"Prodotto '{product.Name}' ({product.Code}) non è attivo. Stato: {product.Status}",
                "PRODUCT_NOT_ACTIVE"
            );
        }
        
        return ValidationResult.Success();
    }
    
    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateBusinessPartyCompatibilityAsync(
        Guid businessPartyId,
        PriceListDirection direction,
        CancellationToken ct = default)
    {
        var businessParty = await _context.BusinessParties
            .Where(bp => bp.Id == businessPartyId && !bp.IsDeleted)
            .Select(bp => new { bp.PartyType, bp.Name })
            .FirstOrDefaultAsync(ct);
        
        if (businessParty == null)
        {
            return ValidationResult.NotFound("Business Party non trovato");
        }
        
        // Verifica compatibilità tipo con direzione listino
        var isCompatible = (direction, businessParty.PartyType) switch
        {
            (PriceListDirection.Output, BusinessPartyType.Cliente) => true,
            (PriceListDirection.Output, BusinessPartyType.ClienteFornitore) => true,
            (PriceListDirection.Input, BusinessPartyType.Fornitore) => true,
            (PriceListDirection.Input, BusinessPartyType.ClienteFornitore) => true,
            _ => false
        };
        
        if (!isCompatible)
        {
            _logger.LogWarning(
                "Business party {BusinessPartyId} (type: {Type}) incompatible with price list direction {Direction}",
                businessPartyId, businessParty.PartyType, direction);
            
            return ValidationResult.Invalid(
                $"Business Party '{businessParty.Name}' (tipo: {businessParty.PartyType}) " +
                $"non compatibile con listino {direction}",
                "INCOMPATIBLE_BUSINESS_PARTY_TYPE"
            );
        }
        
        return ValidationResult.Success();
    }
}
