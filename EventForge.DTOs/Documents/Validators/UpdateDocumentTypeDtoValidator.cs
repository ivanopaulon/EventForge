using FluentValidation;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateDocumentTypeDto"/>.
    /// </summary>
    public class UpdateDocumentTypeDtoValidator : AbstractValidator<UpdateDocumentTypeDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDocumentTypeDtoValidator"/> class.
        /// </summary>
        public UpdateDocumentTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .Length(1, 50)
                .WithMessage("Name must be between 1 and 50 characters.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Code is required.")
                .Length(1, 10)
                .WithMessage("Code must be between 1 and 10 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.");
        }
    }
}
