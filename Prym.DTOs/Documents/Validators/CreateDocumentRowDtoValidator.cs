using FluentValidation;
using Prym.DTOs.Common;

namespace Prym.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentRowDto"/>.
    /// </summary>
    public class CreateDocumentRowDtoValidator : AbstractValidator<CreateDocumentRowDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentRowDtoValidator"/> class.
        /// </summary>
        public CreateDocumentRowDtoValidator()
        {
            RuleFor(x => x.DocumentHeaderId)
                .NotEmpty()
                .WithMessage("The document header ID is required.");

            RuleFor(x => x.ProductCode)
                .MaximumLength(50)
                .WithMessage("Product code cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.")
                .MaximumLength(200)
                .WithMessage("Description cannot exceed 200 characters.");

            RuleFor(x => x.UnitOfMeasure)
                .MaximumLength(10)
                .WithMessage("Unit of measure cannot exceed 10 characters.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit price must be non-negative.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween((decimal)ValidationConstants.MinimumQuantity, (decimal)ValidationConstants.MaximumQuantity)
                .WithMessage("Quantity must be between 0.0001 and 10000.");

            RuleFor(x => x.LineDiscount)
                .InclusiveBetween(0, 100)
                .WithMessage("Line discount must be between 0 and 100.");

            RuleFor(x => x.LineDiscountString)
                .MaximumLength(50)
                .WithMessage("Line discount string cannot exceed 50 characters.")
                .Must((dto, s) => ValidateLineDiscountString(s))
                .When(x => !string.IsNullOrWhiteSpace(x.LineDiscountString))
                .WithMessage("Formato sconto non valido. Usare numeri tra 0 e 100 separati da '+' (es. '10+5').")
                .Must((dto, s) => dto.DiscountType == DiscountType.Percentage)
                .When(x => !string.IsNullOrWhiteSpace(x.LineDiscountString))
                .WithMessage("Lo sconto concatenato è applicabile solo con tipo sconto Percentuale.");

            RuleFor(x => x.LineDiscountValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Line discount value must be non-negative.");

            RuleFor(x => x.VatRate)
                .InclusiveBetween(0, 100)
                .WithMessage("VAT rate must be between 0 and 100.");

            RuleFor(x => x.VatDescription)
                .MaximumLength(30)
                .WithMessage("VAT description cannot exceed 30 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.");
        }
        private static bool ValidateLineDiscountString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var result = DiscountStringParser.Parse(value);
            return result.IsValid;
        }
    }
}
