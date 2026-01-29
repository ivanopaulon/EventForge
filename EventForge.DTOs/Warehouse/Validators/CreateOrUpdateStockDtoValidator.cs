using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateOrUpdateStockDto.
    /// </summary>
    public class CreateOrUpdateStockDtoValidator : AbstractValidator<CreateOrUpdateStockDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOrUpdateStockDtoValidator"/> class.
        /// </summary>
        public CreateOrUpdateStockDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");

            RuleFor(x => x.StorageLocationId)
                .NotEmpty()
                .WithMessage("Storage location is required.");

            RuleFor(x => x.NewQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantity must be non-negative.");

            RuleFor(x => x.ReservedQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Reserved quantity must be non-negative.");

            RuleFor(x => x.MinimumLevel)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum level must be non-negative.")
                .When(x => x.MinimumLevel.HasValue);

            RuleFor(x => x.MaximumLevel)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Maximum level must be non-negative.")
                .When(x => x.MaximumLevel.HasValue);

            RuleFor(x => x.ReorderPoint)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Reorder point must be non-negative.")
                .When(x => x.ReorderPoint.HasValue);

            RuleFor(x => x.ReorderQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Reorder quantity must be non-negative.")
                .When(x => x.ReorderQuantity.HasValue);

            RuleFor(x => x.UnitCost)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit cost must be non-negative.")
                .When(x => x.UnitCost.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);
        }
    }
}
