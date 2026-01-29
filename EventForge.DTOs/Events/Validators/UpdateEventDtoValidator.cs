using FluentValidation;

namespace EventForge.DTOs.Events.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateEventDto"/>.
    /// </summary>
    public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEventDtoValidator"/> class.
        /// </summary>
        public UpdateEventDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The event name is required.")
                .MaximumLength(100)
                .WithMessage("The event name cannot exceed 100 characters.");

            RuleFor(x => x.ShortDescription)
                .NotEmpty()
                .WithMessage("The short description is required.")
                .MaximumLength(200)
                .WithMessage("The short description cannot exceed 200 characters.");

            RuleFor(x => x.Location)
                .MaximumLength(200)
                .WithMessage("The location cannot exceed 200 characters.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("The start date is required.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be greater than or equal to start date.")
                .When(x => x.EndDate.HasValue);

            RuleFor(x => x.Capacity)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Capacity must be at least 1.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid event status.");
        }
    }
}
