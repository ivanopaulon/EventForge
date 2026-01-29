using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateReferenceDto"/>.
    /// </summary>
    public class CreateReferenceDtoValidator : AbstractValidator<CreateReferenceDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateReferenceDtoValidator"/> class.
        /// </summary>
        public CreateReferenceDtoValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Owner ID is required.");

            RuleFor(x => x.OwnerType)
                .NotEmpty()
                .WithMessage("Owner type is required.")
                .MaximumLength(50)
                .WithMessage("Owner type cannot exceed 50 characters.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.")
                .MaximumLength(50)
                .WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .MaximumLength(50)
                .WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Department)
                .MaximumLength(50)
                .WithMessage("Department cannot exceed 50 characters.")
                .When(x => x.Department != null);

            RuleFor(x => x.Notes)
                .MaximumLength(100)
                .WithMessage("Notes cannot exceed 100 characters.")
                .When(x => x.Notes != null);
        }
    }
}
