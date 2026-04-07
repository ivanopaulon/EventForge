using FluentValidation;

namespace EventForge.DTOs.Station.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateStationDto"/>.
    /// </summary>
    public class CreateStationDtoValidator : AbstractValidator<CreateStationDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStationDtoValidator"/> class.
        /// </summary>
        public CreateStationDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The station name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid station status.");

            RuleFor(x => x.Location)
                .MaximumLength(50)
                .WithMessage("The location cannot exceed 50 characters.")
                .When(x => x.Location != null);

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);
        }
    }
}
