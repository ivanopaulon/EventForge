using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateProductBundleItemDto"/>.
    /// </summary>
    public class CreateProductBundleItemDtoValidator : AbstractValidator<CreateProductBundleItemDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProductBundleItemDtoValidator"/> class.
        /// </summary>
        public CreateProductBundleItemDtoValidator()
        {
            RuleFor(x => x.BundleProductId)
                .NotEmpty()
                .WithMessage("The bundle product is required.");

            RuleFor(x => x.ComponentProductId)
                .NotEmpty()
                .WithMessage("The component product is required.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 10000)
                .WithMessage("Quantity must be between 1 and 10,000.");
        }
    }
}
