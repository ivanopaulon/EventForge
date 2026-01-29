using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for UpdateLotDto.
    /// </summary>
    public class UpdateLotDtoValidator : AbstractValidator<UpdateLotDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateLotDtoValidator"/> class.
        /// </summary>
        public UpdateLotDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Lot code is required.")
                .MaximumLength(50)
                .WithMessage("Lot code cannot exceed 50 characters.");

            RuleFor(x => x.AvailableQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Available quantity must be non-negative.");

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
