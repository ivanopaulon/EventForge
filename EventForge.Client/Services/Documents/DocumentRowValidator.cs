using EventForge.Client.Services;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Validates document row data before submission
/// </summary>
public class DocumentRowValidator : IDocumentRowValidator
{
    /// <summary>
    /// Validates a document row DTO for creation or update
    /// </summary>
    public ValidationResult Validate(CreateDocumentRowDto model)
    {
        var errors = new List<string>();

        // Required field validations
        if (string.IsNullOrWhiteSpace(model.Description))
        {
            errors.Add("validation.descriptionRequired");
        }

        // Quantity validations
        if (model.Quantity <= 0)
        {
            errors.Add("validation.quantityMustBePositive");
        }

        if (model.Quantity > 999999)
        {
            errors.Add("validation.quantityTooLarge");
        }

        // Price validations
        if (model.UnitPrice < 0)
        {
            errors.Add("validation.unitPriceCannotBeNegative");
        }

        if (model.UnitPrice > 9999999.99m)
        {
            errors.Add("validation.unitPriceTooLarge");
        }

        // Discount validations
        if (model.DiscountType == EventForge.DTOs.Common.DiscountType.Percentage)
        {
            if (model.LineDiscount < 0 || model.LineDiscount > 100)
            {
                errors.Add("validation.discountPercentageInvalid");
            }
        }
        else if (model.DiscountType == EventForge.DTOs.Common.DiscountType.Value)
        {
            if (model.LineDiscountValue < 0)
            {
                errors.Add("validation.discountValueCannotBeNegative");
            }

            var lineTotal = model.Quantity * model.UnitPrice;
            if (model.LineDiscountValue > lineTotal)
            {
                errors.Add("validation.discountValueExceedsTotal");
            }
        }

        // VAT validations
        if (model.VatRate < 0 || model.VatRate > 100)
        {
            errors.Add("validation.vatRateInvalid");
        }

        // Unit of measure validation
        if (!model.UnitOfMeasureId.HasValue && string.IsNullOrWhiteSpace(model.UnitOfMeasure))
        {
            errors.Add("validation.unitOfMeasureRequired");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            ErrorKeys = errors
        };
    }

    /// <summary>
    /// Validates a document row DTO for update operations
    /// </summary>
    public ValidationResult Validate(UpdateDocumentRowDto model)
    {
        var errors = new List<string>();

        // Reuse same validation logic as create
        // Convert to CreateDocumentRowDto for validation
        var createDto = new CreateDocumentRowDto
        {
            Description = model.Description,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            LineDiscount = model.LineDiscount,
            LineDiscountValue = model.LineDiscountValue,
            DiscountType = model.DiscountType,
            VatRate = model.VatRate,
            UnitOfMeasure = model.UnitOfMeasure,
            UnitOfMeasureId = model.UnitOfMeasureId
        };

        return Validate(createDto);
    }
}

/// <summary>
/// Validation result containing status and error messages
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of translation keys for validation errors
    /// </summary>
    public List<string> ErrorKeys { get; init; } = new();

    /// <summary>
    /// Gets translated error messages
    /// </summary>
    public List<string> GetErrorMessages(ITranslationService translationService)
    {
        return ErrorKeys.Select(key => 
            translationService.GetTranslation(key, GetDefaultMessage(key))
        ).ToList();
    }

    private static string GetDefaultMessage(string key)
    {
        return key switch
        {
            "validation.descriptionRequired" => "La descrizione è obbligatoria",
            "validation.quantityMustBePositive" => "La quantità deve essere maggiore di zero",
            "validation.quantityTooLarge" => "La quantità è troppo elevata (max 999,999)",
            "validation.unitPriceCannotBeNegative" => "Il prezzo unitario non può essere negativo",
            "validation.unitPriceTooLarge" => "Il prezzo unitario è troppo elevato (max 9,999,999.99)",
            "validation.discountPercentageInvalid" => "Lo sconto percentuale deve essere tra 0 e 100",
            "validation.discountValueCannotBeNegative" => "Il valore dello sconto non può essere negativo",
            "validation.discountValueExceedsTotal" => "Il valore dello sconto supera il totale della riga",
            "validation.vatRateInvalid" => "L'aliquota IVA deve essere tra 0 e 100",
            "validation.unitOfMeasureRequired" => "L'unità di misura è obbligatoria",
            _ => "Errore di validazione"
        };
    }
}
