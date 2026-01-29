using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateProductBundleItemDto"/>.
    /// </summary>
    public class UpdateProductBundleItemDtoValidator : AbstractValidator<UpdateProductBundleItemDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProductBundleItemDtoValidator"/> class.
        /// </summary>
        public UpdateProductBundleItemDtoValidator()
        {
            RuleFor(x => x.ComponentProductId)
                .NotEmpty()
                .WithMessage("The component product is required.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 10000)
                .WithMessage("Quantity must be between 1 and 10,000.");
        }
    }
}
