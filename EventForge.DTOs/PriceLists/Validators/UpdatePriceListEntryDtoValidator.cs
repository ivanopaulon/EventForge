using FluentValidation;

namespace EventForge.DTOs.PriceLists.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdatePriceListEntryDto"/>.
    /// </summary>
    public class UpdatePriceListEntryDtoValidator : AbstractValidator<UpdatePriceListEntryDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePriceListEntryDtoValidator"/> class.
        /// </summary>
        public UpdatePriceListEntryDtoValidator()
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("The price must be greater than or equal to zero.");

            RuleFor(x => x.Currency)
                .MaximumLength(3)
                .WithMessage("The currency code cannot exceed 3 characters.");

            RuleFor(x => x.Score)
                .InclusiveBetween(0, 100)
                .WithMessage("The score must be between 0 and 100.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid price list entry status.");

            RuleFor(x => x.MinQuantity)
                .GreaterThanOrEqualTo(1)
                .WithMessage("The minimum quantity must be at least 1.");

            RuleFor(x => x.MaxQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("The maximum quantity must be greater than or equal to zero.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
