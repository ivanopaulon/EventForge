using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateContactDto"/>.
    /// </summary>
    public class CreateContactDtoValidator : AbstractValidator<CreateContactDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateContactDtoValidator"/> class.
        /// </summary>
        public CreateContactDtoValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Owner ID is required.");

            RuleFor(x => x.OwnerType)
                .NotEmpty()
                .WithMessage("Owner type is required.")
                .MaximumLength(50)
                .WithMessage("Owner type cannot exceed 50 characters.");

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
