using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateMembershipCardDto"/>.
    /// </summary>
    public class CreateMembershipCardDtoValidator : AbstractValidator<CreateMembershipCardDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateMembershipCardDtoValidator"/> class.
        /// </summary>
        public CreateMembershipCardDtoValidator()
        {
            RuleFor(x => x.TeamMemberId)
                .NotEmpty()
                .WithMessage("Team member is required.");

            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage("The card number is required.")
                .MaximumLength(50)
                .WithMessage("The card number cannot exceed 50 characters.");

            RuleFor(x => x.Federation)
                .NotEmpty()
                .WithMessage("The federation is required.")
                .MaximumLength(100)
                .WithMessage("The federation cannot exceed 100 characters.");

            RuleFor(x => x.ValidFrom)
                .NotEmpty()
                .WithMessage("Valid from date is required.");

            RuleFor(x => x.ValidTo)
                .NotEmpty()
                .WithMessage("Valid to date is required.")
                .GreaterThan(x => x.ValidFrom)
                .WithMessage("Valid to date must be after valid from date.");

            RuleFor(x => x.Category)
                .MaximumLength(50)
                .WithMessage("The category cannot exceed 50 characters.")
                .When(x => x.Category != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
