using FluentValidation;

namespace EventForge.DTOs.Licensing.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateLicenseDto"/>.
    /// </summary>
    public class CreateLicenseDtoValidator : AbstractValidator<CreateLicenseDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLicenseDtoValidator"/> class.
        /// </summary>
        public CreateLicenseDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .WithMessage("Display name is required.")
                .MaximumLength(200)
                .WithMessage("Display name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.MaxUsers)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Maximum users must be at least 1.");

            RuleFor(x => x.MaxApiCallsPerMonth)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Maximum API calls per month must be non-negative.");

            RuleFor(x => x.TierLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("Tier level must be between 1 and 10.");
        }
    }
}
