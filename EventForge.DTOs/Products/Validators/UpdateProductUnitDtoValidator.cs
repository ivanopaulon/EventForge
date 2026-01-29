using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateProductUnitDto"/>.
    /// </summary>
    public class UpdateProductUnitDtoValidator : AbstractValidator<UpdateProductUnitDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProductUnitDtoValidator"/> class.
        /// </summary>
        public UpdateProductUnitDtoValidator()
        {
            RuleFor(x => x.UnitOfMeasureId)
                .NotEmpty()
                .WithMessage("The unit of measure is required.");

            RuleFor(x => x.ConversionFactor)
                .GreaterThanOrEqualTo(0.001m)
                .WithMessage("The conversion factor must be greater than zero.");

            RuleFor(x => x.UnitType)
                .NotEmpty()
                .WithMessage("The unit type is required.")
                .MaximumLength(20)
                .WithMessage("The unit type cannot exceed 20 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(100)
                .WithMessage("The description cannot exceed 100 characters.");

            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("The product unit status is required.");
        }
    }
}
