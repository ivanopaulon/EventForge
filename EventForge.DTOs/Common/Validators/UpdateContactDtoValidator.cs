using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateContactDto"/>.
    /// </summary>
    public class UpdateContactDtoValidator : AbstractValidator<UpdateContactDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateContactDtoValidator"/> class.
        /// </summary>
        public UpdateContactDtoValidator()
        {
            RuleFor(x => x.ContactType)
                .NotEmpty()
                .WithMessage("Contact type is required.");

            RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Contact value is required.")
                .MaximumLength(100)
                .WithMessage("Contact value cannot exceed 100 characters.");

            RuleFor(x => x.Relationship)
                .MaximumLength(50)
                .WithMessage("The relationship cannot exceed 50 characters.")
                .When(x => x.Relationship != null);

            RuleFor(x => x.Notes)
                .MaximumLength(100)
                .WithMessage("Notes cannot exceed 100 characters.")
                .When(x => x.Notes != null);
        }
    }
}
