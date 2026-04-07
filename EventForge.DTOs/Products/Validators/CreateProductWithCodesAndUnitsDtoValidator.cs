using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateProductWithCodesAndUnitsDto"/>.
    /// </summary>
    public class CreateProductWithCodesAndUnitsDtoValidator : AbstractValidator<CreateProductWithCodesAndUnitsDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProductWithCodesAndUnitsDtoValidator"/> class.
        /// </summary>
        public CreateProductWithCodesAndUnitsDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The product name is required.")
                .MaximumLength(100)
                .WithMessage("The product name cannot exceed 100 characters.");

            RuleFor(x => x.ShortDescription)
                .MaximumLength(50)
                .WithMessage("The short description cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("The description cannot exceed 500 characters.");

            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("The product status is required.");

            RuleFor(x => x.DefaultPrice)
                .GreaterThanOrEqualTo(0)
                .When(x => x.DefaultPrice.HasValue)
                .WithMessage("Price must be positive.");
        }
    }
}
