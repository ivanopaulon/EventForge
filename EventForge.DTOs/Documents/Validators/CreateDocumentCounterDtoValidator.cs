using FluentValidation;
using System;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentCounterDto"/>.
    /// </summary>
    public class CreateDocumentCounterDtoValidator : AbstractValidator<CreateDocumentCounterDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentCounterDtoValidator"/> class.
        /// </summary>
        public CreateDocumentCounterDtoValidator()
        {
            RuleFor(x => x.DocumentTypeId)
                .NotEmpty()
                .WithMessage("Document type is required.");

            RuleFor(x => x.Series)
                .NotEmpty()
                .WithMessage("Series is required.")
                .MaximumLength(10)
                .WithMessage("Series cannot exceed 10 characters.");

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
