using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentReferenceDto"/>.
    /// </summary>
    public class CreateDocumentReferenceDtoValidator : AbstractValidator<CreateDocumentReferenceDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentReferenceDtoValidator"/> class.
        /// </summary>
        public CreateDocumentReferenceDtoValidator()
        {
            RuleFor(x => x.OwnerType)
                .MaximumLength(50)
                .WithMessage("The owner type cannot exceed 50 characters.")
                .When(x => x.OwnerType != null);

            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("The filename is required.")
                .MaximumLength(255)
                .WithMessage("The filename cannot exceed 255 characters.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid document reference type.");

            RuleFor(x => x.SubType)
                .IsInEnum()
                .WithMessage("Invalid document reference sub-type.");

            RuleFor(x => x.MimeType)
                .NotEmpty()
                .WithMessage("The MIME type is required.")
                .MaximumLength(100)
                .WithMessage("The MIME type cannot exceed 100 characters.");

            RuleFor(x => x.StorageKey)
                .NotEmpty()
                .WithMessage("The storage key is required.")
                .MaximumLength(500)
                .WithMessage("The storage key cannot exceed 500 characters.");

            RuleFor(x => x.Url)
                .MaximumLength(1000)
                .WithMessage("The URL cannot exceed 1000 characters.")
                .When(x => x.Url != null);

            RuleFor(x => x.ThumbnailStorageKey)
                .MaximumLength(500)
                .WithMessage("The thumbnail storage key cannot exceed 500 characters.")
                .When(x => x.ThumbnailStorageKey != null);

            RuleFor(x => x.FileSizeBytes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("File size must be non-negative.");

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
