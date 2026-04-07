using FluentValidation;

namespace EventForge.DTOs.VatRates.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateVatNatureDto"/>.
    /// </summary>
    public class UpdateVatNatureDtoValidator : AbstractValidator<UpdateVatNatureDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateVatNatureDtoValidator"/> class.
        /// </summary>
        public UpdateVatNatureDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("The code is required.")
                .MaximumLength(10)
                .WithMessage("The code cannot exceed 10 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("The description cannot exceed 500 characters.")
                .When(x => x.Description != null);
        }
    }
}
