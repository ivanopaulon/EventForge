using FluentValidation;

namespace EventForge.DTOs.VatRates.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateVatRateDto"/>.
    /// </summary>
    public class UpdateVatRateDtoValidator : AbstractValidator<UpdateVatRateDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateVatRateDtoValidator"/> class.
        /// </summary>
        public UpdateVatRateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Percentage)
                .InclusiveBetween(0, 100)
                .WithMessage("The percentage must be between 0 and 100.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid VAT rate status.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);
        }
    }
}
