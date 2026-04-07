using FluentValidation;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateDocumentCounterDto"/>.
    /// </summary>
    public class UpdateDocumentCounterDtoValidator : AbstractValidator<UpdateDocumentCounterDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDocumentCounterDtoValidator"/> class.
        /// </summary>
        public UpdateDocumentCounterDtoValidator()
        {
            RuleFor(x => x.CurrentValue)
                .NotEmpty()
                .WithMessage("Current value is required.");

            RuleFor(x => x.Prefix)
                .MaximumLength(10)
                .WithMessage("Prefix cannot exceed 10 characters.");

            RuleFor(x => x.PaddingLength)
                .InclusiveBetween(1, 10)
                .WithMessage("Padding length must be between 1 and 10.");

            RuleFor(x => x.FormatPattern)
                .MaximumLength(50)
                .WithMessage("Format pattern cannot exceed 50 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.");
        }
    }
}
