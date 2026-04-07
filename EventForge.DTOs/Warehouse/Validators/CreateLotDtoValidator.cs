using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateLotDto.
    /// </summary>
    public class CreateLotDtoValidator : AbstractValidator<CreateLotDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLotDtoValidator"/> class.
        /// </summary>
        public CreateLotDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Lot code is required.")
                .MaximumLength(50)
                .WithMessage("Lot code cannot exceed 50 characters.");

            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");

            RuleFor(x => x.OriginalQuantity)
                .NotEmpty()
                .WithMessage("Original quantity is required.")
                .GreaterThanOrEqualTo(0.01m)
                .WithMessage("Original quantity must be at least 0.01.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.Barcode)
                .MaximumLength(50)
                .WithMessage("Barcode cannot exceed 50 characters.")
                .When(x => x.Barcode != null);

            RuleFor(x => x.CountryOfOrigin)
                .MaximumLength(50)
                .WithMessage("Country of origin cannot exceed 50 characters.")
                .When(x => x.CountryOfOrigin != null);
        }
    }
}
