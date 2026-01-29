using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateTeamDto"/>.
    /// </summary>
    public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTeamDtoValidator"/> class.
        /// </summary>
        public CreateTeamDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The team name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.ShortDescription)
                .MaximumLength(200)
                .WithMessage("The short description cannot exceed 200 characters.");

            RuleFor(x => x.LongDescription)
                .MaximumLength(1000)
                .WithMessage("The long description cannot exceed 1000 characters.");

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .WithMessage("The email cannot exceed 100 characters.")
                .EmailAddress()
                .WithMessage("Invalid email address.")
                .When(x => x.Email != null);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid team status.");

            RuleFor(x => x.EventId)
                .NotEmpty()
                .WithMessage("Event is required.");

            RuleFor(x => x.ClubCode)
                .MaximumLength(50)
                .WithMessage("The club code cannot exceed 50 characters.")
                .When(x => x.ClubCode != null);

            RuleFor(x => x.FederationCode)
                .MaximumLength(50)
                .WithMessage("The federation code cannot exceed 50 characters.")
                .When(x => x.FederationCode != null);

            RuleFor(x => x.Category)
                .MaximumLength(50)
                .WithMessage("The category cannot exceed 50 characters.")
                .When(x => x.Category != null);
        }
    }
}
