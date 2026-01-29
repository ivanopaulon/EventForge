using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateBrandDto"/>.
    /// </summary>
    public class UpdateBrandDtoValidator : AbstractValidator<UpdateBrandDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateBrandDtoValidator"/> class.
        /// </summary>
        public UpdateBrandDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The brand name is required.")
                .MaximumLength(200)
                .WithMessage("The brand name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("The description cannot exceed 1000 characters.");

            RuleFor(x => x.Website)
                .MaximumLength(500)
                .WithMessage("The website URL cannot exceed 500 characters.");

            RuleFor(x => x.Country)
                .MaximumLength(100)
                .WithMessage("The country cannot exceed 100 characters.");
        }
    }
}
