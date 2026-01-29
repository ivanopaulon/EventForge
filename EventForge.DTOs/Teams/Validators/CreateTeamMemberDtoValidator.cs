using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateTeamMemberDto"/>.
    /// </summary>
    public class CreateTeamMemberDtoValidator : AbstractValidator<CreateTeamMemberDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTeamMemberDtoValidator"/> class.
        /// </summary>
        public CreateTeamMemberDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("The first name is required.")
                .MaximumLength(100)
                .WithMessage("The first name cannot exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("The last name is required.")
                .MaximumLength(100)
                .WithMessage("The last name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .WithMessage("The email cannot exceed 100 characters.")
                .EmailAddress()
                .WithMessage("Invalid email address.")
                .When(x => x.Email != null);

            RuleFor(x => x.Role)
                .MaximumLength(50)
                .WithMessage("The role cannot exceed 50 characters.")
                .When(x => x.Role != null);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid team member status.");

            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team is required.");

            RuleFor(x => x.Position)
                .MaximumLength(50)
                .WithMessage("The position cannot exceed 50 characters.")
                .When(x => x.Position != null);

            RuleFor(x => x.JerseyNumber)
                .InclusiveBetween(1, 999)
                .WithMessage("Jersey number must be between 1 and 999.")
                .When(x => x.JerseyNumber.HasValue);

            RuleFor(x => x.EligibilityStatus)
                .IsInEnum()
                .WithMessage("Invalid eligibility status.");
        }
    }
}
