using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateInventoryEntryDto.
    /// </summary>
    public class CreateInventoryEntryDtoValidator : AbstractValidator<CreateInventoryEntryDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInventoryEntryDtoValidator"/> class.
        /// </summary>
        public CreateInventoryEntryDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product identifier is required.");

            RuleFor(x => x.LocationId)
                .NotEmpty()
                .WithMessage("Storage location identifier is required.");

            RuleFor(x => x.Quantity)
                .NotEmpty()
                .WithMessage("Quantity is required.")
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantity must be non-negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
