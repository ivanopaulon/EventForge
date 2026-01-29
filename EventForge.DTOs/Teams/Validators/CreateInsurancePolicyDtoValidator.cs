using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateInsurancePolicyDto"/>.
    /// </summary>
    public class CreateInsurancePolicyDtoValidator : AbstractValidator<CreateInsurancePolicyDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInsurancePolicyDtoValidator"/> class.
        /// </summary>
        public CreateInsurancePolicyDtoValidator()
        {
            RuleFor(x => x.TeamMemberId)
                .NotEmpty()
                .WithMessage("Team member is required.");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("The provider is required.")
                .MaximumLength(100)
                .WithMessage("The provider cannot exceed 100 characters.");

            RuleFor(x => x.PolicyNumber)
                .NotEmpty()
                .WithMessage("The policy number is required.")
                .MaximumLength(50)
                .WithMessage("The policy number cannot exceed 50 characters.");

            RuleFor(x => x.ValidFrom)
                .NotEmpty()
                .WithMessage("Valid from date is required.");

            RuleFor(x => x.ValidTo)
                .NotEmpty()
                .WithMessage("Valid to date is required.")
                .GreaterThan(x => x.ValidFrom)
                .WithMessage("Valid to date must be after valid from date.");

            RuleFor(x => x.CoverageType)
                .MaximumLength(100)
                .WithMessage("The coverage type cannot exceed 100 characters.")
                .When(x => x.CoverageType != null);

            RuleFor(x => x.CoverageAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Coverage amount must be non-negative.")
                .When(x => x.CoverageAmount.HasValue);

            RuleFor(x => x.Currency)
                .MaximumLength(3)
                .WithMessage("The currency code cannot exceed 3 characters.")
                .When(x => x.Currency != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
