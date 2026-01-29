using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateProductDto"/>.
    /// </summary>
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProductDtoValidator"/> class.
        /// </summary>
        public UpdateProductDtoValidator()
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

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500)
                .WithMessage("The image URL cannot exceed 500 characters.");

            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("The product status is required.");

            RuleFor(x => x.DefaultPrice)
                .GreaterThanOrEqualTo(0)
                .When(x => x.DefaultPrice.HasValue)
                .WithMessage("Price must be positive.");

            RuleFor(x => x.ReorderPoint)
                .GreaterThanOrEqualTo(0)
                .When(x => x.ReorderPoint.HasValue)
                .WithMessage("Reorder point must be non-negative.");

            RuleFor(x => x.SafetyStock)
                .GreaterThanOrEqualTo(0)
                .When(x => x.SafetyStock.HasValue)
                .WithMessage("Safety stock must be non-negative.");

            RuleFor(x => x.TargetStockLevel)
                .GreaterThanOrEqualTo(0)
                .When(x => x.TargetStockLevel.HasValue)
                .WithMessage("Target stock level must be non-negative.");

            RuleFor(x => x.AverageDailyDemand)
                .GreaterThanOrEqualTo(0)
                .When(x => x.AverageDailyDemand.HasValue)
                .WithMessage("Average daily demand must be non-negative.");
        }
    }
}
