using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateInventoryDocumentDto.
    /// </summary>
    public class CreateInventoryDocumentDtoValidator : AbstractValidator<CreateInventoryDocumentDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInventoryDocumentDtoValidator"/> class.
        /// </summary>
        public CreateInventoryDocumentDtoValidator()
        {
            RuleFor(x => x.InventoryDate)
                .NotEmpty()
                .WithMessage("Inventory date is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.Series)
                .MaximumLength(10)
                .WithMessage("Series cannot exceed 10 characters.")
                .When(x => x.Series != null);

            RuleFor(x => x.Number)
                .MaximumLength(30)
                .WithMessage("Number cannot exceed 30 characters.")
                .When(x => x.Number != null);
        }
    }
}
