using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for UpdateInventoryDocumentDto.
    /// </summary>
    public class UpdateInventoryDocumentDtoValidator : AbstractValidator<UpdateInventoryDocumentDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInventoryDocumentDtoValidator"/> class.
        /// </summary>
        public UpdateInventoryDocumentDtoValidator()
        {
            RuleFor(x => x.InventoryDate)
                .NotEmpty()
                .WithMessage("Inventory date is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
