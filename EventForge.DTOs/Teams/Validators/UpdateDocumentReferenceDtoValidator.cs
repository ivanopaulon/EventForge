using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateDocumentReferenceDto"/>.
    /// </summary>
    public class UpdateDocumentReferenceDtoValidator : AbstractValidator<UpdateDocumentReferenceDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDocumentReferenceDtoValidator"/> class.
        /// </summary>
        public UpdateDocumentReferenceDtoValidator()
        {
            RuleFor(x => x.FileName)
                .MaximumLength(255)
                .WithMessage("The filename cannot exceed 255 characters.")
                .When(x => x.FileName != null);

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid document reference type.")
                .When(x => x.Type.HasValue);

            RuleFor(x => x.SubType)
                .IsInEnum()
                .WithMessage("Invalid document reference sub-type.")
                .When(x => x.SubType.HasValue);

            RuleFor(x => x.Url)
                .MaximumLength(1000)
                .WithMessage("The URL cannot exceed 1000 characters.")
                .When(x => x.Url != null);

            RuleFor(x => x.ThumbnailStorageKey)
                .MaximumLength(500)
                .WithMessage("The thumbnail storage key cannot exceed 500 characters.")
                .When(x => x.ThumbnailStorageKey != null);

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("The title cannot exceed 200 characters.")
                .When(x => x.Title != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
