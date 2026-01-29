using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateStockMovementDto.
    /// </summary>
    public class CreateStockMovementDtoValidator : AbstractValidator<CreateStockMovementDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStockMovementDtoValidator"/> class.
        /// </summary>
        public CreateStockMovementDtoValidator()
        {
            RuleFor(x => x.MovementType)
                .NotEmpty()
                .WithMessage("Movement type is required.");

            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");

            RuleFor(x => x.Quantity)
                .NotEmpty()
                .WithMessage("Quantity is required.");

            RuleFor(x => x.UnitCost)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit cost must be non-negative.")
                .When(x => x.UnitCost.HasValue);

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.Reference)
                .MaximumLength(50)
                .WithMessage("Reference cannot exceed 50 characters.")
                .When(x => x.Reference != null);

            RuleFor(x => x.UserId)
                .MaximumLength(100)
                .WithMessage("User ID cannot exceed 100 characters.")
                .When(x => x.UserId != null);
        }
    }
}
