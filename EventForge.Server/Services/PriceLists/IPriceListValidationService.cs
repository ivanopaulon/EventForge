using PriceListDirection = EventForge.DTOs.Common.PriceListDirection;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service per validazioni business rules su listini prezzi
/// </summary>
public interface IPriceListValidationService
{
    // === VALIDAZIONI TEMPORALI ===

    /// <summary>
    /// Valida che il listino sia applicabile alla data specificata
    /// </summary>
    Task<ValidationResult> ValidatePriceListDateRangeAsync(
        Guid priceListId,
        DateTime evaluationDate,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica che non ci siano listini sovrapposti per lo stesso business party
    /// </summary>
    Task<ValidationResult> ValidateNoPriceListOverlapAsync(
        Guid businessPartyId,
        PriceListDirection direction,
        DateTime? validFrom,
        DateTime? validTo,
        Guid? excludePriceListId = null,
        CancellationToken ct = default);

    // === VALIDAZIONI STATO ===

    /// <summary>
    /// Verifica che il listino sia nello stato corretto
    /// </summary>
    Task<ValidationResult> ValidatePriceListStatusAsync(
        Guid priceListId,
        PriceListStatus requiredStatus,
        CancellationToken ct = default);

    /// <summary>
    /// Valida che la transizione di stato sia permessa
    /// </summary>
    ValidationResult ValidateStatusTransition(
        PriceListStatus currentStatus,
        PriceListStatus newStatus);

    // === VALIDAZIONI DUPLICATI ===

    /// <summary>
    /// Verifica che un prodotto non sia già presente nel listino
    /// </summary>
    Task<ValidationResult> ValidateNoDuplicateProductAsync(
        Guid priceListId,
        Guid productId,
        Guid? excludeEntryId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica che un business party non sia già assegnato al listino
    /// </summary>
    Task<ValidationResult> ValidateNoDuplicateBusinessPartyAsync(
        Guid priceListId,
        Guid businessPartyId,
        CancellationToken ct = default);

    // === VALIDAZIONI PREZZI ===

    /// <summary>
    /// Valida che il prezzo sia valido
    /// </summary>
    ValidationResult ValidatePriceValue(decimal price, string fieldName = "Price");

    /// <summary>
    /// Valida che i range quantità siano coerenti
    /// </summary>
    ValidationResult ValidateQuantityRange(int minQuantity, int maxQuantity);

    /// <summary>
    /// Valida che la valuta sia supportata
    /// </summary>
    ValidationResult ValidateCurrency(string currency);

    // === VALIDAZIONI RELAZIONALI ===

    /// <summary>
    /// Verifica che il prodotto esista e sia attivo
    /// </summary>
    Task<ValidationResult> ValidateProductIsActiveAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica compatibilità business party con direzione listino
    /// </summary>
    Task<ValidationResult> ValidateBusinessPartyCompatibilityAsync(
        Guid businessPartyId,
        PriceListDirection direction,
        CancellationToken ct = default);
}
