using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for UpdateInventoryDocumentRowDto.
    /// </summary>
    public class UpdateInventoryDocumentRowDtoValidator : AbstractValidator<UpdateInventoryDocumentRowDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInventoryDocumentRowDtoValidator"/> class.
        /// </summary>
        public UpdateInventoryDocumentRowDtoValidator()
        {
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
