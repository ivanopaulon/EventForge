using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateReferenceDto"/>.
    /// </summary>
    public class UpdateReferenceDtoValidator : AbstractValidator<UpdateReferenceDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateReferenceDtoValidator"/> class.
        /// </summary>
        public UpdateReferenceDtoValidator()
        {
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
