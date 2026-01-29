using System;
using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateInsurancePolicyDto"/>.
    /// </summary>
    public class UpdateInsurancePolicyDtoValidator : AbstractValidator<UpdateInsurancePolicyDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInsurancePolicyDtoValidator"/> class.
        /// </summary>
        public UpdateInsurancePolicyDtoValidator()
        {
            RuleFor(x => x.Provider)
                .MaximumLength(100)
                .WithMessage("The provider cannot exceed 100 characters.")
                .When(x => x.Provider != null);

            RuleFor(x => x.PolicyNumber)
                .MaximumLength(50)
                .WithMessage("The policy number cannot exceed 50 characters.")
                .When(x => x.PolicyNumber != null);

            RuleFor(x => x.ValidTo)
                .GreaterThan(x => x.ValidFrom ?? DateTime.MinValue)
                .WithMessage("Valid to date must be after valid from date.")
                .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue);

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
