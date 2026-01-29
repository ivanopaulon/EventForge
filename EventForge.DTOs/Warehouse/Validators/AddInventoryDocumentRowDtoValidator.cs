using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for AddInventoryDocumentRowDto.
    /// </summary>
    public class AddInventoryDocumentRowDtoValidator : AbstractValidator<AddInventoryDocumentRowDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddInventoryDocumentRowDtoValidator"/> class.
        /// </summary>
        public AddInventoryDocumentRowDtoValidator()
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
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);
        }
    }
}
